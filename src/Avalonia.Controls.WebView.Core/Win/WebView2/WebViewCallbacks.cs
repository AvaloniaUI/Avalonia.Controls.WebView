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
    ICoreWebView2NewWindowRequestedEventHandler, ICoreWebView2WebResourceRequestedEventHandler
{
    public void Invoke(ICoreWebView2 sender, ICoreWebView2NavigationStartingEventArgs e)
    {
        if (weakAdapter.TryGetTarget(out var adapter)
            && Uri.TryCreate(e.GetUri(), UriKind.Absolute, out var uri))
        {
            var args = new WebViewNavigationStartingEventArgs { Request = uri };
            adapter.OnNavigationStarted(args);
            if (args.Cancel) e.SetCancel(1);
        }
    }

    public void Invoke(ICoreWebView2 sender, ICoreWebView2NavigationCompletedEventArgs e)
    {
        if (weakAdapter.TryGetTarget(out var adapter))
        {
            adapter.OnNavigationCompleted(
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

            adapter.OnWebMessageReceived(new WebMessageReceivedEventArgs { Body = message });
        }
    }

    public void Invoke(ICoreWebView2 sender, ICoreWebView2NewWindowRequestedEventArgs e)
    {
        if (weakAdapter.TryGetTarget(out var adapter)
            && Uri.TryCreate(e.GetUri(), UriKind.Absolute, out var uri))
        {
            var args = new WebViewNewWindowRequestedEventArgs { Request = uri };
            adapter.OnNewWindowRequested(args);
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
                var request = new WebViewWebResourceRequest
                {
                    Headers = new NativeHeadersCollection(new WebView2NativeHttpRequestHeaders(nativeRequest.GetHeaders())),
                    Method = new HttpMethod(nativeRequest.GetMethod()),
                    Uri = uri
                };

                var args = new WebResourceRequestedEventArgs { Request = request };
                handler.Invoke(adapter, args);
            }
        }
    }
}
