using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Utils;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using static Avalonia.Controls.Gtk.GtkInterop;
using static Avalonia.Controls.Gtk.AvaloniaGtk;

namespace Avalonia.Controls.Gtk;

internal class GtkWebViewAdapter : IWebViewAdapterWithFocus, IGtkWebViewPlatformHandle
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

    private static readonly unsafe IntPtr s_scriptMessageReceivedCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)&ScriptMessagReceivedCallback);

    private static readonly unsafe IntPtr s_resourceLoadStartedCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, void>)&ResourceLoadStartedCallback);

    private GtkSignal? _loadChangedSignal;
    private GtkSignal? _decidePolicySignal;
    private GtkSignal? _focusInSignal;
    private GtkSignal? _focusOutSignal;
    private GtkSignal? _scriptMessageReceivedSignal;
    private GtkSignal? _resourceLoadStarted;
    private Uri _source = WebViewHelper.EmptyPage;

    public GtkWebViewAdapter(GtkWebViewEnvironmentRequestedEventArgs environmentArgs)
    {
        EnvironmentArgs = environmentArgs;
        RunOnGlibThread(() =>
        {
            InitializeSafe();
            IsInitialized = true;
        });
        // ReSharper disable once VirtualMemberCallInConstructor
        OnInitialized();
    }

    internal GtkWebViewEnvironmentRequestedEventArgs EnvironmentArgs { get; }

    public bool IsInitialized { get; private set; }
    public IntPtr WebViewHandle { get; private set; }

    IntPtr IPlatformHandle.Handle => WebViewHandle;
    string IPlatformHandle.HandleDescriptor => "WebKitWebView";

    public bool CanGoBack => RunOnGlibThread(() => webkit_web_view_can_go_back(WebViewHandle));
    public bool CanGoForward => RunOnGlibThread(() => webkit_web_view_can_go_forward(WebViewHandle));

    public Uri Source
    {
        get => _source;
        set => Navigate(value);
    }

    public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;
    public event EventHandler<WebViewNewWindowRequestedEventArgs>? NewWindowRequested;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;
    public event EventHandler? Initialized;

    public event EventHandler? GotFocus;
    public event EventHandler<IWebViewAdapterWithFocus.LostFocusDirection>? LostFocus;

    public bool Focus() => RunOnGlibThread(() =>
    {
        gtk_widget_grab_focus(WebViewHandle);
        return gtk_widget_has_focus(WebViewHandle);
    });

    public bool ResignFocus() => false;

    public bool GoBack()
    {
        if (!CanGoBack)
        {
            return false;
        }

        RunOnGlibThreadAsync(() => webkit_web_view_go_back(WebViewHandle));
        return true;
    }

    public bool GoForward()
    {
        if (!CanGoForward)
        {
            return false;
        }

        RunOnGlibThreadAsync(() => webkit_web_view_go_forward(WebViewHandle));
        return true;
    }

    public async Task<string?> InvokeScript(string script)
    {
        var tcs = new TaskCompletionSource<string?>();
        var gcHandle = GCHandle.Alloc(tcs);
        try
        {
            await RunOnGlibThreadAsync(() => webkit_web_view_run_javascript(
                WebViewHandle,
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
        await RunOnGlibThreadAsync(() => webkit_web_view_load_uri(WebViewHandle, url.ToString()));
    }

    public async void NavigateToString(string text)
    {
        await RunOnGlibThreadAsync(() => webkit_web_view_load_html(WebViewHandle, text, null));
    }

    public bool Refresh()
    {
        RunOnGlibThreadAsync(() => webkit_web_view_reload(WebViewHandle));
        return true;
    }

    public bool Stop()
    {
        RunOnGlibThreadAsync(() => webkit_web_view_stop_loading(WebViewHandle));
        return true;
    }

    public void SetParent(IPlatformHandle parent)
    {
    }

    public virtual Color DefaultBackground
    {
        set
        {
            // webkit_web_view_set_background_color (WebViewHandle, new GdkRGBA
            // {
            //     alpha = value.A /  255.0f,
            //     red = value.R /  255.0f,
            //     green = value.G /  255.0f,
            //     blue = value.B /  255.0f,
            // });
        }
    }

    public virtual void SizeChanged(PixelSize containerSize)
    {
    }

    protected virtual void InitializeSafe()
    {
        IntPtr context;
        if (EnvironmentArgs.EphemeralDataManager
                || EnvironmentArgs.BaseDataDirectory is { Length: >0}
                 || EnvironmentArgs.BaseCacheDirectory is { Length: >0}
                 || !EnvironmentArgs.SharedProcessModel
                 || EnvironmentArgs.DisableCache)
        {
            if (EnvironmentArgs.EphemeralDataManager)
            {
                context = webkit_web_context_new_ephemeral();
            }
            else if (EnvironmentArgs is { BaseDataDirectory.Length: > 0, BaseCacheDirectory.Length: > 0 })
            {
                context = webkit_web_context_new_with_website_data_manager(webkit_website_data_manager_new(
                    "base-data-directory", EnvironmentArgs.BaseDataDirectory,
                    "base-cache-directory", EnvironmentArgs.BaseCacheDirectory,
                    IntPtr.Zero));
            }
            else if (EnvironmentArgs.BaseDataDirectory is { Length: > 0 })
            {
                context = webkit_web_context_new_with_website_data_manager(webkit_website_data_manager_new(
                    "base-data-directory", EnvironmentArgs.BaseDataDirectory,
                    IntPtr.Zero));
            }
            else if (EnvironmentArgs.BaseCacheDirectory is { Length: > 0 })
            {
                context = webkit_web_context_new_with_website_data_manager(webkit_website_data_manager_new(
                    "base-data-directory", EnvironmentArgs.BaseCacheDirectory,
                    IntPtr.Zero));
            }
            else
            {
                context = webkit_web_context_new();
            }

            if (EnvironmentArgs.DisableCache)
            {
                webkit_web_context_set_cache_model(context, 0 /*WEBKIT_CACHE_MODEL_DOCUMENT_VIEWER*/);
            }
            if (!EnvironmentArgs.SharedProcessModel)
            {
                webkit_web_context_set_process_model(context, 1 /*WEBKIT_PROCESS_MODEL_MULTIPLE_SECONDARY_PROCESSES*/);
            }
        }
        else
        {
            context = webkit_web_context_get_default();
        }

        WebViewHandle = webkit_web_view_new_with_context(context);

        var contentManager = webkit_web_view_get_user_content_manager(WebViewHandle);
        _scriptMessageReceivedSignal = new GtkSignal(contentManager, $"script-message-received::{PostAvWebViewMessageName}", s_scriptMessageReceivedCallback, this);
        webkit_user_content_manager_register_script_message_handler(contentManager, PostAvWebViewMessageName);

        var script = webkit_user_script_new(
            $$"""
              function invokeCSharpAction(data)
              {
                var message = typeof data === 'object' ? JSON.stringify(data) : data;
                window.webkit.messageHandlers.{{PostAvWebViewMessageName}}.postMessage(message);
              }
              """,
            0, 0, IntPtr.Zero, IntPtr.Zero);
        webkit_user_content_manager_add_script(contentManager, script);

        g_object_ref_sink(WebViewHandle);

        var settings = webkit_web_view_get_settings(WebViewHandle);
        if (EnvironmentArgs.EnableDevTools)
        {
            webkit_settings_set_enable_developer_extras(settings, true);
        }
        if (EnvironmentArgs.ApplicationNameForUserAgent is { Length: > 0 } appUserAgent)
        {
            webkit_settings_set_user_agent_with_application_details(settings, appUserAgent, null);
        }

        _loadChangedSignal = new GtkSignal(WebViewHandle, "load-changed", s_loadChangedCallback, this);
        _decidePolicySignal = new GtkSignal(WebViewHandle, "decide-policy", s_decidePolicyCallback, this);
        _focusInSignal = new GtkSignal(WebViewHandle, "focus-in-event", s_focusInCallback, this);
        _focusOutSignal = new GtkSignal(WebViewHandle, "focus-out-event", s_focusOutCallback, this);
        _resourceLoadStarted = new GtkSignal(WebViewHandle, "resource-load-started", s_resourceLoadStartedCallback, this);
    }

    protected virtual void OnInitialized()
    {
        Initialized?.Invoke(this, EventArgs.Empty);
    }

    private Uri GetSourceUnsafe()
    {
        if (WebViewHandle == IntPtr.Zero)
        {
            return WebViewHelper.EmptyPage;
        }

        var uriPtr = webkit_web_view_get_uri(WebViewHandle);
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
                    Dispatcher.UIThread.Invoke(() => adapter.NavigationStarted?.Invoke(adapter, args));
                    return args.Cancel;
                }

                return false;
            // WEBKIT_POLICY_DECISION_TYPE_NEW_WINDOW_ACTION
            case 1:
                if (GetUrlFromPolicyDecision(decision) is { } winUrlStr &&
                    Uri.TryCreate(winUrlStr, UriKind.Absolute, out var winUrl))
                {
                    var args = new WebViewNewWindowRequestedEventArgs { Request = winUrl };
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
            case WebKitLoadEvent.Committed:
                adapter._source = adapter.GetSourceUnsafe();
                Dispatcher.UIThread.InvokeAsync(() => adapter.NavigationCompleted?
                    .Invoke(adapter,
                        new WebViewNavigationCompletedEventArgs
                        {
                            IsSuccess = true, Request = adapter._source
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
                tcs.SetResult(GetValueFromJsResult(jsResult));
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

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            adapter.LostFocus?.Invoke(adapter, IWebViewAdapterWithFocus.LostFocusDirection.Unknown);
        });
        return false;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe bool FocusInCallback(IntPtr widget, GdkEvent* gdkEvent, IntPtr data)
    {
        if (data == IntPtr.Zero || GCHandle.FromIntPtr(data).Target is not GtkWebViewAdapter adapter)
        {
            return false;
        }

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            adapter.GotFocus?.Invoke(adapter, EventArgs.Empty);
        });
        return false;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ScriptMessagReceivedCallback(IntPtr widget, IntPtr jsResult, IntPtr data)
    {
        if (data == IntPtr.Zero
            || GCHandle.FromIntPtr(data).Target is not GtkWebViewAdapter adapter
            || adapter.WebMessageReceived is null)
        {
            return;
        }

        var result = GetValueFromJsResult(jsResult);

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            adapter.WebMessageReceived?.Invoke(adapter,
                new WebMessageReceivedEventArgs { Body = result });
        });
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ResourceLoadStartedCallback(IntPtr widget, IntPtr resource, IntPtr request, IntPtr data)
    {
        if (data == IntPtr.Zero
            || GCHandle.FromIntPtr(data).Target is not GtkWebViewAdapter adapter
            || adapter.WebResourceRequested is null)
        {
            return;
        }

        var uriPtr = webkit_uri_request_get_uri(request);
        var uriString = Marshal.PtrToStringAuto(uriPtr);
        if (uriString is null)
        {
            return;
        }

        var headers = webkit_uri_request_get_http_headers(request);
        var headersWrapper = new NativeHeadersCollection(headers != IntPtr.Zero ?
            new SoupHttpHeaders(headers, false) : // seems like we can't mutate headers in this callback
            new DictionaryNativeHttpRequestHeaders(new Dictionary<string, string>()));
        var args = new WebResourceRequestedEventArgs
        {
            Request = new WebViewWebResourceRequest
            {
                Method = HttpMethod.Get, // that's not right, but let's keep it this way
                Uri = new Uri(uriString),
                Headers = headersWrapper
            }
        };

        // DANGEROUS - if user accesses some of the GTK webview APIs inside of this callback, they WILL get deadlock.
        // Because GTK threads is waiting for the sync UI Dispatcher call.
        // While some of the sync webview APIs would wait for the GTK thread to return value (like, get_Url).
        // TODO: what to do here? Can be replaced with InvokeAsync, but then headers won't be accessible 
        Dispatcher.UIThread.Invoke(() => adapter.WebResourceRequested?.Invoke(adapter, args));

        headersWrapper.Dispose();
    }

    private static string? GetValueFromJsResult(IntPtr jsResult)
    {
        var jsValue = webkit_javascript_result_get_js_value(jsResult);
        if (jsValue == IntPtr.Zero)
        {
            return null;
        }

        var p = jsc_value_to_string(jsValue);
        if (p == IntPtr.Zero)
        {
            return null;
        }

        return Marshal.PtrToStringAuto(p);
    }

    protected virtual void DisposeSafe(bool disposing)
    {
        if (disposing)
        {
            Interlocked.Exchange(ref _loadChangedSignal, null)?.Dispose();
            Interlocked.Exchange(ref _decidePolicySignal, null)?.Dispose();
            Interlocked.Exchange(ref _focusInSignal, null)?.Dispose();
            Interlocked.Exchange(ref _focusOutSignal, null)?.Dispose();
            Interlocked.Exchange(ref _scriptMessageReceivedSignal, null)?.Dispose();
            Interlocked.Exchange(ref _resourceLoadStarted, null)?.Dispose();
        }

        WebViewHandle = IntPtr.Zero;
    }

    public void Dispose()
    {
        RunOnGlibThreadAsync(() =>
        {
            DisposeSafe(true);
        });
        GC.SuppressFinalize(this);
    }

    ~GtkWebViewAdapter()
    {
        _ = RunOnGlibThreadAsync(() =>
        {
            DisposeSafe(false);
        });
    }

    IntPtr IGtkWebViewPlatformHandle.WebKitWebView => WebViewHandle;
}
