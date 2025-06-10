using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Win.WebView2.Interop;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Controls.Win.WebView2;

[SupportedOSPlatform("windows6.1")] // win7
internal abstract partial class WebView2BaseAdapter : IWebViewAdapterWithCookieManager, IWindowsWebView2PlatformHandle
{
    private ICoreWebView2Controller? _controller;
    private Action? _subscriptions;

    protected WebView2BaseAdapter(IPlatformHandle parent)
    {
        Initialize(parent);
    }

    public abstract IntPtr Handle { get; }
    public abstract string? HandleDescriptor { get; }

    protected unsafe ICoreWebView2? TryGetWebView2()
    {
        try
        {
            return _controller?.GetCoreWebView2();
        }
        // That's what WPF control does.
        catch (COMException ex) when (ex.HResult == -2147019873)
        {
            return null;
        }
    }

    public bool IsInitialized { get; private set; }

    public bool CanGoBack => TryGetWebView2()?.GetCanGoBack() == 1;

    public bool CanGoForward => TryGetWebView2()?.GetCanGoForward() == 1;

    public Uri Source
    {
        get => Uri.TryCreate(TryGetWebView2()?.GetSource(), UriKind.Absolute, out var url) ? url : null!;
        set => Navigate(value);
    }

    public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;
    public event EventHandler<WebViewNewWindowRequestedEventArgs>? NewWindowRequested;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public event EventHandler? Initialized;

    public bool GoBack()
    {
        if (TryGetWebView2() is { } webView2)
        {
            webView2.GoBack();
            return true;
        }
        return false;
    }

    public bool GoForward()
    {
        if (TryGetWebView2() is { } webView2)
        {
            webView2.GoForward();
            return true;
        }
        return false;
    }

    public Task<string?> InvokeScript(string scriptName)
    {
        if (TryGetWebView2() is { } webView2)
        {
            var handler = new WebView2ExecuteScriptCompletedHandler();
            webView2.ExecuteScript(scriptName, handler);
            return handler.Result.Task;
        }

        return Task.FromResult<string?>(null);
    }

    public void Navigate(Uri url)
    {
        TryGetWebView2()?.Navigate(url.AbsoluteUri);
    }

    public void NavigateToString(string text)
    {
        TryGetWebView2()?.NavigateToString(text);
    }

    public bool Refresh()
    {
        TryGetWebView2()?.Reload();
        return true;
    }

    public bool Stop()
    {
        TryGetWebView2()?.Stop();
        return true;
    }

    public virtual void SizeChanged(PixelSize containerSize)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (PInvoke.GetWindowRect(new HWND(Handle), out var rect)
                && _controller is not null)
            {
                if (_controller is ICoreWebView2Controller3 controller3)
                {
                    controller3.SetBoundsMode(COREWEBVIEW2_BOUNDS_MODE.COREWEBVIEW2_BOUNDS_MODE_USE_RAW_PIXELS);
                }

                _controller.SetBounds(new tagRECT
                {
                    right = rect.Width,
                    bottom = rect.Height
                });
                _controller.NotifyParentWindowPositionChanged();
            }
        });
    }

    public virtual void SetParent(IPlatformHandle parent)
    {
        if (_controller is null)
            return;

        if (parent.HandleDescriptor != "HWND")
            throw new InvalidOperationException("IPlatformHandle.HandleDescriptor must be HWND");

        _controller.SetParentWindow(parent.Handle);
    }

    internal void OnNavigationStarted(WebViewNavigationStartingEventArgs args) => NavigationStarted?.Invoke(this, args);
    internal void OnNavigationCompleted(WebViewNavigationCompletedEventArgs args) => NavigationCompleted?.Invoke(this, args);
    internal void OnWebMessageReceived(WebMessageReceivedEventArgs args) => WebMessageReceived?.Invoke(this, args);
    internal void OnNewWindowRequested(WebViewNewWindowRequestedEventArgs args) => NewWindowRequested?.Invoke(this, args);

    private async void Initialize(IPlatformHandle parentHost)
    {
        var env = await CoreWebView2Environment.CreateAsync();
        var controller = await CreateWebView2Controller(env, parentHost.Handle);
        var webView = controller.GetCoreWebView2();

        var addScriptCompletion = new AddScriptToExecuteOnDocumentCreatedCompletedHandler();
        webView.AddScriptToExecuteOnDocumentCreated(
            "function invokeCSharpAction(data){window.chrome.webview.postMessage(data);}", addScriptCompletion);
        _ = await addScriptCompletion.Result.Task;

        controller.SetIsVisible(1);

        if (controller is ICoreWebView2Controller3 controller3)
        {
            controller3.SetShouldDetectMonitorScaleChanges(0);
        }

        _controller = controller;

        SizeChanged(default);

        _subscriptions = AddHandlers(webView);

        IsInitialized = true;
        Initialized?.Invoke(this, EventArgs.Empty);
    }

    protected abstract Task<ICoreWebView2Controller> CreateWebView2Controller(ICoreWebView2Environment env, IntPtr handle);

    private Action AddHandlers(ICoreWebView2 webView)
    {
        var callbacks = new WebViewCallbacks(new WeakReference<WebView2BaseAdapter>(this));
        webView.add_NavigationStarting(callbacks, out var token1);
        webView.add_NavigationCompleted(callbacks, out var token2);
        webView.add_WebMessageReceived(callbacks, out var token3);
        webView.add_NewWindowRequested(callbacks, out var token4);

        return () =>
        {
            webView.remove_NavigationStarting(token1);
            webView.remove_NavigationCompleted(token2);
            webView.remove_WebMessageReceived(token3);
            webView.remove_NewWindowRequested(token4);
        };
    }

    public void AddOrUpdateCookie(Cookie cookie)
    {
        if (TryGetWebView2() is ICoreWebView2_2 webView2)
        {
            var cookieManager = webView2.GetCookieManager();
            var unitEpoch = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
            var seconds = (cookie.Expires.ToUniversalTime() - unitEpoch).TotalSeconds;

            var webViewCookie = cookieManager.CreateCookie(cookie.Name, cookie.Value, cookie.Domain, cookie.Path);
            webViewCookie.SetIsHttpOnly(cookie.HttpOnly ? 1 : 0);
            webViewCookie.SetIsSecure(cookie.Secure ? 1 : 0);
            webViewCookie.SetExpires(seconds < 0 ? -1.0d : seconds);
            cookieManager.AddOrUpdateCookie(webViewCookie);
        }
    }

    public void DeleteCookie(string name, string domain, string path)
    {
        if (TryGetWebView2() is ICoreWebView2_2 webView2)
        {
            var cookieManager = webView2.GetCookieManager();
            cookieManager.DeleteCookiesWithDomainAndPath(name, domain, path);
        }
    }

    public async Task<IReadOnlyList<Cookie>> GetCookiesAsync()
    {
        if (TryGetWebView2() is ICoreWebView2_2 webView2)
        {
            var cookieManager = webView2.GetCookieManager();
            var handler = new WebView2GetCookiesCompletedHandler();
            var unitEpoch = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);

            cookieManager.GetCookies(null, handler);
            var list = await handler.Result.Task;

            var cookies = new List<Cookie>();
            for (uint i = 0; i < list.GetCount(); i++)
            {
                var cookie = list.GetValueAtIndex(i);
                var seconds = cookie.GetExpires();
                cookies.Add(new Cookie(cookie.GetName(), cookie.GetValue(), cookie.GetPath(), cookie.GetDomain())
                {
                    Expires = seconds < 0.0 ?
                        DateTime.MinValue :
                        seconds * 10000000.0 + unitEpoch.Ticks > DateTime.MaxValue.Ticks ?
                            DateTime.MaxValue :
                            unitEpoch.AddSeconds(seconds),
                    HttpOnly = cookie.GetIsHttpOnly() == 1,
                    Secure = cookie.GetIsSecure() == 1,
                });
            }

            return cookies;
        }

        return [];
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _controller?.Close();
            _subscriptions?.Invoke();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~WebView2BaseAdapter()
    {
        Dispose(false);
    }

    unsafe IntPtr IWindowsWebView2PlatformHandle.CoreWebView2 => TryGetWebView2() is { } webView ?
        new(ComInterfaceMarshaller<ICoreWebView2>.ConvertToUnmanaged(webView)) :
        IntPtr.Zero;

    unsafe IntPtr IWindowsWebView2PlatformHandle.CoreWebView2Controller => _controller is not null ?
        new(ComInterfaceMarshaller<ICoreWebView2Controller>.ConvertToUnmanaged(_controller)) :
        IntPtr.Zero;
}
