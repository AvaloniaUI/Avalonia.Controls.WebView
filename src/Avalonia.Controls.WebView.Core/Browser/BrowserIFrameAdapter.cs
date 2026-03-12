#if BROWSER
using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using Avalonia.Browser;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Controls.Browser;

// Note: this adapter is not yet compatible with WASM multithreading.
// In order to support that we need to use only async JS interop, but IWebViewAdapter API is not compatible with that.
[SupportedOSPlatform("browser")]
internal class BrowserIFrameAdapter : JSObjectControlHandle, IWebViewAdapter
{
    private static readonly Lazy<Task> s_importModule = new(WebViewInterop.EnsureLoaded);

    private Action? _subscriptions;
    private Uri? _lastSrc;

    private BrowserIFrameAdapter(JSObject iframe) : base(iframe)
    {
    }

    public static async Task<WebViewAdapter.NativeWebViewAdapterBuilder> CreateBuilder(
        BrowserWebViewEnvironmentRequestedEventArgs environmentArgs)
    {
        await s_importModule.Value;
        var iframe = await WebViewInterop.CreateElement("iframe");

        return (_, _) =>
        {
            var adapter = new BrowserIFrameAdapter(iframe);
            return new WebViewAdapter.AdapterWrapper(adapter, InitializeAsync(adapter, environmentArgs));
        };

        static async Task<IWebViewAdapter> InitializeAsync(
            BrowserIFrameAdapter adapter,
            BrowserWebViewEnvironmentRequestedEventArgs environmentArgs)
        {
            await adapter.InitializeAsync(environmentArgs);
            return adapter;
        }
    }

    public Color DefaultBackground { set { } }

    public void SizeChanged(PixelSize containerSize) { }

    public void SetParent(IPlatformHandle parent) { }

    public bool CanGoBack => WebViewInterop.CanGoBack(Object);

    public bool CanGoForward => false;

    public Uri Source
    {
        get
        {
            if (Uri.TryCreate(WebViewInterop.GetActualLocation(Object), UriKind.Absolute, out var location))
            {
                return location;
            }
            return _lastSrc!;
        }
        set { Navigate(value); }
    }

    public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;
    public event EventHandler<WebViewNewWindowRequestedEventArgs>? NewWindowRequested;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;

    public bool GoBack() => WebViewInterop.GoBack(Object);

    public bool GoForward() => WebViewInterop.GoForward(Object);

    public Task<string?> InvokeScript(string script)
    {
        return WebViewInterop.Eval(Object, script);
    }

    public void Navigate(Uri url)
    {
        _lastSrc = url;
        NavigationStarted?.Invoke(this, new WebViewNavigationStartingEventArgs { Request = url });
        Object.SetProperty("src", url.AbsoluteUri);
    }

    public void NavigateToString(string text)
    {
        _lastSrc = new Uri("about:srcdoc");
        Object.SetProperty("srcdoc", text);
    }

    public bool Refresh()
    {
        return WebViewInterop.Refresh(Object);
    }

    public bool Stop()
    {
        return WebViewInterop.Stop(Object);
    }

    public void Dispose()
    {
        _subscriptions?.Invoke();
    }

    internal static DetailedWebViewAdapterInfo GetBrowserInfo()
    {
        return new DetailedWebViewAdapterInfo(
            WebViewAdapterType.BrowserIFrame,
            WebViewEngine.Unknown,
            IsSupported: OperatingSystem.IsBrowser(),
            IsInstalled: OperatingSystem.IsBrowser(),
            Version: null,
            UnavailableReason: OperatingSystem.IsBrowser() ? null : "Not running in a browser environment.",
            SupportedScenarios: WebViewEmbeddingScenario.NativeControlHost);
    }

    private Task InitializeAsync(BrowserWebViewEnvironmentRequestedEventArgs environmentArgs)
    {
        var unsub = WebViewInterop.Subscribe(Object,
            src => NavigationCompleted?.Invoke(this, new WebViewNavigationCompletedEventArgs
            {
                Request = Uri.TryCreate(src, UriKind.Absolute, out var request) ? request : null
            }));

        _subscriptions = unsub;
        return Task.CompletedTask;
    }
}
#endif
