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

namespace Avalonia.Controls.Win;

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

    public Uri Source
    {
        get => _webBrowser->LocationURL.ToString() is { Length:> 0 } str ? new Uri(str) : WebViewHelper.EmptyPage;
        set => Navigate(value);
    }

    public bool GoBack()
    {
        try
        {
            _webBrowser->GoBack();
            return true;
        }
        catch (Exception ex) when (!IsCriticalException(ex))
        {
            return false;
        }
    }

    public bool GoForward()
    {
        try
        {
            _webBrowser->GoForward();
            return true;
        }
        catch (Exception ex) when (!IsCriticalException(ex))
        {
            return false;
        }
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
            var uriVariant = new VARIANT
            {
                Anonymous = new VARIANT._Anonymous_e__Union
                {
                    Anonymous = new VARIANT._Anonymous_e__Union._Anonymous_e__Struct
                    {
                        vt = VARENUM.VT_BSTR,
                        Anonymous = new VARIANT._Anonymous_e__Union._Anonymous_e__Struct._Anonymous_e__Union
                        {
                            bstrVal = new BSTR((char*)str)
                        }
                    }
                }
            };

            var flagsVariant = new VARIANT
            {
                Anonymous = new VARIANT._Anonymous_e__Union
                {
                    Anonymous = new VARIANT._Anonymous_e__Union._Anonymous_e__Struct
                    {
                        vt = VARENUM.VT_BOOL,
                        Anonymous = new VARIANT._Anonymous_e__Union._Anonymous_e__Struct._Anonymous_e__Union
                        {
                            boolVal = VARIANT_BOOL.VARIANT_FALSE // newWindow = false
                        }
                    }
                }
            };

            var nullVariant = new VARIANT
            {
                Anonymous = new VARIANT._Anonymous_e__Union
                {
                    Anonymous = new VARIANT._Anonymous_e__Union._Anonymous_e__Struct
                    {
                        vt = VARENUM.VT_NULL
                    }
                }
            };

            _webBrowser->Navigate2(uriVariant, flagsVariant, nullVariant, nullVariant, nullVariant);

        }
        catch (COMException ce)
        {
            if ((uint)unchecked(ce.ErrorCode) != unchecked(0x800704c7))
            {
                // "the operation was canceled by the user" - navigation failed
                // ignore this error, IE has already alerted the user.
                throw;
            }
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
        try
        {
            _webBrowser->Refresh();
            return true;
        }
        catch (Exception ex) when (!IsCriticalException(ex))
        {
            return false;
        }
    }

    public bool Stop()
    {
        try
        {
            _webBrowser->Stop();
            return true;
        }
        catch (Exception ex) when (!IsCriticalException(ex))
        {
            return false;
        }
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

    // That's what WinForms uses https://github.com/dotnet/winforms/blob/main/src/System.Private.Windows.Core/src/System/ExceptionExtensions.cs#L12
    // But we likely always have COMException, but that's an issue for another day. 
    private static bool IsCriticalException(Exception ex)
        => ex is NullReferenceException
            or StackOverflowException
            or OutOfMemoryException
            or IndexOutOfRangeException
            or AccessViolationException;
}
