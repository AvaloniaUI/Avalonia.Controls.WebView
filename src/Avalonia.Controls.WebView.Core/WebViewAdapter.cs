using System;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Controls;

internal static class WebViewAdapter
{
    public static bool UseHeadless { get; set; }

    public abstract record AdapterFactory(WebViewAdapterInfo Info);

    public record AdapterWrapper(IPlatformHandle AdapterHandle, Task<IWebViewAdapter> AdapterInitializeTask);
    public delegate AdapterWrapper NativeWebViewAdapterBuilder(IPlatformHandle parent, Func<IPlatformHandle, IPlatformHandle> createChild);

    public record NativeHostAdapterFactory(NativeWebViewAdapterBuilder InvokeAsync, WebViewAdapterInfo Info) : AdapterFactory(Info);

    public delegate Task<IWebViewAdapterWithOffscreenBuffer> OffscreenWebViewAdapterBuilder(Control parent);
    public record CompositorHostAdapterFactory(OffscreenWebViewAdapterBuilder InvokeAsync, WebViewAdapterInfo Info) : AdapterFactory(Info);

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public static async Task<AdapterFactory?> CreateFactory(Action<WebViewEnvironmentRequestedEventArgs> environmentRequested)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        var deferralManager = new DeferralManager();
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

        if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
        {
            {
                var args = new WindowsWebView2EnvironmentRequestedEventArgs(deferralManager);
                environmentRequested(args);
                await deferralManager.WaitForDeferralsAsync();
                if (!args.PreferWebView1Instead
                    && Win.WebView2.CoreWebView2Environment.TryFindWebView2Runtime(args.BrowserExecutableFolder) !=
                    IntPtr.Zero)
                {
                    if (args.ExperimentalOffscreen && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
                    {
                        var info = Win.WebView2.WebView2BaseAdapter.GetWebView2Info(args.BrowserExecutableFolder, WebViewEmbeddingScenario.OffscreenRenderer);
                        var builder = await Win.WebView2.WebView2CompAdapter.CreateBuilder(args);
                        return new CompositorHostAdapterFactory(builder, info);
                    }
                    else
                    {
                        var info = Win.WebView2.WebView2BaseAdapter.GetWebView2Info(args.BrowserExecutableFolder);
                        var builder = await Win.WebView2.WebView2HwndAdapter.CreateBuilder(args);
                        return new NativeHostAdapterFactory(builder, info);
                    }
                }
            }
            {
                var args = new WindowsWebView1EnvironmentRequestedEventArgs(deferralManager);
                environmentRequested(args);
                await deferralManager.WaitForDeferralsAsync();
                if (Win.WebView1.WebView1Process.GetOrCreateProcess(args) is { } process)
                {
                    var builder = await Win.WebView1.WebView1Adapter.CreateBuilder(process);
                    return new NativeHostAdapterFactory(
                        builder,
                        Win.WebView1.WebView1Adapter.GetWebView1Info());
                }
            }
        }

        if (OperatingSystem.IsLinux())
        {
            if (Linux.WpeWebViewAdapter.IsAvailable())
            {
                var args = new LinuxWpeWebViewEnvironmentRequestedEventArgs(deferralManager);
                environmentRequested(args);
                await deferralManager.WaitForDeferralsAsync();

                if (!args.PreferWebKitGtkInstead)
                {
                    var builder = await Linux.WpeWebViewAdapter.CreateBuilder(args);
                    return new CompositorHostAdapterFactory(
                        builder,
                        Linux.WpeWebViewAdapter.GetWpeInfo());
                }
            }

            {
                var args = new GtkWebViewEnvironmentRequestedEventArgs(deferralManager);
                environmentRequested(args);
                await deferralManager.WaitForDeferralsAsync();
                if (args.ExperimentalOffscreen)
                {
                    var builder = await Gtk.GtkOffscreenAvaloniaWebViewAdapter.CreateBuilder(args);
                    return new CompositorHostAdapterFactory(builder, Gtk.GtkWebViewAdapter.GetWebKitGtkInfo(WebViewEmbeddingScenario.OffscreenRenderer));
                }
                else
                {
                    var builder = await Gtk.GtkX11WebViewAdapter.CreateBuilder(args);
                    return new NativeHostAdapterFactory(builder, Gtk.GtkWebViewAdapter.GetWebKitGtkInfo());
                }
            }
        }
#endif

        return null;
    }
}
