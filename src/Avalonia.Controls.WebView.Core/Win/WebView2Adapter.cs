#if NET6_0_OR_GREATER || NETFRAMEWORK
using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Avalonia.Platform;
using Microsoft.Web.WebView2.Core;

namespace Avalonia.Controls.Win;

[SupportedOSPlatform("windows6.1")] // win7
internal class WebView2Adapter : IWebViewAdapter
{
    private CoreWebView2Controller? _controller;
    private Action? _subscriptions;

    public WebView2Adapter(IPlatformHandle handle)
    {
        Handle = handle.Handle;

        Initialize();
    }

    public IntPtr Handle { get; }
    public string HandleDescriptor => "HWDN";

    public bool IsInitialized { get; private set; }

    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public bool CanGoBack => _controller?.CoreWebView2?.CanGoBack ?? false;

    public bool CanGoForward => _controller?.CoreWebView2?.CanGoForward ?? false;

    public Uri Source
    {
        get
        {
            return Uri.TryCreate(_controller?.CoreWebView2?.Source, UriKind.Absolute, out var url) ? url : null!;
        }
        set => _controller?.CoreWebView2?.Navigate(value.AbsoluteUri);
    }

    public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;
    public event EventHandler? Initialized;

    public void Dispose()
    {
        _subscriptions?.Invoke();
        _controller?.Close();
    }

    public bool GoBack()
    {
        _controller?.CoreWebView2.GoBack();
        return true;
    }

    public bool GoForward()
    {
        _controller?.CoreWebView2.GoForward();
        return true;
    }

    public Task<string?> InvokeScript(string scriptName)
    {
        return _controller?.CoreWebView2?.ExecuteScriptAsync(scriptName) ?? Task.FromResult<string?>(null);
    }

    public void Navigate(Uri url)
    {
        _controller?.CoreWebView2?.Navigate(url.AbsoluteUri);
    }

    public void NavigateToString(string text)
    {
        _controller?.CoreWebView2?.NavigateToString(text);
    }

    public bool Refresh()
    {
        _controller?.CoreWebView2?.Reload();
        return true;
    }

    public bool Stop()
    {
        _controller?.CoreWebView2?.Stop();
        return true;
    }

    public void SizeChanged()
    {
        if (PInvoke.GetWindowRect(new HWND(Handle), out var rect)
            && _controller is not null)
        {
            _controller.BoundsMode = CoreWebView2BoundsMode.UseRawPixels;
            _controller.Bounds = new Rectangle(0, 0, rect.Width, rect.Height);
        }
    }

    public void SetParent(IPlatformHandle parent)
    {
        if (parent.HandleDescriptor != "HWND")
            throw new InvalidOperationException("IPlatformHandle.HandleDescriptor must be HWND");

        _ = PInvoke.SetParent(new HWND(Handle), new HWND(parent.Handle));
    }

    private async void Initialize()
    {
        var env = await CoreWebView2Environment.CreateAsync();
        var controller = await env.CreateCoreWebView2ControllerAsync(Handle);
        await controller.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
            "function invokeCSharpAction(data){window.chrome.webview.postMessage(data);}");

        controller.IsVisible = true;

        _controller = controller;

        SizeChanged();

        _subscriptions = AddHandlers(_controller.CoreWebView2);

        IsInitialized = true;
        Initialized?.Invoke(this, EventArgs.Empty);
    }

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
            WebMessageReceived?.Invoke(this,
                new WebMessageReceivedEventArgs { Body = e.TryGetWebMessageAsString() ?? e.WebMessageAsJson });
        }

        return () =>
        {
            webView.NavigationStarting -= WebViewOnNavigationStarting;
            webView.NavigationCompleted -= WebViewOnNavigationCompleted;
            webView.WebMessageReceived -= WebViewOnWebMessageReceived;
        };
    }
}
#endif
