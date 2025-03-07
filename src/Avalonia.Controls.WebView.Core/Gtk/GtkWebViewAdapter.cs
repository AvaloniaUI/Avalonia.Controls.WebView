using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Threading;
using static Avalonia.Controls.Gtk.GtkInterop;
using static Avalonia.Controls.Gtk.AvaloniaGtk;

namespace Avalonia.Controls.Gtk;

internal class GtkWebViewAdapter : IWebViewAdapter
{
    private const string PostAvWebViewMessageName = "postAvWebViewMessage";

    internal enum WebKitLoadEvent
    {
        Started,
        Redirected,
        Committed,
        Finished
    }

    private static readonly unsafe IntPtr s_decidePolicyCallback = new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, IntPtr, bool>)&DecidePolicy);
    private static readonly unsafe IntPtr s_loadChangedCallback = new((delegate* unmanaged[Cdecl]<IntPtr, WebKitLoadEvent, IntPtr, void>)&LoadChanged);
    private static readonly unsafe IntPtr s_scriptCallback = new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)&InvokeScriptCallback);

    private bool _isDisposed;
    private GtkSignal? _loadChangedSignal;
    private GtkSignal? _decidePolicySignal;

    public GtkWebViewAdapter()
    {
        RunOnGlibThread(() =>
        {
            Handle = webkit_web_view_new();

            _loadChangedSignal = new GtkSignal(Handle, "load-changed", s_loadChangedCallback, this);
            _decidePolicySignal = new GtkSignal(Handle, "decide-policy", s_decidePolicyCallback, this);
        });
    }

    public bool IsInitialized => true;
    public IntPtr Handle { get; private set; }
    public string HandleDescriptor => "WebKitWebView";

    public bool CanGoBack => RunOnGlibThread(() => webkit_web_view_can_go_back(Handle));
    public bool CanGoForward => RunOnGlibThread(() => webkit_web_view_can_go_forward(Handle));

    public Uri Source
    {
        get => RunOnGlibThread(GetSourceUnsafe);
        set => Navigate(value);
    }

    public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public event EventHandler? Initialized;

    public bool GoBack()
    {
        if (!CanGoBack)
        {
            return false;
        }

        RunOnGlibThread(() => webkit_web_view_go_back(Handle));
        return true;
    }

    public bool GoForward()
    {
        if (!CanGoForward)
        {
            return false;
        }

        RunOnGlibThread(() => webkit_web_view_go_forward(Handle));
        return true;
    }

    public async Task<string?> InvokeScript(string script)
    {
        var tcs = new TaskCompletionSource<string?>();
        var gcHandle = GCHandle.Alloc(tcs);
        try
        {
            RunOnGlibThread(() => webkit_web_view_run_javascript(
                Handle,
                script,
                IntPtr.Zero,
                s_scriptCallback,
                GCHandle.ToIntPtr(gcHandle)));
            return await tcs.Task;
        }
        finally
        {
            gcHandle.Free();
        }
    }

    public void Navigate(Uri url)
    {
        RunOnGlibThread(() => webkit_web_view_load_uri(Handle, url.ToString()));
    }

    public void NavigateToString(string text)
    {
        RunOnGlibThread(() => webkit_web_view_load_html(Handle, text, null));
    }

    public bool Refresh()
    {
        RunOnGlibThread(() => webkit_web_view_reload(Handle));
        return true;
    }

    public bool Stop()
    {
        RunOnGlibThread(() => webkit_web_view_stop_loading(Handle));
        return true;
    }

    public void SetParent(IPlatformHandle parent)
    {
    }

    public void SizeChanged()
    {
        // GTK handles sizing automatically through its container system
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        if (Handle != IntPtr.Zero)
        {
            RunOnGlibThread(() =>
            {
                _loadChangedSignal?.Dispose();
                _decidePolicySignal?.Dispose();
                gtk_widget_destroy(Handle);
            });
            Handle = IntPtr.Zero;
        }

        _isDisposed = true;
    }

    private Uri GetSourceUnsafe()
    {
        var uriPtr = webkit_web_view_get_uri(Handle);
        if (uriPtr == IntPtr.Zero)
        {
            return WebViewHelper.EmptyPage;
        }

        var uriString = Marshal.PtrToStringAuto(uriPtr);
        return uriString != null ? new Uri(uriString) : WebViewHelper.EmptyPage;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static bool DecidePolicy(IntPtr webView, IntPtr decision, int type, IntPtr data)
    {
        if (data == IntPtr.Zero || GCHandle.FromIntPtr(data).Target is not GtkWebViewAdapter adapter)
        {
            return false;
        }

        switch (type)
        {
            // WEBKIT_POLICY_DECISION_TYPE_NAVIGATION_ACTION
            case 0:
                if (GetUrlFromPolicyDecision(decision) is { } urlStr && Uri.TryCreate(urlStr, UriKind.Absolute, out var url))
                {
                    var args = new WebViewNavigationStartingEventArgs { Request = url };
                    Dispatcher.UIThread.Invoke(() => adapter.NavigationStarted?.Invoke(adapter, args));
                    return args.Cancel;
                }
                return false;
            // WEBKIT_POLICY_DECISION_TYPE_NEW_WINDOW_ACTION
            case 1:
                return false;
            default:
                return true;
        }

        static string? GetUrlFromPolicyDecision(IntPtr decision)
        {
            var navigationAction = webkit_navigation_policy_decision_get_navigation_action(decision);
            if (navigationAction == IntPtr.Zero)
            {
                return null;
            }

            var request = webkit_navigation_action_get_request(navigationAction);
            if (request == IntPtr.Zero)
            {
                return null;
            }

            var uriPtr = webkit_uri_request_get_uri(request);
            if (uriPtr == IntPtr.Zero)
            {
                return null;
            }

            return Marshal.PtrToStringAuto(uriPtr);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void LoadChanged(IntPtr webView, WebKitLoadEvent loadEvent, IntPtr data)
    {
        if (data == IntPtr.Zero || GCHandle.FromIntPtr(data).Target is not GtkWebViewAdapter adapter)
        {
            return;
        }

        switch (loadEvent)
        {
            case WebKitLoadEvent.Finished:
                _ = adapter.InvokeScript(
                    $"function invokeCSharpAction(data){{window.webkit.messageHandlers.{PostAvWebViewMessageName}.postMessage(data);}}");

                Dispatcher.UIThread.Invoke(() => adapter.NavigationCompleted?
                    .Invoke(adapter, new WebViewNavigationCompletedEventArgs
                    {
                        IsSuccess = true,
                        Request = adapter.GetSourceUnsafe()
                    }));
                break;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void InvokeScriptCallback(IntPtr webView, IntPtr result, IntPtr data)
    {
        if (data == IntPtr.Zero || GCHandle.FromIntPtr(data).Target is not TaskCompletionSource<string?> tcs)
        {
            return;
        }

        GError* err;
        var jsResult = webkit_web_view_run_javascript_finish(webView, result, &err);
        if (jsResult == IntPtr.Zero)
        {
            if (err != null)
            {
                try
                {
                    var exception = Marshal.PtrToStringAuto(err->Message);
                    tcs.SetException(new Exception(exception));
                }
                finally
                {
                    g_error_free(err);
                }
            }
        }
        else
        {
            try
            {
                var jsValue = webkit_javascript_result_get_js_value(jsResult);
                if (jsValue == IntPtr.Zero)
                {
                    tcs.SetResult(null);
                }

                var p = jsc_value_to_string(jsValue);
                if (p == IntPtr.Zero)
                {
                    tcs.SetResult(null);
                }

                tcs.SetResult(Marshal.PtrToStringAuto(p));
            }
            finally
            {
                webkit_javascript_result_unref(jsResult);
            }
        }
    }
}
