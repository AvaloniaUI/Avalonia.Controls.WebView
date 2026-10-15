using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;
using Avalonia.Controls.Gtk;
using Avalonia.Controls.Utils;
using static Avalonia.Controls.Gtk.GtkInterop;

namespace Avalonia.Controls.Linux;

/// <summary>
/// The adapter that answers custom scheme requests of a web view.
/// </summary>
internal interface IWebKitUriSchemeTarget
{
    /// <summary>
    /// Raises WebResourceRequested on the UI thread and waits for its deferrals.
    /// </summary>
    Task<WebViewWebResourceResponse?> RaiseUriSchemeRequestAsync(WebViewWebResourceRequest request);
}

/// <summary>
/// Serves <see cref="WebViewEnvironmentRequestedEventArgs.CustomSchemes"/> for WebKitGTK and WPE WebKit.
/// </summary>
/// <remarks>
/// Both libraries share the API names, so the WebKit calls are resolved from the library in use.
/// Scheme handlers belong to the WebKitWebContext, which web views may share,
/// so requests are routed to the requesting web view, for the schemes it declared. Needs WebKit 2.36 or later.
/// </remarks>
internal sealed class WebKitUriSchemeHandler
{
    private const string LibWpeWebKit = "libWPEWebKit-2.0.so.1";
    private const string ErrorDomain = "avalonia-webview";
    private const string RegisteredSchemeKeyPrefix = "avalonia-webview-scheme:";
    private const int SoupMessageHeadersResponse = 1;

    private static readonly unsafe IntPtr s_callback =
        new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)&OnRequest);
    private static readonly unsafe IntPtr s_headerCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)&OnHeader);

    private static readonly Lazy<WebKitUriSchemeHandler> s_gtk = new(() => new WebKitUriSchemeHandler(
        AvaloniaGtk.LoadLibrary(GtkInterop.LibWebKit), static action => AvaloniaGtk.RunOnGlibThreadAsync(action)));

    // WPE dispatches GLib on the UI thread.
    private static readonly Lazy<WebKitUriSchemeHandler> s_wpe = new(() => new WebKitUriSchemeHandler(
        NativeLibrary.Load(LibWpeWebKit),
        static action => WebViewDispatcher.Current.InvokeAsyncHandler(action)));

    private readonly Func<Action, Task> _runOnGlibThread;
    private readonly IntPtr _self;
    private readonly object _lock = new();
    private readonly Dictionary<IntPtr, (WeakReference<IWebKitUriSchemeTarget> Target, string[] Schemes)> _targets = [];

    private readonly unsafe delegate* unmanaged[Cdecl]<IntPtr> _getDefaultContext;
    private readonly unsafe delegate* unmanaged[Cdecl]<IntPtr, byte*, IntPtr, IntPtr, IntPtr, void> _registerUriScheme;
    private readonly unsafe delegate* unmanaged[Cdecl]<IntPtr, IntPtr> _getSecurityManager;
    private readonly unsafe delegate* unmanaged[Cdecl]<IntPtr, byte*, void> _registerAsSecure;
    private readonly unsafe delegate* unmanaged[Cdecl]<IntPtr, byte*, void> _registerAsCorsEnabled;
    private readonly unsafe delegate* unmanaged[Cdecl]<IntPtr, IntPtr> _requestGetUri;
    private readonly unsafe delegate* unmanaged[Cdecl]<IntPtr, IntPtr> _requestGetWebView;
    private readonly unsafe delegate* unmanaged[Cdecl]<IntPtr, GError*, void> _requestFinishError;
    private readonly unsafe delegate* unmanaged[Cdecl]<IntPtr, IntPtr> _requestGetHttpMethod;
    private readonly unsafe delegate* unmanaged[Cdecl]<IntPtr, IntPtr> _requestGetHttpHeaders;
    private readonly unsafe delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> _requestFinishWithResponse;
    private readonly unsafe delegate* unmanaged[Cdecl]<IntPtr, long, IntPtr> _responseNew;
    private readonly unsafe delegate* unmanaged[Cdecl]<IntPtr, uint, byte*, void> _responseSetStatus;
    private readonly unsafe delegate* unmanaged[Cdecl]<IntPtr, byte*, void> _responseSetContentType;
    private readonly unsafe delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> _responseSetHttpHeaders;
    private readonly unsafe delegate* unmanaged[Cdecl]<int, IntPtr> _headersNew;
    private readonly unsafe delegate* unmanaged[Cdecl]<IntPtr, byte*, byte*, void> _headersAppend;
    private readonly unsafe delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void> _headersForeach;

    private unsafe WebKitUriSchemeHandler(IntPtr library, Func<Action, Task> runOnGlibThread)
    {
        _runOnGlibThread = runOnGlibThread;
        // Registers the resolver for the GLib and GIO names of GtkInterop.
        RuntimeHelpers.RunClassConstructor(typeof(AvaloniaGtk).TypeHandle);
        // Lives as long as the process, like the registrations.
        _self = GCHandle.ToIntPtr(GCHandle.Alloc(this));

        _getDefaultContext = (delegate* unmanaged[Cdecl]<IntPtr>)
            Export(library, "webkit_web_context_get_default");
        _registerUriScheme = (delegate* unmanaged[Cdecl]<IntPtr, byte*, IntPtr, IntPtr, IntPtr, void>)
            Export(library, "webkit_web_context_register_uri_scheme");
        _getSecurityManager = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr>)
            Export(library, "webkit_web_context_get_security_manager");
        _registerAsSecure = (delegate* unmanaged[Cdecl]<IntPtr, byte*, void>)
            Export(library, "webkit_security_manager_register_uri_scheme_as_secure");
        _registerAsCorsEnabled = (delegate* unmanaged[Cdecl]<IntPtr, byte*, void>)
            Export(library, "webkit_security_manager_register_uri_scheme_as_cors_enabled");
        _requestGetUri = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr>)
            Export(library, "webkit_uri_scheme_request_get_uri");
        _requestGetWebView = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr>)
            Export(library, "webkit_uri_scheme_request_get_web_view");
        _requestFinishError = (delegate* unmanaged[Cdecl]<IntPtr, GError*, void>)
            Export(library, "webkit_uri_scheme_request_finish_error");
        _requestGetHttpMethod = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr>)
            Export(library, "webkit_uri_scheme_request_get_http_method");
        _requestGetHttpHeaders = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr>)
            Export(library, "webkit_uri_scheme_request_get_http_headers");
        _requestFinishWithResponse = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)
            Export(library, "webkit_uri_scheme_request_finish_with_response");
        _responseNew = (delegate* unmanaged[Cdecl]<IntPtr, long, IntPtr>)
            Export(library, "webkit_uri_scheme_response_new");
        _responseSetStatus = (delegate* unmanaged[Cdecl]<IntPtr, uint, byte*, void>)
            Export(library, "webkit_uri_scheme_response_set_status");
        _responseSetContentType = (delegate* unmanaged[Cdecl]<IntPtr, byte*, void>)
            Export(library, "webkit_uri_scheme_response_set_content_type");
        _responseSetHttpHeaders = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)
            Export(library, "webkit_uri_scheme_response_set_http_headers");

        // From the libsoup this WebKit links, as libsoup 2 and 3 may both be installed. Same calls in both.
        _headersNew = (delegate* unmanaged[Cdecl]<int, IntPtr>)
            Export(library, "soup_message_headers_new");
        _headersAppend = (delegate* unmanaged[Cdecl]<IntPtr, byte*, byte*, void>)
            Export(library, "soup_message_headers_append");
        _headersForeach = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)
            Export(library, "soup_message_headers_foreach");
    }

    public static WebKitUriSchemeHandler Gtk => s_gtk.Value;
    public static WebKitUriSchemeHandler Wpe => s_wpe.Value;

    public unsafe IntPtr GetDefaultContext() => _getDefaultContext();

    /// <summary>
    /// Registers the schemes on the context, which should happen before its web views are created.
    /// </summary>
    public unsafe void Register(IntPtr context, string[] schemes)
    {
        foreach (var scheme in schemes)
        {
            // Marked on the context itself, as a freed context's address can be reused.
            var key = RegisteredSchemeKeyPrefix + scheme;
            if (g_object_get_data(context, key) != IntPtr.Zero)
                continue;
            g_object_set_data(context, key, 1);

            var nativeScheme = Utf8StringMarshaller.ConvertToUnmanaged(scheme);
            _registerUriScheme(context, nativeScheme, s_callback, _self, IntPtr.Zero);
            var securityManager = _getSecurityManager(context);
            _registerAsSecure(securityManager, nativeScheme);
            _registerAsCorsEnabled(securityManager, nativeScheme);
            Utf8StringMarshaller.Free(nativeScheme);
        }
    }

    public void AddWebView(IntPtr webView, IWebKitUriSchemeTarget target, string[] schemes)
    {
        lock (_lock)
            _targets[webView] = (new WeakReference<IWebKitUriSchemeTarget>(target), schemes);
    }

    public void RemoveWebView(IntPtr webView)
    {
        lock (_lock)
            _targets.Remove(webView);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnRequest(IntPtr request, IntPtr userData)
    {
        if (GCHandle.FromIntPtr(userData).Target is WebKitUriSchemeHandler handler)
            handler.OnRequest(request);
    }

    private unsafe void OnRequest(IntPtr request)
    {
        try
        {
            var webView = _requestGetWebView(request);
            IWebKitUriSchemeTarget? target = null;
            string[] schemes = [];
            lock (_lock)
            {
                if (_targets.TryGetValue(webView, out var registration))
                {
                    registration.Target.TryGetTarget(out target);
                    schemes = registration.Schemes;
                }
            }

            // Another web view of the context may have registered the scheme.
            if (target is null
                || !Uri.TryCreate(Marshal.PtrToStringUTF8(_requestGetUri(request)), UriKind.Absolute, out var uri)
                || !WebResourceResponseHelper.IsCustomScheme(uri, schemes))
            {
                FinishError(request, "No web view to answer the request.");
                return;
            }

            var method = Marshal.PtrToStringUTF8(_requestGetHttpMethod(request)) is { Length: > 0 } methodName ?
                new HttpMethod(methodName) :
                HttpMethod.Get;

            // A copy, as handlers read it on the UI thread.
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (_requestGetHttpHeaders(request) is var nativeHeaders and not 0)
            {
                var headersHandle = GCHandle.Alloc(headers);
                try
                {
                    _headersForeach(nativeHeaders, s_headerCallback, GCHandle.ToIntPtr(headersHandle));
                }
                finally
                {
                    headersHandle.Free();
                }
            }

            g_object_ref(request);
            _ = AnswerAsync(target, request, new WebViewWebResourceRequest
            {
                Method = method,
                Uri = uri,
                Headers = new NativeHeadersCollection(new DictionaryNativeHttpRequestHeaders(headers, true))
            });
        }
        catch (Exception ex)
        {
            LogError(ex);
            FinishError(request, ex.Message);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnHeader(IntPtr name, IntPtr value, IntPtr userData)
    {
        var headers = (Dictionary<string, string>)GCHandle.FromIntPtr(userData).Target!;
        if (Marshal.PtrToStringUTF8(name) is not { } key)
            return;
        var text = Marshal.PtrToStringUTF8(value) ?? "";
        // Repeated headers are joined, as in HTTP.
        headers[key] = headers.TryGetValue(key, out var previous) ? previous + ", " + text : text;
    }

    private async Task AnswerAsync(IWebKitUriSchemeTarget target, IntPtr request, WebViewWebResourceRequest webRequest)
    {
        WebViewWebResourceResponse? response = null;
        ReadOnlyMemory<byte>? content = null;
        try
        {
            response = await target.RaiseUriSchemeRequestAsync(webRequest).ConfigureAwait(false)
                       ?? WebResourceResponseHelper.NotFound();
            response = WebResourceResponseHelper.ApplyRange(webRequest, response);

            content = await WebResourceResponseHelper.ReadContentAsync(response.Content).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogError(ex);
        }
        finally
        {
            ((NativeHeadersCollection)webRequest.Headers).Dispose();
        }

        try
        {
            await _runOnGlibThread(() =>
            {
                var finished = false;
                try
                {
                    if (response is not null && content is { } body)
                    {
                        Finish(request, response, body.Span);
                        finished = true;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
                finally
                {
                    if (!finished)
                        FinishError(request, "Failed to answer the request.");
                    g_object_unref(request);
                }
            }).ConfigureAwait(false);
        }
        finally
        {
            // After Finish, as the content may be the stream's own buffer.
            WebResourceResponseHelper.DisposeContent(response);
        }
    }

    private unsafe void Finish(IntPtr request, WebViewWebResourceResponse response, ReadOnlySpan<byte> content)
    {
        IntPtr bytes;
        fixed (byte* ptr = content)
        {
            bytes = g_bytes_new(ptr, (nuint)content.Length);
        }
        var stream = g_memory_input_stream_new_from_bytes(bytes);
        g_bytes_unref(bytes);

        var schemeResponse = IntPtr.Zero;
        try
        {
            schemeResponse = _responseNew(stream, content.Length);
            var reasonPhrase =
                Utf8StringMarshaller.ConvertToUnmanaged(WebResourceResponseHelper.GetReasonPhrase(response));
            _responseSetStatus(schemeResponse, (uint)response.StatusCode, reasonPhrase);
            Utf8StringMarshaller.Free(reasonPhrase);
            var validHeaders = WebResourceResponseHelper.GetValidHeaders(response);
            if (validHeaders.TryGetValue(HttpHeaderNames.ContentType, out var contentType))
            {
                var nativeContentType = Utf8StringMarshaller.ConvertToUnmanaged(contentType);
                _responseSetContentType(schemeResponse, nativeContentType);
                Utf8StringMarshaller.Free(nativeContentType);
            }
            if (validHeaders.Count > 0)
            {
                var headers = _headersNew(SoupMessageHeadersResponse);
                foreach (var header in validHeaders)
                {
                    var name = Utf8StringMarshaller.ConvertToUnmanaged(header.Key);
                    var value = Utf8StringMarshaller.ConvertToUnmanaged(header.Value);
                    _headersAppend(headers, name, value);
                    Utf8StringMarshaller.Free(name);
                    Utf8StringMarshaller.Free(value);
                }
                // Takes ownership of the headers.
                _responseSetHttpHeaders(schemeResponse, headers);
            }
            _requestFinishWithResponse(request, schemeResponse);
        }
        finally
        {
            if (schemeResponse != IntPtr.Zero)
                g_object_unref(schemeResponse);
            g_object_unref(stream);
        }
    }

    private unsafe void FinishError(IntPtr request, string message)
    {
        var error = g_error_new_literal(g_quark_from_string(ErrorDomain), 0, message);
        _requestFinishError(request, error);
        g_error_free(error);
    }

    private static IntPtr Export(IntPtr library, string name) =>
        NativeLibrary.TryGetExport(library, name, out var address) ?
            address :
            throw new NotSupportedException($"Custom schemes need WebKit 2.36 or later, {name} is missing.");

    private static void LogError(Exception ex) =>
        WebResourceResponseHelper.LogError("Failed to answer a custom scheme request: {Exception}", ex);
}
