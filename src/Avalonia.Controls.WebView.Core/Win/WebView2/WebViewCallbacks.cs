using System;
using System.Net.Http;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using Avalonia.Controls.Utils;
using Avalonia.Controls.Win.WebView2.Interop;

namespace Avalonia.Controls.Win.WebView2;

#if COM_SOURCE_GEN
[GeneratedComClass]
#endif
[SupportedOSPlatform("windows6.1")]
internal partial class WebViewCallbacks(WeakReference<WebView2BaseAdapter> weakAdapter) : ICoreWebView2NavigationStartingEventHandler,
    ICoreWebView2NavigationCompletedEventHandler, ICoreWebView2WebMessageReceivedEventHandler,
    ICoreWebView2NewWindowRequestedEventHandler, ICoreWebView2WebResourceRequestedEventHandler,
    ICoreWebView2MoveFocusRequestedEventHandler, ICoreWebView2FocusChangedEventHandler
{
    public void Invoke(ICoreWebView2 sender, ICoreWebView2NavigationStartingEventArgs e)
    {
        if (weakAdapter.TryGetTarget(out var adapter)
            && adapter.GetNavigationStarted() is { } handler
            && Uri.TryCreate(e.GetUri(), UriKind.Absolute, out var uri))
        {
            var args = new WebViewNavigationStartingEventArgs { Request = uri };
            handler.Invoke(adapter, args);
            if (args.Cancel) e.SetCancel(1);
        }
    }

    public void Invoke(ICoreWebView2 sender, ICoreWebView2NavigationCompletedEventArgs e)
    {
        if (weakAdapter.TryGetTarget(out var adapter)
            && adapter.GetNavigationCompleted() is { } handler)
        {
            handler.Invoke(adapter,
                new WebViewNavigationCompletedEventArgs
                {
                    Request = new Uri(sender.GetSource()), IsSuccess = e.GetIsSuccess() == 1
                });
        }
    }

    public void Invoke(ICoreWebView2 sender, ICoreWebView2WebMessageReceivedEventArgs e)
    {
        if (weakAdapter.TryGetTarget(out var adapter)
            && adapter.GetWebMessageReceived() is { } handler)
        {
            // this `Try` method can throw undescriptive ArgumentException. Keep going WinRT.
            if (e.TryGetWebMessageAsString(out var message) != 0)
            {
                message = e.WebMessageAsJson();
            }

            handler.Invoke(adapter, new WebMessageReceivedEventArgs { Body = message });
        }
    }

    public void Invoke(ICoreWebView2 sender, ICoreWebView2NewWindowRequestedEventArgs e)
    {
        if (weakAdapter.TryGetTarget(out var adapter)
            && adapter.GetNewWindowRequested() is { } handler
            && Uri.TryCreate(e.GetUri(), UriKind.Absolute, out var uri))
        {
            var args = new WebViewNewWindowRequestedEventArgs { Request = uri };
            handler.Invoke(adapter, args);
            if (args.Handled) e.SetHandled(1);
        }
    }

    public void Invoke(ICoreWebView2 sender, ICoreWebView2WebResourceRequestedEventArgs e)
    {
        if (weakAdapter.TryGetTarget(out var adapter)
            && adapter.GetWebResourceRequested() is { } handler)
        {
            var nativeRequest = e.GetRequest();
            if (Uri.TryCreate(nativeRequest.GetUri(), UriKind.Absolute, out var uri))
            {
                var headersWrapper = new NativeHeadersCollection(new WebView2NativeHttpRequestHeaders(nativeRequest.GetHeaders()));
                var request = new WebViewWebResourceRequest
                {
                    Headers = headersWrapper,
                    Method = new HttpMethod(nativeRequest.GetMethod()),
                    Uri = uri
                };

                var args = new WebResourceRequestedEventArgs { Request = request };
                handler.Invoke(adapter, args);
                headersWrapper.Dispose();
            }
        }
    }

    public void Invoke(ICoreWebView2Controller sender, ICoreWebView2MoveFocusRequestedEventArgs e)
    {
        if (weakAdapter.TryGetTarget(out var adapter)
            && adapter.GetLostFocus() is { } handler)
        {
            var reason = e.GetReason();
            handler.Invoke(adapter, reason switch
            {
                COREWEBVIEW2_MOVE_FOCUS_REASON.COREWEBVIEW2_MOVE_FOCUS_REASON_NEXT => IWebViewAdapterWithFocus.LostFocusDirection.Next,
                COREWEBVIEW2_MOVE_FOCUS_REASON.COREWEBVIEW2_MOVE_FOCUS_REASON_PREVIOUS => IWebViewAdapterWithFocus.LostFocusDirection.Previous,
                _ => IWebViewAdapterWithFocus.LostFocusDirection.Unknown
            });
        }
    }

    void ICoreWebView2FocusChangedEventHandler.Invoke(ICoreWebView2Controller sender, IntPtr args)
    {
        if (weakAdapter.TryGetTarget(out var adapter)
            && adapter.GetGotFocus() is { } handler)
        {
            handler.Invoke(adapter, EventArgs.Empty);
        }
    }
}
