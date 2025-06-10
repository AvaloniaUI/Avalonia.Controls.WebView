using System;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Win.WebView1.Interop;
using Avalonia.Controls.Win.WebView2;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Controls.Win.WebView1;

[SupportedOSPlatform("windows6.1")]
internal sealed class WebView1Adapter : IWebViewAdapter, IWindowsWebView1PlatformHandle
{
    private static IWebViewControlProcess? s_lazyProcess;
    private static int s_webViewsCount;

    private IWebViewControl? _webViewControl;
    private IWebViewControlSite? _webViewControlSite;
    private Action? _subscriptions;

    public static bool IsAvailable
    {
        get
        {
            try
            {
                return (s_lazyProcess ??= CreateProcess()) is not null;
            }
            catch
            {
                return false;
            }
        }
    }

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

        if (!PInvoke.GetWindowRect(new HWND(Handle), out var rect))
            rect = RECT.FromXYWH(0, 0, 100, 100);

        var operation = process.CreateWebViewControl((long)Handle,
            new winrtRect { Height = rect.Width, Width = rect.Height });
        var handler = new WebViewControlHandler();
        operation.put_Completed(handler);

        var control = await handler.Task;

        if (control.get_Settings() is { } settings)
        {
            settings.put_IsJavaScriptEnabled(true);
            settings.put_IsScriptNotifyAllowed(true);
        }

        // Doesn't work for some reason.
        // Instead injecting script in the NavigationCompleted
        // if (control is IWebViewControl2 control2)
        // {
        //      var initScript =
        //         new HStringInterop("""
        //                            window.invokeCSharpAction = function(data) {
        //                                var message = typeof data === 'object' ? JSON.stringify(data) : data;
        //                                window.external.notify(message);
        //                            };
        //                            """);
        //     control2.AddInitializeScript(initScript.Handle);
        // }

        _webViewControl = control;
        // ReSharper disable once SuspiciousTypeConversion.Global
        // IWebViewControlSite can be queried from the IWebViewControl
        _webViewControlSite = (IWebViewControlSite)control;
        
        _webViewControlSite.put_IsVisible(true);
        SizeChanged(default);

        _subscriptions = AddHandlers(_webViewControl);

        IsInitialized = true;
        Initialized?.Invoke(this, EventArgs.Empty);
    }

    public bool IsInitialized { get; private set; }

    public event EventHandler? Initialized;
    public event EventHandler<WebViewNewWindowRequestedEventArgs>? NewWindowRequested;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;

    public bool CanGoBack => _webViewControl?.get_CanGoBack() ?? false;

    public bool CanGoForward => _webViewControl?.get_CanGoForward() ?? false;

    public Uri Source
    {
        get
        {
            var absoluteUri = _webViewControl?.get_Source()?.get_AbsoluteUri();
            return absoluteUri.HasValue
                   && Uri.TryCreate(HStringInterop.FromIntPtr(absoluteUri.Value), UriKind.Absolute, out var uri)
                ? uri : WebViewHelper.EmptyPage;
        }
        set
        {
            Navigate(value);
        }
    }

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

    public async Task<string?> InvokeScript(string script)
    {
        if (_webViewControl is null)
            return null;

        using var args = new HStringIterator([script]);
        using var command = new HStringInterop("eval");

        var operation = _webViewControl.InvokeScriptAsync(command.Handle, args);
        var handler = new HStringResultHandler();
        operation.put_Completed(handler);

        return await handler.Task;
    }

    public void Navigate(Uri url)
    {
        var factory = NativeWinRTMethods.CreateActivationFactory<IUriRuntimeClassFactory>("Windows.Foundation.Uri");
        using var hstring = new HStringInterop(url.AbsoluteUri);
        var uri = factory?.CreateUri(hstring.Handle);
        if (uri is not null)
            _webViewControl?.put_Source(uri);
    }

    public void NavigateToString(string text)
    {
        using var hstring = new HStringInterop(text);
        _webViewControl?.NavigateToString(hstring.Handle);
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

    public void SizeChanged(PixelSize containerSize)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (PInvoke.GetWindowRect(new HWND(Handle), out var rect)
                && _webViewControlSite is not null)
            {
                _webViewControlSite.put_Bounds(new winrtRect
                {
                    Width = rect.Width,
                    Height = rect.Height
                });
            }
        });
    }

    public void SetParent(IPlatformHandle parent)
    {
        if (parent.HandleDescriptor != "HWND")
            throw new InvalidOperationException("IPlatformHandle.HandleDescriptor must be HWND");

        PInvoke.SetParent(new HWND(Handle), new HWND(parent.Handle));
    }

    internal void OnNavigationStarted(WebViewNavigationStartingEventArgs args) => NavigationStarted?.Invoke(this, args);
    internal void OnNavigationCompleted(WebViewNavigationCompletedEventArgs args) => NavigationCompleted?.Invoke(this, args);
    internal void OnWebMessageReceived(WebMessageReceivedEventArgs args) => WebMessageReceived?.Invoke(this, args);
    internal void OnNewWindowRequested(WebViewNewWindowRequestedEventArgs args) => NewWindowRequested?.Invoke(this, args);

    private static IWebViewControlProcess CreateProcess()
    {
        var options = NativeWinRTMethods.CreateInstance<IWebViewControlProcessOptions>("Windows.Web.UI.Interop.WebViewControlProcessOptions")
            ?? throw new InvalidOperationException("Unable to create WebViewControlProcessOptions.");
        options.put_PrivateNetworkClientServerCapability(WebViewControlProcessCapabilityState.Enabled);
        var factory = NativeWinRTMethods.CreateActivationFactory<IWebViewControlProcessFactory>("Windows.Web.UI.Interop.WebViewControlProcess")
            ?? throw new InvalidOperationException("Unable to create WebViewControlProcess.");
        return factory.CreateWithOptions(options);
    }

    private Action AddHandlers(IWebViewControl webView)
    {
        var callbacks = new WebViewCallbacks(new WeakReference<WebView1Adapter>(this));
        webView.add_NavigationStarting(callbacks, out var token1);
        webView.add_NavigationCompleted(callbacks, out var token2);
        webView.add_ScriptNotify(callbacks, out var token3);
        webView.add_NewWindowRequested(callbacks, out var token4);

        return () =>
        {
            webView.remove_NavigationStarting(token1);
            webView.remove_NavigationCompleted(token2);
            webView.remove_ScriptNotify(token3);
            webView.remove_NewWindowRequested(token4);
        };
    }

    public void Dispose()
    {
        var webViewsCount = Interlocked.Decrement(ref s_webViewsCount);

        _subscriptions?.Invoke();

        if (_webViewControlSite is not null)
        {
            _webViewControlSite.Close();
            _webViewControlSite = null;
        }

        _webViewControl = null;

        if (s_lazyProcess is { } process && webViewsCount == 0)
        {
            s_lazyProcess = null;
        }
    }

    unsafe IntPtr IWindowsWebView1PlatformHandle.WebViewControl => _webViewControl is not null ?
        new(ComInterfaceMarshaller<IWebViewControl>.ConvertToUnmanaged(_webViewControl)) :
        IntPtr.Zero;
}
