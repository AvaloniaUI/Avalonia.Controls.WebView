#if DISABLE
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

using Windows.Web.UI;
using Windows.Web.UI.Interop;
using Avalonia.Platform;

namespace Avalonia.Controls.Win;

[SupportedOSPlatform("Windows10.0.17134")]
internal sealed class WebView1Adapter : IWebViewAdapter
{
    private static WebViewControlProcess? s_lazyProcess;
    private static int s_webViewsCount;
    
    private WebViewControl? _webViewControl;
    private Action? _subscriptions;

    public WebView1Adapter(IPlatformHandle handle)
    {
        Handle = handle.Handle;
        Initialize();

        Interlocked.Increment(ref s_webViewsCount);
    }
    
    public IntPtr Handle { get; }
    public string? HandleDescriptor => "HWDN";

    private async void Initialize()
    {
        var process = s_lazyProcess ??= CreateProcess();

        var control = await process.CreateWebViewControlAsync((long)Handle,
            new Windows.Foundation.Rect(0, 0, 100, 100));
        control.Settings.IsScriptNotifyAllowed = true;
        control.Settings.IsJavaScriptEnabled = true;

        _webViewControl = control;
        _webViewControl.IsVisible = true;
        SizeChanged();

        _subscriptions = AddHandlers(_webViewControl);

        IsInitialized = true;
        Initialized?.Invoke(this, EventArgs.Empty);
    }
    
    public bool IsInitialized { get; private set; }

    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public bool CanGoBack => _webViewControl?.CanGoBack ?? false;

    public bool CanGoForward => _webViewControl?.CanGoForward ?? false;

    public Uri Source
    {
        [return: MaybeNull] get
        {
            return _webViewControl?.Source!;
        }
        set
        {
            Navigate(value);
        }
    }

    public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;
    public event EventHandler? Initialized;

    public bool GoBack()
    {
        if (!CanGoBack) return false;
        _webViewControl?.GoBack();
        return true;
    }

    public bool GoForward()
    {
        if (!CanGoForward) return false;
        _webViewControl?.GoForward();
        return true;
    }

    public Task<string?> InvokeScript(string script)
    {
        return _webViewControl?.InvokeScriptAsync("eval", new[] { script }).AsTask() ?? Task.FromResult<string?>(null);
    }

    public void Navigate(Uri url)
    {
        if (_webViewControl is not null)
        {
            _webViewControl.Source = url;
        }
    }

    public void NavigateToString(string text)
    {
        _webViewControl?.NavigateToString(text);
    }

    public bool Refresh()
    {
        _webViewControl?.Refresh();
        return true;
    }

    public bool Stop()
    {
        _webViewControl?.Stop();
        return true;
    }

    public void SizeChanged()
    {
        WinApiHelpers.GetWindowRect(Handle, out var rect);

        if (_webViewControl is not null)
        {
            _webViewControl.Bounds = new Windows.Foundation.Rect(0, 0, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }
    }
    
    private static WebViewControlProcess CreateProcess()
    {
        var options = new WebViewControlProcessOptions();
        options.PrivateNetworkClientServerCapability = WebViewControlProcessCapabilityState.Enabled;
        return new WebViewControlProcess(options);
    }

    private Action AddHandlers(WebViewControl webView)
    {
        webView.NavigationStarting += WebViewOnNavigationStarting;
        void WebViewOnNavigationStarting(object? sender, WebViewControlNavigationStartingEventArgs e)
        {
            var args = new WebViewNavigationStartingEventArgs { Request = e.Uri };
            NavigationStarted?.Invoke(this, args);
            if (args.Cancel)
            {
                e.Cancel = true;
            }
        }

        webView.NavigationCompleted += WebViewOnNavigationCompleted;
        async void WebViewOnNavigationCompleted(object? sender, WebViewControlNavigationCompletedEventArgs e)
        {
            await InvokeScript("function invokeCSharpAction(data){window.external.notify(data);}");
            
            NavigationCompleted?.Invoke(this, new WebViewNavigationCompletedEventArgs
            {
                Request = ((WebViewControl)sender!).Source,
                IsSuccess = e.IsSuccess
            });
        }
        
        webView.ScriptNotify += WebViewOnScriptNotify;
        void WebViewOnScriptNotify(IWebViewControl sender, WebViewControlScriptNotifyEventArgs args)
        {
            WebMessageReceived?.Invoke(this, new WebMessageReceivedEventArgs { Body = args.Value });
        }

        return () =>
        {
            webView.NavigationStarting -= WebViewOnNavigationStarting;
            webView.NavigationCompleted -= WebViewOnNavigationCompleted;
            webView.ScriptNotify -= WebViewOnScriptNotify;
        };
    }

    public void Dispose()
    {
        var webViewsCount = Interlocked.Decrement(ref s_webViewsCount);

        _subscriptions?.Invoke();

        _webViewControl?.Close();
        _webViewControl = null;

        if (s_lazyProcess is { } process && webViewsCount == 0)
        {
            s_lazyProcess = null;
        }
    }
}
#endif
