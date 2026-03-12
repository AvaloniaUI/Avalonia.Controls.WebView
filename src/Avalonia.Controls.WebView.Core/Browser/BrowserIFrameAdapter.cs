#if BROWSER
using System;
using System.IO;
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
internal class BrowserIFrameAdapter : JSObjectControlHandle, IWebViewAdapter,
    IWebViewAdapterWithFocus, IWebViewWithPrint
{
    private static readonly Lazy<Task> s_importModule = new(WebViewInterop.EnsureLoaded);

    private Action? _subscriptions;
    private Uri? _lastSrc;
    private bool _enablePostMessageBridge;

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

    public Color DefaultBackground
    {
        set
        {
            var color = $"rgba({value.R},{value.G},{value.B},{value.A / 255.0:F2})";
            WebViewInterop.SetBackground(Object, color);
        }
    }

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

    public event EventHandler? GotFocus;
    public event EventHandler<IWebViewAdapterWithFocus.LostFocusDirection>? LostFocus;

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
        NavigationStarted?.Invoke(this, new WebViewNavigationStartingEventArgs { Request = _lastSrc });
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

    public void Focus() => WebViewInterop.FocusIframe(Object);

    public void ResignFocus() => WebViewInterop.BlurIframe(Object);

    public bool ShowPrintUI()
    {
        return WebViewInterop.ShowPrintUI(Object);
    }

    public Task<Stream> PrintToPdfStreamAsync()
    {
        // PDF generation is not available in browser iframe context.
        throw new PlatformNotSupportedException("PrintToPdfStreamAsync is not supported in a browser environment.");
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
        _enablePostMessageBridge = environmentArgs.EnablePostMessageBridge;

        if (environmentArgs.Sandbox is { } sandbox)
        {
            WebViewInterop.SetSandbox(Object, sandbox);
        }

        var unsubLoad = WebViewInterop.Subscribe(Object, OnNavigationCompleted);

        var unsubFocus = WebViewInterop.SubscribeFocus(Object,
            () => GotFocus?.Invoke(this, EventArgs.Empty),
            () => LostFocus?.Invoke(this, IWebViewAdapterWithFocus.LostFocusDirection.Unknown));

        var unsubMessages = WebViewInterop.SubscribeMessages(Object,
            body => WebMessageReceived?.Invoke(this, new WebMessageReceivedEventArgs { Body = body }));

        _subscriptions = () =>
        {
            unsubLoad();
            unsubFocus();
            unsubMessages();
        };

        return Task.CompletedTask;
    }

    private void OnNavigationCompleted(string src)
    {
        if (_enablePostMessageBridge)
        {
            WebViewInterop.InjectPostMessageBridge(Object);
        }

        NavigationCompleted?.Invoke(this, new WebViewNavigationCompletedEventArgs
        {
            Request = Uri.TryCreate(src, UriKind.Absolute, out var request) ? request : null
        });
    }
}
#endif
