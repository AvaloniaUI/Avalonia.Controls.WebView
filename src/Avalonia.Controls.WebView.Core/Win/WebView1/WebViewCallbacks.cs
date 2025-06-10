using System;
using System.Net.Http;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using Avalonia.Controls.Utils;
using Avalonia.Controls.Win.WebView1.Interop;
using Avalonia.Logging;

namespace Avalonia.Controls.Win.WebView1;

#if COM_SOURCE_GEN
[GeneratedComClass]
#endif
[SupportedOSPlatform("windows6.1")]
internal partial class WebViewCallbacks(WeakReference<WebView1Adapter> weakAdapter) : InspectableCallbackBase,
    IWebViewControlNavigationStartingHandler, IWebViewControlNavigationCompletedHandler,
    IWebViewControlScriptNotifyHandler, IWebViewControlNewWindowRequestedHandler,
    IWebViewControlWebResourceRequestedHandler
{
    public void Invoke(IntPtr sender, IWebViewControlNavigationStartingEventArgs e)
    {
        if (weakAdapter.TryGetTarget(out var adapter)
            && adapter.GetNavigationStarted() is { } handler
            && Uri.TryCreate(HStringInterop.FromIntPtr(e.get_Uri().get_AbsoluteUri()), UriKind.Absolute, out var uri))
        {
            var args = new WebViewNavigationStartingEventArgs { Request = uri };
            handler.Invoke(adapter, args);
            if (args.Cancel) e.put_Cancel(true);
        }
    }

    public void Invoke(IntPtr sender, IWebViewControlNavigationCompletedEventArgs e)
    {
        if (weakAdapter.TryGetTarget(out var adapter)
            && adapter.GetNavigationCompleted() is { } handler
            && Uri.TryCreate(HStringInterop.FromIntPtr(e.get_Uri().get_AbsoluteUri()), UriKind.Absolute, out var uri))
        {
            InitScript();

            handler.Invoke(adapter,
                new WebViewNavigationCompletedEventArgs { Request = uri, IsSuccess = e.get_IsSuccess() });
        }

        // Ideally should be using AddInitializeScript.
        // But I couldn't get it working for some reason. It's simply not executed.
        async void InitScript()
        {
            try
            {
                var initScript =
                    """
                    window.invokeCSharpAction = function(data) {
                        var message = typeof data === 'object' ? JSON.stringify(data) : data;
                        window.external.notify(message);
                    };
                    """;
                await adapter.InvokeScript(initScript);
            }
            catch (Exception ex)
            {
                Logger.TryGet(LogEventLevel.Warning, "WebView")?.Log(adapter,
                    "`invokeCSharpAction` script initialization on page loading failed.\r\n{Exception}", ex);
            }
        }
    }

    public void Invoke(IntPtr sender, IWebViewControlScriptNotifyEventArgs e)
    {
        if (weakAdapter.TryGetTarget(out var adapter)
            && adapter.GetWebMessageReceived() is { } handler)
        {
            var value = e.get_Value();
            var message = HStringInterop.FromIntPtr(value);

            handler.Invoke(adapter, new WebMessageReceivedEventArgs { Body = message });
        }
    }

    public void Invoke(IntPtr sender, IWebViewControlNewWindowRequestedEventArgs e)
    {
        if (weakAdapter.TryGetTarget(out var adapter)
            && adapter.GetNewWindowRequested() is { } handler
            && Uri.TryCreate(HStringInterop.FromIntPtr(e.get_Uri().get_AbsoluteUri()), UriKind.Absolute, out var uri))
        {
            var args = new WebViewNewWindowRequestedEventArgs { Request = uri };
            handler.Invoke(adapter, args);
            if (args.Handled) e.put_Handled(true);
        }
    }

    public void Invoke(IntPtr sender, IWebViewControlWebResourceRequestedEventArgs e)
    {
        if (weakAdapter.TryGetTarget(out var adapter)
            && adapter.GetWebResourceRequested() is { } handler)
        {
            var nativeRequest = e.GetRequest();
            if (Uri.TryCreate(HStringInterop.FromIntPtr(nativeRequest.GetRequestUri().get_AbsoluteUri()), UriKind.Absolute, out var uri))
            {
                var request = new WebViewWebResourceRequest
                {
                    Headers = new NativeHeadersCollection(new WebView1NativeHttpRequestHeaders(nativeRequest.GetHeaders())),
                    Method = new HttpMethod(HStringInterop.FromIntPtr(nativeRequest.GetMethod().Method())!),
                    Uri = uri
                };

                var args = new WebResourceRequestedEventArgs { Request = request };
                handler.Invoke(adapter, args);
            }
        }
    }

    protected override Guid[] GetIids() => s_iids;

    private static readonly Guid[] s_iids =
    [
        typeof(IWebViewControlNavigationStartingHandler).GUID,
        typeof(IWebViewControlNavigationCompletedHandler).GUID,
        typeof(IWebViewControlScriptNotifyHandler).GUID,
        typeof(IWebViewControlNewWindowRequestedHandler).GUID,
        typeof(IWebViewControlWebResourceRequestedHandler).GUID
    ];
}
