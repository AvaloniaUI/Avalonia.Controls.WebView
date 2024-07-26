using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Variant;
using Windows.Win32.UI.Shell;
using Avalonia.Platform;

namespace AvaloniaUI.WebView.Win;

[SupportedOSPlatform("windows5.1.2600")]
internal unsafe class WebBrowserAdapter : IWebViewAdapter
{
    private static readonly Guid s_webBrowserGuid = Guid.Parse("8856f961-340a-11d0-a96b-00c04fd705a2");
    private readonly IWebBrowser2* _webBrowser;

    public WebBrowserAdapter()
    {
        var res = PInvoke.CoCreateInstance(s_webBrowserGuid, null, CLSCTX.CLSCTX_INPROC_SERVER, out IWebBrowser* browser);
        _ = res.ThrowOnFailure();

        res = browser->QueryInterface(IWebBrowser2.IID_Guid, out var browser2);
        _ = res.ThrowOnFailure();

        _ = browser->Release();
        _webBrowser = (IWebBrowser2*)browser2;
        Handle = new IntPtr(browser2);
    }

    public IntPtr Handle { get; }
    public string HandleDescriptor => "HWDN";
    public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public bool CanGoBack => true;
    public bool CanGoForward => true;
    public Uri Source { get => throw new NotImplementedException(); set => Navigate(value); }

    public bool GoBack()
    {
        _webBrowser->GoBack();
        return true;
    }

    public bool GoForward()
    {
        _webBrowser->GoForward();
        return true;
    }

    public Task<string?> InvokeScript(string script)
    {
        return Task.FromResult<string?>(null);
    }

    public void Navigate(Uri url)
    {
        var str = Marshal.StringToBSTR(url.AbsoluteUri);
        try
        {
            var emptyVar = new VARIANT();
            _webBrowser->Navigate(new BSTR((char*)str), emptyVar, null, null, null);
        }
        finally
        {
            Marshal.FreeBSTR(str);
        }
    }

    public void NavigateToString(string text)
    {
        // I don't want to spend my time on IDispatch
    }

    public bool Refresh()
    {
        _webBrowser->Refresh();
        return true;
    }

    public bool Stop()
    {
        _webBrowser->Stop();
        return true;
    }

    public void Dispose()
    {
        _ = _webBrowser->Release();
    }

    public event EventHandler? Initialized;
    public bool IsInitialized => true;

    public void SizeChanged()
    {
    }

    public void SetParent(IPlatformHandle parent)
    {
        if (parent.HandleDescriptor != "HWND")
        {
            throw new InvalidOperationException("IPlatformHandle.HandleDescriptor must be HWND");
        }

        _ = PInvoke.SetParent(new HWND(Handle), new HWND(parent.Handle));
    }
}
