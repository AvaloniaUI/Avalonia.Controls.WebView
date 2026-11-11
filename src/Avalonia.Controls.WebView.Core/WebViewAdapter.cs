using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Platform;

namespace Avalonia.Controls;

internal static class WebViewAdapter
{
    internal static readonly IReadOnlyList<WebViewAdapterType> WindowsDefaultOrder =
        [WebViewAdapterType.WebView2, WebViewAdapterType.WebView1];
    internal static readonly IReadOnlyList<WebViewAdapterType> LinuxDefaultOrder =
        [WebViewAdapterType.WpeWebKit, WebViewAdapterType.WebKitGtk];

    public static bool UseHeadless { get; set; }

    public abstract record AdapterFactory(WebViewAdapterInfo Info);

    public record AdapterWrapper(IPlatformHandle AdapterHandle, Task<IWebViewAdapter> AdapterInitializeTask);
    public delegate AdapterWrapper NativeWebViewAdapterBuilder(IPlatformHandle parent, Func<IPlatformHandle, IPlatformHandle> createChild);

    public record NativeHostAdapterFactory(NativeWebViewAdapterBuilder InvokeAsync, WebViewAdapterInfo Info) : AdapterFactory(Info);

    public delegate Task<IWebViewAdapterWithOffscreenBuffer> OffscreenWebViewAdapterBuilder(Control parent);
    public record CompositorHostAdapterFactory(OffscreenWebViewAdapterBuilder InvokeAsync, WebViewAdapterInfo Info) : AdapterFactory(Info);

    public record UnavailableAdapterFactory(DetailedWebViewAdapterInfo DetailedInfo) : AdapterFactory(DetailedInfo);

    /// <summary>
    /// Selects an adapter and prepares its factory. Never throws:
    /// a failure is logged and returned as <see cref="UnavailableAdapterFactory"/>.
    /// </summary>
    public static async Task<AdapterFactory?> CreateFactory(
        Action<WebViewEnvironmentRequestedEventArgs> environmentRequested,
        IReadOnlyList<WebViewAdapterType>? adapterPreference = null)
    {
        // Kept separate from CreateFactoryCore: try/catch inside that method makes the platform compatibility analyzer run out of memory.
        try
        {
            return await CreateFactoryCore(environmentRequested, adapterPreference);
        }
        catch (Exception ex)
        {
            Logger.TryGet(LogEventLevel.Warning, "WebView")?.Log(null,
                "Failed to create the WebView adapter: {Exception}", ex);
            return new UnavailableAdapterFactory(WebViewAdapterInfo.Failed(ex));
        }
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private static async Task<AdapterFactory?> CreateFactoryCore(
        Action<WebViewEnvironmentRequestedEventArgs> environmentRequested,
        IReadOnlyList<WebViewAdapterType>? adapterPreference)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        var deferralManager = new DeferralManager();
        DetailedWebViewAdapterInfo? unavailableInfo = null;
        if (UseHeadless)
        {
            var args = new HeadlessWebViewEnvironmentRequestedEventArgs(deferralManager);
            environmentRequested(args);

            await deferralManager.WaitForDeferralsAsync();
            await Task.Yield();

            // Headless platform doesn't support NativeControlHost yet,
            // But compositor solution kinda works there.
            // Even though it's not production ready part of WebView (compositor/offscreen impl is not enabled by default).
            return new CompositorHostAdapterFactory(
                async _ => await Headless.HeadlessWebViewAdapter.CreateAsync(args),
                Headless.HeadlessWebViewAdapter.GetHeadlessInfo());
        }

#if ANDROID // Android is the only backend which conditionally compiled, the rest is always present and loaded in runtime
            // TODO: I would like to avoid Xamarin.Android here (as an extra target), and make it in-line with the rest. 
        if (OperatingSystem.IsAndroid())
        {
            var args = new AndroidWebViewEnvironmentRequestedEventArgs(deferralManager);
            environmentRequested(args);
            await deferralManager.WaitForDeferralsAsync();
            return new NativeHostAdapterFactory((parent, _) =>
            {
                IWebViewAdapter adapter = new Android.AndroidWebViewAdapter(parent, args);
                return new AdapterWrapper(adapter, Task.FromResult(adapter));
            }, Android.AndroidWebViewAdapter.GetAndroidWebViewInfo());
        }
#elif BROWSER
        if (OperatingSystem.IsBrowser())
        {
            var args = new BrowserWebViewEnvironmentRequestedEventArgs(deferralManager);
            environmentRequested(args);
            await deferralManager.WaitForDeferralsAsync();

            var builder = await Browser.BrowserIFrameAdapter.CreateBuilder(args);
            return new NativeHostAdapterFactory(
                builder,
                Browser.BrowserIFrameAdapter.GetBrowserInfo());
        }
#else
        if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
        {
            var args = new AppleWKWebViewEnvironmentRequestedEventArgs(deferralManager);
            environmentRequested(args);
            await deferralManager.WaitForDeferralsAsync();
            return new NativeHostAdapterFactory((_, _) =>
            {
                // NOTE: we add double platform condition here to shut up Roslyn Analyzer false positives.
                // If you are from the future, please check if it's still the case.
                if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
                {
                    IWebViewAdapter adapter = new Macios.MaciosWebViewAdapter(args);
                    return new AdapterWrapper(adapter, Task.FromResult(adapter));
                }
                throw new PlatformNotSupportedException();
            }, Macios.MaciosWebViewAdapter.GetWkWebViewInfo());
        }

        var hasExplicitPref = adapterPreference?.Count > 0;

        if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
        {
            var adapterTypes = GetAdapterTypes(adapterPreference,
                WindowsDefaultOrder);

            foreach (var type in adapterTypes)
            {
                if (type == WebViewAdapterType.WebView2)
                {
                    var args = new WindowsWebView2EnvironmentRequestedEventArgs(deferralManager);
                    environmentRequested(args);
                    await deferralManager.WaitForDeferralsAsync();

                    var factory = await CreateWebView2Factory(args);
                    if (factory is UnavailableAdapterFactory unavailable)
                    {
                        unavailableInfo ??= unavailable.DetailedInfo;
                        continue;
                    }

                    return factory;
                }

                if (type == WebViewAdapterType.WebView1)
                {
                    var info = Win.WebView1.WebView1Adapter.GetWebView1Info();
                    if (!info.IsInstalled)
                    {
                        unavailableInfo ??= info;
                        continue;
                    }

                    var args = new WindowsWebView1EnvironmentRequestedEventArgs(deferralManager);
                    environmentRequested(args);
                    await deferralManager.WaitForDeferralsAsync();

                    var factory = await CreateWebView1Factory(args, info);
                    if (factory is UnavailableAdapterFactory unavailable)
                    {
                        unavailableInfo ??= unavailable.DetailedInfo;
                        continue;
                    }

                    return factory;
                }
            }
        }

        if (OperatingSystem.IsLinux())
        {
            var adapterTypes = GetAdapterTypes(adapterPreference,
                LinuxDefaultOrder);

            foreach (var type in adapterTypes)
            {
                if (type == WebViewAdapterType.WpeWebKit && Linux.WpeWebViewAdapter.IsAvailable())
                {
                    var args = new LinuxWpeWebViewEnvironmentRequestedEventArgs(deferralManager);
                    environmentRequested(args);
                    await deferralManager.WaitForDeferralsAsync();

#pragma warning disable CS0618 // Type or member is obsolete
                    if (hasExplicitPref || !args.PreferWebKitGtkInstead)
#pragma warning restore CS0618 // Type or member is obsolete
                    {
                        var builder = await Linux.WpeWebViewAdapter.CreateBuilder(args);
                        return new CompositorHostAdapterFactory(
                            builder,
                            Linux.WpeWebViewAdapter.GetWpeInfo());
                    }
                }

                if (type == WebViewAdapterType.WebKitGtk)
                {
                    if (!Gtk.GtkWebViewAdapter.GetWebKitGtkInfo().IsInstalled)
                        continue;

                    var args = new GtkWebViewEnvironmentRequestedEventArgs(deferralManager);
                    environmentRequested(args);
                    await deferralManager.WaitForDeferralsAsync();
                    if (args.ExperimentalOffscreen)
                    {
                        var builder = await Gtk.GtkOffscreenAvaloniaWebViewAdapter.CreateBuilder(args);
                        return new CompositorHostAdapterFactory(builder,
                            Gtk.GtkWebViewAdapter.GetWebKitGtkInfo(WebViewEmbeddingScenario.OffscreenRenderer));
                    }

                    var nativeBuilder = await Gtk.GtkX11WebViewAdapter.CreateBuilder(args);
                    return new NativeHostAdapterFactory(nativeBuilder, Gtk.GtkWebViewAdapter.GetWebKitGtkInfo());
                }
            }
        }
#endif

        return unavailableInfo is not null ? new UnavailableAdapterFactory(unavailableInfo) : null;
    }

#if !ANDROID && !BROWSER
    // Kept out of CreateFactory: try/catch inside that method makes the platform compatibility analyzer run out of memory.
    [SupportedOSPlatform("windows6.1")]
    private static async Task<AdapterFactory> CreateWebView2Factory(WindowsWebView2EnvironmentRequestedEventArgs args)
    {
        var offscreen = args.ExperimentalOffscreen && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763);
        var info = Win.WebView2.WebView2BaseAdapter.GetWebView2Info(args.BrowserExecutableFolder,
            offscreen ? WebViewEmbeddingScenario.OffscreenRenderer : WebViewEmbeddingScenario.NativeControlHost);

        if (!info.IsInstalled)
        {
            // An explicit environment doesn't need the runtime lookup.
            if (args.ExplicitEnvironment == IntPtr.Zero)
                return new UnavailableAdapterFactory(info);

            info = info with { IsInstalled = true, UnavailableReason = null };
        }

        try
        {
            if (offscreen && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
            {
                var builder = await Win.WebView2.WebView2CompAdapter.CreateBuilder(args);
                return new CompositorHostAdapterFactory(builder, info);
            }

            var nativeBuilder = await Win.WebView2.WebView2HwndAdapter.CreateBuilder(args);
            return new NativeHostAdapterFactory(nativeBuilder, info);
        }
        catch (Exception ex)
        {
            // The runtime is installed, but the environment couldn't be created.
            // For example, the user data folder isn't writable.
            Logger.TryGet(LogEventLevel.Warning, "WebView2")?.Log(null,
                "Failed to create the WebView2 environment: {Exception}", ex);
            return new UnavailableAdapterFactory(info with { UnavailableReason = ex.Message });
        }
    }

    [SupportedOSPlatform("windows6.1")]
    private static async Task<AdapterFactory> CreateWebView1Factory(
        WindowsWebView1EnvironmentRequestedEventArgs args, DetailedWebViewAdapterInfo info)
    {
        try
        {
            var process = Win.WebView1.WebView1Process.GetOrCreateProcess(args);
            var builder = await Win.WebView1.WebView1Adapter.CreateBuilder(process);
            return new NativeHostAdapterFactory(builder, info);
        }
        catch (Exception ex)
        {
            // EdgeHTML WebViewControl isn't available, for example on Windows editions without it.
            Logger.TryGet(LogEventLevel.Warning, "WebView1")?.Log(null,
                "Failed to create the WebView1 process: {Exception}", ex);
            return new UnavailableAdapterFactory(info with { UnavailableReason = ex.Message });
        }
    }
#endif

    internal static IReadOnlyList<WebViewAdapterType> GetAdapterTypes(
        IReadOnlyList<WebViewAdapterType>? preferredOrder,
        IReadOnlyList<WebViewAdapterType> defaultOrder)
    {
        if (preferredOrder is null || preferredOrder.Count == 0)
            return defaultOrder;

        var supported = new HashSet<WebViewAdapterType>(defaultOrder);
        var candidates = new List<WebViewAdapterType>(preferredOrder.Count);
        foreach (var candidate in preferredOrder)
        {
            if (supported.Remove(candidate))
                candidates.Add(candidate);
        }

        return candidates;
    }
}
