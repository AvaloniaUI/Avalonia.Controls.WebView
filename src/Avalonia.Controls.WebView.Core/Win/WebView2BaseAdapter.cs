#if !ANDROID && (NET6_0_OR_GREATER || NETFRAMEWORK)
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Avalonia.Platform;
using Avalonia.Threading;
using Microsoft.Web.WebView2.Core;

namespace Avalonia.Controls.Win;

[SupportedOSPlatform("windows6.1")] // win7
internal abstract class WebView2BaseAdapter : IWebViewAdapterWithCookieManager
{
    private CoreWebView2Controller? _controller;
    private Action? _subscriptions;

    protected WebView2BaseAdapter(IPlatformHandle parent)
    {
        Initialize(parent);
    }

    public abstract IntPtr Handle { get; }
    public abstract string? HandleDescriptor { get; }

    protected CoreWebView2? TryGetWebView2()
    {
        try
        {
            return _controller?.CoreWebView2;
        }
        catch (InvalidOperationException ex)
        {
            // That's what WPF control does.
            if (ex.InnerException?.HResult == -2147019873)
            {
                return null;
            }

            throw;
        }
    }

    public bool IsInitialized { get; private set; }

    public bool CanGoBack => TryGetWebView2()?.CanGoBack ?? false;

    public bool CanGoForward => TryGetWebView2()?.CanGoForward ?? false;

    public Uri Source
    {
        get
        {
            return Uri.TryCreate(TryGetWebView2()?.Source, UriKind.Absolute, out var url) ? url : null!;
        }
        set => TryGetWebView2()?.Navigate(value.AbsoluteUri);
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
        return TryGetWebView2()?.ExecuteScriptAsync(scriptName) ?? Task.FromResult<string?>(null);
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
                _controller.BoundsMode = CoreWebView2BoundsMode.UseRawPixels;
                _controller.Bounds = new Rectangle(0, 0, rect.Width, rect.Height);
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

        _controller.ParentWindow = parent.Handle;
    }

    private async void Initialize(IPlatformHandle parentHost)
    {
        var env = await CoreWebView2Environment.CreateAsync();
        var controller = await CreateWebView2Controller(env, parentHost.Handle);
        var webView = controller.CoreWebView2;
        await webView.AddScriptToExecuteOnDocumentCreatedAsync(
            "function invokeCSharpAction(data){window.chrome.webview.postMessage(data);}");
        controller.IsVisible = true;
        controller.ShouldDetectMonitorScaleChanges = false;
        _controller = controller;

        SizeChanged(default);

        _subscriptions = AddHandlers(webView);

        IsInitialized = true;
        Initialized?.Invoke(this, EventArgs.Empty);
    }

    protected abstract Task<CoreWebView2Controller> CreateWebView2Controller(CoreWebView2Environment env, IntPtr handle);

    private Action AddHandlers(CoreWebView2 webView)
    {
        webView.NavigationStarting += WebViewOnNavigationStarting;

        void WebViewOnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
            {
                var args = new WebViewNavigationStartingEventArgs { Request = uri };
                NavigationStarted?.Invoke(this, args);
                if (args.Cancel) e.Cancel = true;
            }
        }

        webView.NavigationCompleted += WebViewOnNavigationCompleted;

        void WebViewOnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            NavigationCompleted?.Invoke(this,
                new WebViewNavigationCompletedEventArgs
                {
                    Request = new Uri(((CoreWebView2)sender!).Source),
                    IsSuccess = e.IsSuccess
                });
        }

        webView.WebMessageReceived += WebViewOnWebMessageReceived;

        void WebViewOnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
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

            message ??= e.WebMessageAsJson;

            WebMessageReceived?.Invoke(this, new WebMessageReceivedEventArgs { Body = message });
        }

        webView.NewWindowRequested += WebViewOnNewWindowRequested;
        
        void WebViewOnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
            {
                var args = new WebViewNewWindowRequestedEventArgs { Request = uri };
                NewWindowRequested?.Invoke(this, args);
                if (args.Handled) e.Handled = true;
            }
        }

        return () =>
        {
            webView.NavigationStarting -= WebViewOnNavigationStarting;
            webView.NavigationCompleted -= WebViewOnNavigationCompleted;
            webView.WebMessageReceived -= WebViewOnWebMessageReceived;
        };
    }

    public void AddOrUpdateCookie(Cookie cookie)
    {
        if (TryGetWebView2() is { } webView)
        {
            var webViewCookie = webView.CookieManager.CreateCookieWithSystemNetCookie(cookie);
            webView.CookieManager.AddOrUpdateCookie(webViewCookie);
        }
    }

    public void DeleteCookie(string name, string domain, string path)
    {
        TryGetWebView2()?.CookieManager.DeleteCookiesWithDomainAndPath(name, domain, path);
    }

    public async Task<IReadOnlyList<Cookie>> GetCookiesAsync()
    {
        if (TryGetWebView2() is not { } webView)
            return [];
        var cookies = await webView.CookieManager.GetCookiesAsync(null);
        return cookies.Select(c => c.ToSystemNetCookie()).ToArray();
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
#endif
