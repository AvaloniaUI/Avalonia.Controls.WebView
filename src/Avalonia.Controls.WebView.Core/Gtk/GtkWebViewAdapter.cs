using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Threading;
using static Avalonia.Controls.Gtk.GtkInterop;
using static Avalonia.Controls.Gtk.AvaloniaGtk;

namespace Avalonia.Controls.Gtk;

internal class GtkWebViewAdapter : IWebViewAdapter, IWebViewAdapterWithFocus
{
    private const string PostAvWebViewMessageName = "postAvWebViewMessage";

    internal enum WebKitLoadEvent
    {
        Started,
        Redirected,
        Committed,
        Finished
    }

    private static readonly unsafe IntPtr s_decidePolicyCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, IntPtr, bool>)&DecidePolicy);

    private static readonly unsafe IntPtr s_loadChangedCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, WebKitLoadEvent, IntPtr, void>)&LoadChanged);

    private static readonly unsafe IntPtr s_scriptCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)&InvokeScriptCallback);

    private static readonly unsafe IntPtr s_focusInCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, GdkEvent*, IntPtr, bool>)&FocusInCallback);

    private static readonly unsafe IntPtr s_focusOutCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, GdkEvent*, IntPtr, bool>)&FocusOutCallback);

    private GtkSignal? _loadChangedSignal;
    private GtkSignal? _decidePolicySignal;
    private GtkSignal? _focusInSignal;
    private GtkSignal? _focusOutSignal;

    public GtkWebViewAdapter()
    {
        RunOnGlibThread(() =>
        {
            Handle = webkit_web_view_new();

            _loadChangedSignal = new GtkSignal(Handle, "load-changed", s_loadChangedCallback, this);
            _decidePolicySignal = new GtkSignal(Handle, "decide-policy", s_decidePolicyCallback, this);
            _focusInSignal = new GtkSignal(Handle, "focus-in-event", s_focusInCallback, this);
            _focusOutSignal = new GtkSignal(Handle, "focus-out-event", s_focusOutCallback, this);
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
    public event EventHandler<WebViewNewWindowRequestedEventArgs>? NewWindowRequested;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public event EventHandler? Initialized;

    public event EventHandler? GotFocus;
    public event EventHandler? LostFocus;

    public bool Focus()
    {
        gtk_widget_grab_focus(Handle);
        return gtk_widget_has_focus(Handle);
    }

    public bool ResignFocus() => false;

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
            await RunOnGlibThreadAsync(() => webkit_web_view_run_javascript(
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

    public async void Navigate(Uri url)
    {
        await RunOnGlibThreadAsync(() => webkit_web_view_load_uri(Handle, url.ToString()));
    }

    public async void NavigateToString(string text)
    {
        await RunOnGlibThreadAsync(() => webkit_web_view_load_html(Handle, text, null));
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

    public virtual void SizeChanged(PixelSize containerSize)
    {
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
                if (GetUrlFromPolicyDecision(decision) is { } urlStr &&
                    Uri.TryCreate(urlStr, UriKind.Absolute, out var url))
                {
                    var args = new WebViewNavigationStartingEventArgs { Request = url };
                    Dispatcher.UIThread.InvokeAsync(() => adapter.NavigationStarted?.Invoke(adapter, args));
                    return args.Cancel;
                }

                return false;
            // WEBKIT_POLICY_DECISION_TYPE_NEW_WINDOW_ACTION
            case 1:
                if (GetUrlFromPolicyDecision(decision) is { } winUrlStr &&
                    Uri.TryCreate(winUrlStr, UriKind.Absolute, out var winUrl))
                {
                    var args = new WebViewNewWindowRequestedEventArgs() { Request = winUrl };
                    Dispatcher.UIThread.Invoke(() => adapter.NewWindowRequested?.Invoke(adapter, args));
                    return args.Handled;
                }

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

                Dispatcher.UIThread.InvokeAsync(() => adapter.NavigationCompleted?
                    .Invoke(adapter,
                        new WebViewNavigationCompletedEventArgs
                        {
                            IsSuccess = true, Request = adapter.GetSourceUnsafe()
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

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe bool FocusOutCallback(IntPtr widget, GdkEvent* gdkEvent, IntPtr data)
    {
        if (data == IntPtr.Zero || GCHandle.FromIntPtr(data).Target is not GtkWebViewAdapter adapter)
        {
            return false;
        }

        adapter.LostFocus?.Invoke(adapter, EventArgs.Empty);
        return false;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe bool FocusInCallback(IntPtr widget, GdkEvent* gdkEvent, IntPtr data)
    {
        if (data == IntPtr.Zero || GCHandle.FromIntPtr(data).Target is not GtkWebViewAdapter adapter)
        {
            return false;
        }

        adapter.GotFocus?.Invoke(adapter, EventArgs.Empty);
        return false;
    }

    protected virtual void Dispose(bool disposing)
    {
        var widgetHandle = Handle;
        if (widgetHandle != IntPtr.Zero)
        {
            Handle = IntPtr.Zero;

            if (disposing)
            {
                _loadChangedSignal?.Dispose();
                _decidePolicySignal?.Dispose();
                _focusInSignal?.Dispose();
                _focusOutSignal?.Dispose();
            }

            gtk_widget_destroy(widgetHandle);
        }
    }

    public void Dispose()
    {
        RunOnGlibThread(() =>
        {
            Dispose(true);
        });
        GC.SuppressFinalize(this);
    }

    ~GtkWebViewAdapter()
    {
        _ = RunOnGlibThreadAsync(() =>
        {
            Dispose(false);
        });
    }
}
