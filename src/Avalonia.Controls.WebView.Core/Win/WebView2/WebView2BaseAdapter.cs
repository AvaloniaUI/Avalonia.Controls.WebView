using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Avalonia.Controls.Utils;
using Avalonia.Controls.Win.WebView2.Interop;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Controls.Win.WebView2;

[SupportedOSPlatform("windows6.1")] // win7
internal abstract partial class WebView2BaseAdapter : IWebViewAdapterWithCookieManager
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
        return Task.FromResult<string?>(null);
        //return TryGetWebView2()?.ExecuteScriptAsync(scriptName) ?? Task.FromResult<string?>(null);
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

    private async void Initialize(IPlatformHandle parentHost)
    {
        var env = await CoreWebView2Environment.CreateAsync();
        var controller = await CreateWebView2Controller(env, parentHost.Handle);
        var webView = controller.GetCoreWebView2();

        //webView.AddScriptToExecuteOnDocumentCreated(
          //  "function invokeCSharpAction(data){window.chrome.webview.postMessage(data);}", IntPtr.Zero);
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
        var callbacks = new Callbacks(new WeakReference<WebView2BaseAdapter>(this));
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

#if COM_SOURCE_GEN
    [GeneratedComClass]
#endif
    private partial class Callbacks(WeakReference<WebView2BaseAdapter> weakAdapter) : ICoreWebView2NavigationStartingEventHandler,
        ICoreWebView2NavigationCompletedEventHandler, ICoreWebView2WebMessageReceivedEventHandler,
        ICoreWebView2NewWindowRequestedEventHandler
    {
        public void Invoke(ICoreWebView2 sender, ICoreWebView2NavigationStartingEventArgs e)
        {
            if (weakAdapter.TryGetTarget(out var adapter)
                && Uri.TryCreate(e.GetUri(), UriKind.Absolute, out var uri))
            {
                var args = new WebViewNavigationStartingEventArgs { Request = uri };
                adapter.NavigationStarted?.Invoke(this, args);
                if (args.Cancel) e.SetCancel(1);
            }
        }

        public void Invoke(ICoreWebView2 sender, ICoreWebView2NavigationCompletedEventArgs e)
        {
            if (weakAdapter.TryGetTarget(out var adapter))
            {
                adapter.NavigationCompleted?.Invoke(this,
                    new WebViewNavigationCompletedEventArgs
                    {
                        Request = new Uri(sender.GetSource()), IsSuccess = e.GetIsSuccess() == 1
                    });
            }
        }

        public void Invoke(ICoreWebView2 sender, ICoreWebView2WebMessageReceivedEventArgs e)
        {
            if (weakAdapter.TryGetTarget(out var adapter))
            {
                string? message = null;

                try
                {
                    // this `Try` method can throw undescriptive ArgumentException. Keep going WinRT.
                    message = e.TryGetWebMessageAsString();
                }
                catch
                {
                    // ignore
                }

                message ??= e.WebMessageAsJson();

                adapter.WebMessageReceived?.Invoke(this, new WebMessageReceivedEventArgs { Body = message });
            }
        }

        public void Invoke(ICoreWebView2 sender, ICoreWebView2NewWindowRequestedEventArgs e)
        {
            if (weakAdapter.TryGetTarget(out var adapter)
                && Uri.TryCreate(e.GetUri(), UriKind.Absolute, out var uri))
            {
                var args = new WebViewNewWindowRequestedEventArgs { Request = uri };
                adapter.NewWindowRequested?.Invoke(this, args);
                if (args.Handled) e.SetHandled(1);
            }
        }
    }

    public void AddOrUpdateCookie(Cookie cookie)
    {
        if (TryGetWebView2() is { } webView)
        {
            // var webViewCookie = webView.CookieManager.CreateCookieWithSystemNetCookie(cookie);
            // webView.CookieManager.AddOrUpdateCookie(webViewCookie);
        }
    }

    public void DeleteCookie(string name, string domain, string path)
    {
        //TryGetWebView2()?.CookieManager.DeleteCookiesWithDomainAndPath(name, domain, path);
    }

    public Task<IReadOnlyList<Cookie>> GetCookiesAsync()
    {
        return Task.FromResult<IReadOnlyList<Cookie>>([]);
        // if (TryGetWebView2() is not { } webView)
        //     return [];
        // var cookies = await webView.CookieManager.GetCookiesAsync(null);
        // return cookies.Select(c => c.ToSystemNetCookie()).ToArray();
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
}
