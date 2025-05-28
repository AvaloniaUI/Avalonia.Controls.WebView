using System;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using Avalonia.Controls.Win.WebView1.Interop;

namespace Avalonia.Controls.Win.WebView1;

#if COM_SOURCE_GEN
[GeneratedComClass]
#endif
[SupportedOSPlatform("windows6.1")]
internal partial class WebViewCallbacks(WeakReference<WebView1Adapter> weakAdapter) : InspectableCallbackBase,
    IWebViewControlNavigationStartingHandler, IWebViewControlNavigationCompletedHandler
{
    public void Invoke(IntPtr sender, IWebViewControlNavigationStartingEventArgs e)
    {
        if (weakAdapter.TryGetTarget(out var adapter)
            && Uri.TryCreate(HStringInterop.FromIntPtr(e.get_Uri().get_AbsoluteUri()), UriKind.Absolute, out var uri))
        {
            var args = new WebViewNavigationStartingEventArgs { Request = uri };
            adapter.OnNavigationStarted(args);
            if (args.Cancel) e.put_Cancel(true);
        }
    }

    public void Invoke(IntPtr sender, IWebViewControlNavigationCompletedEventArgs e)
    {
        if (weakAdapter.TryGetTarget(out var adapter)
            && Uri.TryCreate(HStringInterop.FromIntPtr(e.get_Uri().get_AbsoluteUri()), UriKind.Absolute, out var uri))
        {
            adapter.OnNavigationCompleted(
                new WebViewNavigationCompletedEventArgs { Request = uri, IsSuccess = e.get_IsSuccess() });
        }
    }

    // public void Invoke(ICoreWebView2 sender, ICoreWebView2WebMessageReceivedEventArgs e)
    // {
    //     if (weakAdapter.TryGetTarget(out var adapter))
    //     {
    //         string? message = null;
    //
    //         try
    //         {
    //             // this `Try` method can throw undescriptive ArgumentException. Keep going WinRT.
    //             message = e.TryGetWebMessageAsString();
    //         }
    //         catch
    //         {
    //             // ignore
    //         }
    //
    //         message ??= e.WebMessageAsJson();
    //
    //         adapter.OnWebMessageReceived(new WebMessageReceivedEventArgs { Body = message });
    //     }
    // }
    //
    // public void Invoke(ICoreWebView2 sender, ICoreWebView2NewWindowRequestedEventArgs e)
    // {
    //     if (weakAdapter.TryGetTarget(out var adapter)
    //         && Uri.TryCreate(e.GetUri(), UriKind.Absolute, out var uri))
    //     {
    //         var args = new WebViewNewWindowRequestedEventArgs { Request = uri };
    //         adapter.OnNewWindowRequested(args);
    //         if (args.Handled) e.SetHandled(1);
    //     }
    // }
    protected override Guid[] GetIids() =>
    [
        typeof(IWebViewControlNavigationStartingHandler).GUID,
        typeof(IWebViewControlNavigationCompletedHandler).GUID
    ];
}
