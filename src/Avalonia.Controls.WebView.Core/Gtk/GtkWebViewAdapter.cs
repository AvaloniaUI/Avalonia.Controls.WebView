using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Utils;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Platform;
using static Avalonia.Controls.Gtk.GtkInterop;
using static Avalonia.Controls.Gtk.AvaloniaGtk;

namespace Avalonia.Controls.Gtk;

internal abstract class GtkWebViewAdapter : IWebViewAdapterWithFocus, IGtkWebViewPlatformHandle, IWebViewWithPrintWithOptions
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
        new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, IntPtr, int>)&DecidePolicy);

    private static readonly unsafe IntPtr s_loadChangedCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, int, IntPtr, void>)&LoadChanged);

    private static readonly unsafe IntPtr s_scriptCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)&InvokeScriptCallback);

    private static readonly unsafe IntPtr s_focusInCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, GdkEvent*, IntPtr, int>)&FocusInCallback);

    private static readonly unsafe IntPtr s_focusOutCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, GdkEvent*, IntPtr, int>)&FocusOutCallback);

    private static readonly unsafe IntPtr s_scriptMessageReceivedCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)&ScriptMessageReceivedCallback);

    private static readonly unsafe IntPtr s_resourceLoadStartedCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, void>)&ResourceLoadStartedCallback);

    private GtkSignal? _loadChangedSignal;
    private GtkSignal? _decidePolicySignal;
    private GtkSignal? _focusInSignal;
    private GtkSignal? _focusOutSignal;
    private GtkSignal? _scriptMessageReceivedSignal;
    private GtkSignal? _resourceLoadStarted;
    private Uri _source = WebViewHelper.EmptyPage;

    protected GtkWebViewAdapter(GtkWebViewEnvironmentRequestedEventArgs args)
    {
        IntPtr context;
        if (args.EphemeralDataManager
            || args.BaseDataDirectory is { Length: >0}
            || args.BaseCacheDirectory is { Length: >0}
            || !args.SharedProcessModel
            || args.DisableCache)
        {
            if (args.EphemeralDataManager)
            {
                context = webkit_web_context_new_ephemeral();
            }
            else if (args is { BaseDataDirectory.Length: > 0, BaseCacheDirectory.Length: > 0 })
            {
                context = webkit_web_context_new_with_website_data_manager(webkit_website_data_manager_new(
                    "base-data-directory", args.BaseDataDirectory,
                    "base-cache-directory", args.BaseCacheDirectory,
                    IntPtr.Zero));
            }
            else if (args.BaseDataDirectory is { Length: > 0 })
            {
                context = webkit_web_context_new_with_website_data_manager(webkit_website_data_manager_new(
                    "base-data-directory", args.BaseDataDirectory,
                    IntPtr.Zero));
            }
            else if (args.BaseCacheDirectory is { Length: > 0 })
            {
                context = webkit_web_context_new_with_website_data_manager(webkit_website_data_manager_new(
                    "base-data-directory", args.BaseCacheDirectory,
                    IntPtr.Zero));
            }
            else
            {
                context = webkit_web_context_new();
            }

            if (args.DisableCache)
            {
                webkit_web_context_set_cache_model(context, 0 /*WEBKIT_CACHE_MODEL_DOCUMENT_VIEWER*/);
            }
            if (!args.SharedProcessModel)
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
        if (args.EnableDevTools)
        {
            webkit_settings_set_enable_developer_extras(settings, true);
        }
        if (args.ApplicationNameForUserAgent is { Length: > 0 } appUserAgent)
        {
            webkit_settings_set_user_agent_with_application_details(settings, appUserAgent, null);
        }

        _loadChangedSignal = new GtkSignal(WebViewHandle, "load-changed", s_loadChangedCallback, this);
        _decidePolicySignal = new GtkSignal(WebViewHandle, "decide-policy", s_decidePolicyCallback, this);
        _focusInSignal = new GtkSignal(WebViewHandle, "focus-in-event", s_focusInCallback, this);
        _focusOutSignal = new GtkSignal(WebViewHandle, "focus-out-event", s_focusOutCallback, this);
        _resourceLoadStarted = new GtkSignal(WebViewHandle, "resource-load-started", s_resourceLoadStartedCallback, this);
    }

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

    public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;
    public event EventHandler<WebViewNewWindowRequestedEventArgs>? NewWindowRequested;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;
    public event EventHandler? GotFocus;
    public event EventHandler<IWebViewAdapterWithFocus.LostFocusDirection>? LostFocus;

    public void Focus() => RunOnGlibThreadAsync(() =>
    {
        gtk_widget_grab_focus(WebViewHandle);
        return gtk_widget_has_focus(WebViewHandle);
    });

    public void ResignFocus() { }

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
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var gcHandle = GCHandle.Alloc(tcs);
        try
        {
            await RunOnGlibThreadAsync(() => webkit_web_view_run_javascript(
                WebViewHandle,
                script,
                IntPtr.Zero,
                s_scriptCallback,
                GCHandle.ToIntPtr(gcHandle)))
                .ConfigureAwait(false);
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            gcHandle.Free();
        }
    }

    public async void Navigate(Uri url)
    {
        await RunOnGlibThreadAsync(() => webkit_web_view_load_uri(WebViewHandle, url.ToString())).ConfigureAwait(false);
    }

    public async void NavigateToString(string text)
    {
        await RunOnGlibThreadAsync(() => webkit_web_view_load_html(WebViewHandle, text, null)).ConfigureAwait(false);
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

    public virtual void SetParent(IPlatformHandle parent)
    {
    }

    public bool ShowPrintUI()
    {
        RunOnGlibThreadAsync(() =>
        {
            using var operation = new GtkPrintOperation(WebViewHandle);
            operation.RunDialog(IntPtr.Zero);
        });
        return true;
    }

    public Task<Stream> PrintToPdfStreamAsync(WebViewPrintSettings settings) => PrintToPdfStreamAsyncInternal(settings);
    public Task<Stream> PrintToPdfStreamAsync() => PrintToPdfStreamAsyncInternal(null);

    private async Task<Stream> PrintToPdfStreamAsyncInternal(WebViewPrintSettings? settings)
    {
        var tempFile = Path.GetTempFileName();
        GtkPrintOperation? operation = null; 
        try
        {
            await RunOnGlibThreadAsync(() =>
            {
                operation = new GtkPrintOperation(WebViewHandle);

                if (settings is not null)
                {
                    operation.ApplySettings(settings);
                }
                
                operation.PrintToFile(tempFile);
            }).ConfigureAwait(false);

            await operation!.Task.ConfigureAwait(false);

#if NET6_0_OR_GREATER
            await
#endif
            using var stream = File.OpenRead(tempFile);
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }
        finally
        {
            File.Delete(tempFile);
            if (operation is not null)
            {
                await RunOnGlibThreadAsync(() =>
                {
                    operation.Dispose();
                });
            }
        }
    }

    public virtual void SizeChanged(PixelSize containerSize)
    {
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
    private static int DecidePolicy(IntPtr webView, IntPtr decision, int type, IntPtr data)
    {
        if (!GtkSignal.TryGetState<GtkWebViewAdapter>(data, out var adapter))
        {
            return False;
        }

        switch (type)
        {
            // WEBKIT_POLICY_DECISION_TYPE_NAVIGATION_ACTION
            case 0:
                if (adapter.NavigationStarted is { } startedHandler &&
                    GetUrlFromPolicyDecision(decision) is { } urlStr &&
                    Uri.TryCreate(urlStr, UriKind.Absolute, out var url))
                {
                    var args = new WebViewNavigationStartingEventArgs { Request = url };
                    WebViewDispatcher.Invoke(() => startedHandler.Invoke(adapter, args));
                    return args.Cancel ? True : False;
                }

                return False;
            // WEBKIT_POLICY_DECISION_TYPE_NEW_WINDOW_ACTION
            case 1:
                if (adapter.NewWindowRequested is { } windowHandler &&
                    GetUrlFromPolicyDecision(decision) is { } winUrlStr &&
                    Uri.TryCreate(winUrlStr, UriKind.Absolute, out var winUrl))
                {
                    var args = new WebViewNewWindowRequestedEventArgs { Request = winUrl };
                    WebViewDispatcher.Invoke(() => windowHandler.Invoke(adapter, args));
                    return args.Handled ? True : False;
                }

                return False;
            default:
                return True;
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
    private static void LoadChanged(IntPtr webView, int loadEvent, IntPtr data)
    {
        if (!GtkSignal.TryGetState<GtkWebViewAdapter>(data, out var adapter)
            || adapter.NavigationCompleted is not { } handler)
        {
            return;
        }

        switch ((WebKitLoadEvent)loadEvent)
        {
            case WebKitLoadEvent.Committed:
                adapter._source = adapter.GetSourceUnsafe();
                WebViewDispatcher.InvokeAsync(() => handler.Invoke(adapter,
                    new WebViewNavigationCompletedEventArgs { IsSuccess = true, Request = adapter._source }));
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
    private static unsafe int FocusOutCallback(IntPtr widget, GdkEvent* gdkEvent, IntPtr data)
    {
        if (!GtkSignal.TryGetState<GtkWebViewAdapter>(data, out var adapter)
            || adapter.LostFocus is not { } handler)
        {
            return False;
        }

        WebViewDispatcher.InvokeAsync(() =>
        {
            handler.Invoke(adapter, IWebViewAdapterWithFocus.LostFocusDirection.Unknown);
        });
        return False;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int FocusInCallback(IntPtr widget, GdkEvent* gdkEvent, IntPtr data)
    {
        if (!GtkSignal.TryGetState<GtkWebViewAdapter>(data, out var adapter)
            || adapter.GotFocus is not { } handler)
        {
            return False;
        }

        WebViewDispatcher.InvokeAsync(() =>
        {
            handler.Invoke(adapter, EventArgs.Empty);
        });
        return False;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ScriptMessageReceivedCallback(IntPtr widget, IntPtr jsResult, IntPtr data)
    {
        if (!GtkSignal.TryGetState<GtkWebViewAdapter>(data, out var adapter)
            || adapter.WebMessageReceived is not { } handler)
        {
            return;
        }

        var result = GetValueFromJsResult(jsResult);

        WebViewDispatcher.InvokeAsync(() =>
        {
            handler.Invoke(adapter, new WebMessageReceivedEventArgs { Body = result });
        });
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ResourceLoadStartedCallback(IntPtr widget, IntPtr resource, IntPtr request, IntPtr data)
    {
        if (!GtkSignal.TryGetState<GtkWebViewAdapter>(data, out var adapter)
            || adapter.WebResourceRequested is not { } handler)
        {
            return;
        }

        var uriPtr = webkit_uri_request_get_uri(request);
        var uriString = Marshal.PtrToStringAuto(uriPtr);
        if (uriString is null)
        {
            return;
        }

        INativeHttpRequestHeaders headersImpl;
        if (HasSoup3)
        {
            var headers = webkit_uri_request_get_http_headers(request);
            headersImpl = headers != IntPtr.Zero ?
                new Soup3HttpHeaders(headers, false) : // seems like we can't mutate headers in this callback
                DictionaryNativeHttpRequestHeaders.ImmutableInstance;
        }
        else
        {
            Logger.TryGet(LogEventLevel.Verbose, "WebView")?.Log(null,
                "LibSoup3.0 is not available, header information won't be read");
            headersImpl = DictionaryNativeHttpRequestHeaders.ImmutableInstance;
        }

        var headersWrapper = new NativeHeadersCollection(headersImpl);
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
        WebViewDispatcher.Invoke(() => handler.Invoke(adapter, args));

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
    
    internal static DetailedWebViewAdapterInfo GetWebKitGtkInfo(WebViewEmbeddingScenario scenarios = WebViewEmbeddingScenario.NativeDialog)
    {
        if (!OperatingSystemEx.IsLinux())
        {
            return WebViewAdapterInfo.PlatformNotSupported(WebViewAdapterType.WebKitGtk);
        }

        var version = AvaloniaGtk.TryGetVersion();

        return new DetailedWebViewAdapterInfo(
            WebViewAdapterType.WebKitGtk,
            WebViewEngine.WebKit,
            IsSupported: true,
            IsInstalled: version is not null,
            Version: version?.ToString(),
            UnavailableReason: version is not null ? null : "WebKitGtk library is not installed. Install webkit2gtk 4.0+ package.",
            SupportedScenarios: version is not null ? scenarios : WebViewEmbeddingScenario.None);
    }
}
