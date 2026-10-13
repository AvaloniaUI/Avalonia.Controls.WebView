using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Linux.Interop;
using Avalonia.Controls.Rendering;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Avalonia.Controls.Linux;

internal sealed unsafe class WpeWebViewAdapter
    : IWebViewAdapterWithOffscreenBuffer,
      IWebViewAdapterWithOffscreenInput,
      IWebViewAdapterWithCookieManager,
      IWebViewAdapterWithVisibility,
      IWebViewAdapterWithGpuSurface,
      ILinuxWpePlatformHandle
{
    private IntPtr _webView;
    private IWpeViewBackend? _backend;
    private IntPtr _networkSession;
    private IntPtr _userContentManager;
    private IntPtr _cookieManager;
    private PixelSize _currentSize;
    private float _currentDeviceScale;
    private Control? _parentControl;
    private bool _disposed;

    // Pin callback delegates to prevent GC
    private readonly GSignalCallback _loadChangedCallback;
    private readonly GSignalDecidePolicyCallback _decidePolicyCallback;
    private readonly GSignalCreateCallback _createCallback;
    private readonly GSignalScriptMessageCallback _scriptMessageCallback;
    private readonly GSignalLoadFailedCallback _loadFailedCallback;
    private GCHandle _selfHandle;

    private WpeWebViewAdapter()
    {
        // Pin callbacks as instance fields
        _loadChangedCallback = OnLoadChanged;
        _decidePolicyCallback = OnDecidePolicy;
        _createCallback = OnCreate;
        _scriptMessageCallback = OnScriptMessageReceived;
        _loadFailedCallback = OnLoadFailed;
    }

    // Unmanaged callback signatures
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GSignalCallback(IntPtr instance, int loadEvent, IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GSignalDecidePolicyCallback(IntPtr webView, IntPtr decision, int decisionType, IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GSignalCreateCallback(IntPtr webView, IntPtr navigationAction, IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GSignalScriptMessageCallback(IntPtr manager, IntPtr jsResult, IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GSignalLoadFailedCallback(IntPtr webView, int loadEvent, IntPtr failingUri, IntPtr error, IntPtr userData);

    public static bool IsAvailable() => WpeRuntime.IsAvailable;

    public static Task<WebViewAdapter.OffscreenWebViewAdapterBuilder> CreateBuilder(
        LinuxWpeWebViewEnvironmentRequestedEventArgs args)
    {
        WebViewAdapter.OffscreenWebViewAdapterBuilder builder = parent =>
        {
            var adapter = new WpeWebViewAdapter();
            adapter.Initialize(args, parent);
            return Task.FromResult<IWebViewAdapterWithOffscreenBuffer>(adapter);
        };
        return Task.FromResult(builder);
    }

    private void Initialize(LinuxWpeWebViewEnvironmentRequestedEventArgs args, Control parent)
    {
        _parentControl = parent;
        Dispatcher.UIThread.VerifyAccess();

        _selfHandle = GCHandle.Alloc(this);
        var selfPtr = GCHandle.ToIntPtr(_selfHandle);

        var initialSize = new PixelSize(1280, 720);
        var mode = args.RenderingMode;

        if (mode == WpeRenderingMode.Egl)
            throw new NotImplementedException("EGL rendering mode is not implemented. Use WpeRenderingMode.Auto or WpeRenderingMode.Shm.");
        if (mode == WpeRenderingMode.DmaBuf)
            throw new NotImplementedException("DMABuf rendering mode is not implemented. Use WpeRenderingMode.Auto or WpeRenderingMode.Shm.");

        _backend = WpeRuntime.ResolveApi(args.Api) == WpeApi.Platform
            ? new WpePlatformViewBackend(WpeRuntime.GetPlatformDisplay(mode == WpeRenderingMode.Shm), initialSize)
            : new WpeLegacyViewBackend(initialSize);
        _backend.FrameReady = OnFrameReady;

        if (args.DataDirectory != null || args.CacheDirectory != null)
            _networkSession = WpeInterop.webkit_network_session_new(args.DataDirectory, args.CacheDirectory);
        else
            _networkSession = WpeInterop.webkit_network_session_get_default();

        _userContentManager = WpeInterop.webkit_user_content_manager_new();
        if (_userContentManager == IntPtr.Zero)
            throw new InvalidOperationException("webkit_user_content_manager_new failed.");

        var webViewType = WpeInterop.webkit_web_view_get_type();
        var networkSessionType = WpeInterop.webkit_network_session_get_type();
        var contentManagerType = WpeInterop.webkit_user_content_manager_get_type();
        var keys = new[] { _backend.WebViewPropertyName, "network-session", "user-content-manager" };
        var values = new[]
        {
            _backend.WebViewPropertyValue,
            new GValue(networkSessionType, _networkSession),
            new GValue(contentManagerType, _userContentManager)
        };
        _webView = WpeInterop.g_object_new_with_properties(webViewType, 3, keys, values);
        if (_webView == IntPtr.Zero)
            throw new InvalidOperationException("webkit_web_view_new failed.");

        var viewContentManager = WpeInterop.webkit_web_view_get_user_content_manager(_webView);
        if (viewContentManager != IntPtr.Zero && viewContentManager != _userContentManager)
        {
            WpeInterop.g_object_unref(_userContentManager);
            _userContentManager = WpeInterop.g_object_ref(viewContentManager);
        }

        // 1. Start GLib pump (WebKit needs it for internal IPC)
        WpeGLibIntegration.Start();

        // Pump a few iterations to let WebKit finish internal setup
        var ctx = WpeInterop.g_main_context_default();
        for (int i = 0; i < 10; i++)
            WpeInterop.g_main_context_iteration(ctx, false);

        // 2. Attach the view backend
        _currentDeviceScale = (float)(TopLevel.GetTopLevel(_parentControl!)?.RenderScaling ?? 1.0);
        _backend.Attach(_webView, _currentDeviceScale);

        // 3. Connect GObject signals
        ConnectSignal(_webView, "load-changed", Marshal.GetFunctionPointerForDelegate(_loadChangedCallback), selfPtr);
        ConnectSignal(_webView, "load-failed", Marshal.GetFunctionPointerForDelegate(_loadFailedCallback), selfPtr);
        ConnectSignal(_webView, "decide-policy", Marshal.GetFunctionPointerForDelegate(_decidePolicyCallback), selfPtr);
        ConnectSignal(_webView, "create", Marshal.GetFunctionPointerForDelegate(_createCallback), selfPtr);

        // 4. Register the message handler, connecting before registering so that no message can arrive unhandled.
        ConnectSignal(_userContentManager, $"script-message-received::{WebViewHelper.PostAvWebViewMessageName}",
            Marshal.GetFunctionPointerForDelegate(_scriptMessageCallback), selfPtr);
        WpeInterop.webkit_user_content_manager_register_script_message_handler(
            _userContentManager, WebViewHelper.PostAvWebViewMessageName, null);

        // Inject the invokeCSharpAction wrapper into all frames at document start.
        var bridgeScript = WpeInterop.webkit_user_script_new(
            WebViewHelper.BuildWebKitInvokeCSharpActionScript(), 0, 0, IntPtr.Zero, IntPtr.Zero);
        WpeInterop.webkit_user_content_manager_add_script(_userContentManager, bridgeScript);
        WpeInterop.webkit_user_script_unref(bridgeScript);

        // 5. Apply settings
        var settings = WpeInterop.webkit_web_view_get_settings(_webView);
        if (args.EnableDevTools)
            WpeInterop.webkit_settings_set_enable_developer_extras(settings, true);
        // 6. Get cookie manager
        _cookieManager = WpeInterop.webkit_network_session_get_cookie_manager(_networkSession);

        _currentSize = initialSize;

        Handle = _webView;
    }

    private static void ConnectSignal(IntPtr instance, string signal, IntPtr handler, IntPtr userData)
    {
        WpeInterop.g_signal_connect_data(instance, signal, handler, userData, IntPtr.Zero, 0);
    }

    private bool OnFrameReady()
    {
        if (DrawRequested is null)
            return false;
        DrawRequested.Invoke();
        return true;
    }

    // --- IWebViewAdapterWithGpuSurface ---

    public bool IsGpuExportAvailable => false;

    public event Action? GpuFrameReady;

    public Task PresentGpuFrame(ICompositionGpuInterop gpuInterop, CompositionDrawingSurface surface)
    {
        throw new NotImplementedException("GPU frame presentation is not available in SHM rendering mode.");
    }

    // --- GObject signal handlers ---

    // WebKitLoadEvent
    private const int WEBKIT_LOAD_STARTED = 0;
    private const int WEBKIT_LOAD_REDIRECTED = 1;
    private const int WEBKIT_LOAD_COMMITTED = 2;
    private const int WEBKIT_LOAD_FINISHED = 3;

    private static void OnLoadChanged(IntPtr webView, int loadEvent, IntPtr userData)
    {
        var adapter = (WpeWebViewAdapter)GCHandle.FromIntPtr(userData).Target!;
        var uri = GetCurrentUri(webView);

        switch (loadEvent)
        {
            case WEBKIT_LOAD_FINISHED:
                adapter.NavigationCompleted?.Invoke(adapter, new WebViewNavigationCompletedEventArgs { Request = uri, IsSuccess = true });
                break;
        }
    }

    private static int OnLoadFailed(IntPtr webView, int loadEvent, IntPtr failingUriPtr, IntPtr error, IntPtr userData)
    {
        var adapter = (WpeWebViewAdapter)GCHandle.FromIntPtr(userData).Target!;
        var failingUri = Marshal.PtrToStringUTF8(failingUriPtr);
        Uri.TryCreate(failingUri, UriKind.Absolute, out var uri);
        adapter.NavigationCompleted?.Invoke(adapter, new WebViewNavigationCompletedEventArgs { Request = uri, IsSuccess = false });
        return 0; // FALSE = don't suppress default handling
    }

    // WebKitPolicyDecisionType
    private const int WEBKIT_POLICY_DECISION_TYPE_NAVIGATION_ACTION = 0;
    private const int WEBKIT_POLICY_DECISION_TYPE_NEW_WINDOW_ACTION = 1;

    private static int OnDecidePolicy(IntPtr webView, IntPtr decision, int decisionType, IntPtr userData)
    {
        var adapter = (WpeWebViewAdapter)GCHandle.FromIntPtr(userData).Target!;

        if (decisionType == WEBKIT_POLICY_DECISION_TYPE_NAVIGATION_ACTION)
        {
            var action = WpeInterop.webkit_navigation_policy_decision_get_navigation_action(decision);
            var request = WpeInterop.webkit_navigation_action_get_request(action);
            var uriPtr = WpeInterop.webkit_uri_request_get_uri(request);
            var uriStr = Marshal.PtrToStringUTF8(uriPtr);

            if (Uri.TryCreate(uriStr, UriKind.Absolute, out var uri))
            {
                var args = new WebViewNavigationStartingEventArgs { Request = uri };
                adapter.NavigationStarted?.Invoke(adapter, args);
                if (args.Cancel)
                {
                    WpeInterop.webkit_policy_decision_ignore(decision);
                    return 1; // TRUE = handled
                }
            }
        }

        WpeInterop.webkit_policy_decision_use(decision);
        return 1; // TRUE = handled
    }

    private static IntPtr OnCreate(IntPtr webView, IntPtr navigationAction, IntPtr userData)
    {
        var adapter = (WpeWebViewAdapter)GCHandle.FromIntPtr(userData).Target!;
        var request = WpeInterop.webkit_navigation_action_get_request(navigationAction);
        var uriPtr = WpeInterop.webkit_uri_request_get_uri(request);
        var uriStr = Marshal.PtrToStringUTF8(uriPtr);

        if (Uri.TryCreate(uriStr, UriKind.Absolute, out var uri))
        {
            adapter.NewWindowRequested?.Invoke(adapter,
                new WebViewNewWindowRequestedEventArgs { Request = uri });
        }
        return IntPtr.Zero; // Return null to prevent new window creation
    }

    private static void OnScriptMessageReceived(IntPtr manager, IntPtr jsResult, IntPtr userData)
    {
        var adapter = (WpeWebViewAdapter)GCHandle.FromIntPtr(userData).Target!;
        string? body = null;

        if (jsResult != IntPtr.Zero && !WpeInterop.jsc_value_is_undefined(jsResult) && !WpeInterop.jsc_value_is_null(jsResult))
        {
            var strPtr = WpeInterop.jsc_value_to_string(jsResult);
            if (strPtr != IntPtr.Zero)
            {
                body = Marshal.PtrToStringUTF8(strPtr);
                WpeInterop.g_free(strPtr);
            }
        }

        adapter.WebMessageReceived?.Invoke(adapter, new WebMessageReceivedEventArgs { Body = body });
    }

    // --- IWebViewAdapter ---

    public IntPtr Handle { get; private set; }
    public string HandleDescriptor => "WebKitWebView";

    public Color DefaultBackground
    {
        set
        {
            if (_webView == IntPtr.Zero) return;
            var color = new WebKitColor
            {
                Red = value.R / 255.0,
                Green = value.G / 255.0,
                Blue = value.B / 255.0,
                Alpha = value.A / 255.0
            };
            WpeInterop.webkit_web_view_set_background_color(_webView, &color);
        }
    }

    public string? UserAgent
    {
        get => null; // WPE doesn't provide a getter easily
        set
        {
            if (_webView == IntPtr.Zero) return;
            var settings = WpeInterop.webkit_web_view_get_settings(_webView);
            WpeInterop.webkit_settings_set_user_agent(settings, value);
        }
    }

    public Uri Source
    {
        get => GetCurrentUri(_webView) ?? WebViewHelper.EmptyPage;
        set => Navigate(value);
    }

    public bool CanGoBack => _webView != IntPtr.Zero && WpeInterop.webkit_web_view_can_go_back(_webView);
    public bool CanGoForward => _webView != IntPtr.Zero && WpeInterop.webkit_web_view_can_go_forward(_webView);

    public void Navigate(Uri url)
    {
        if (_webView == IntPtr.Zero || _disposed) return;
        WpeInterop.webkit_web_view_load_uri(_webView, url.AbsoluteUri);
    }

    public void NavigateToString(string text, Uri? baseUri)
    {
        if (_webView == IntPtr.Zero || _disposed) return;
        WpeInterop.webkit_web_view_load_html(_webView, text, baseUri?.AbsoluteUri);
    }

    public bool GoBack()
    {
        if (!CanGoBack) return false;
        WpeInterop.webkit_web_view_go_back(_webView);
        return true;
    }

    public bool GoForward()
    {
        if (!CanGoForward) return false;
        WpeInterop.webkit_web_view_go_forward(_webView);
        return true;
    }

    public bool Refresh()
    {
        if (_webView == IntPtr.Zero) return false;
        WpeInterop.webkit_web_view_reload(_webView);
        return true;
    }

    public bool Stop()
    {
        if (_webView == IntPtr.Zero) return false;
        WpeInterop.webkit_web_view_stop_loading(_webView);
        return true;
    }

    public Task<string?> InvokeScript(string script)
    {
        if (_webView == IntPtr.Zero || _disposed)
            return Task.FromResult<string?>(null);

        var tcs = new TaskCompletionSource<string?>();
        var tcsHandle = GCHandle.Alloc(tcs);

        WpeInterop.webkit_web_view_evaluate_javascript(
            _webView, script, -1, null, null, IntPtr.Zero,
            &OnEvaluateJavascriptFinished,
            GCHandle.ToIntPtr(tcsHandle));

        return tcs.Task;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static void OnEvaluateJavascriptFinished(IntPtr sourceObject, IntPtr result, IntPtr userData)
    {
        var tcsHandle = GCHandle.FromIntPtr(userData);
        var tcs = (TaskCompletionSource<string?>)tcsHandle.Target!;
        tcsHandle.Free();

        IntPtr error = IntPtr.Zero;
        var jsValue = WpeInterop.webkit_web_view_evaluate_javascript_finish(sourceObject, result, &error);

        if (error != IntPtr.Zero || jsValue == IntPtr.Zero)
        {
            tcs.TrySetResult(null);
            return;
        }

        string? strResult = null;
        if (!WpeInterop.jsc_value_is_undefined(jsValue) && !WpeInterop.jsc_value_is_null(jsValue))
        {
            var strPtr = WpeInterop.jsc_value_to_string(jsValue);
            if (strPtr != IntPtr.Zero)
            {
                strResult = Marshal.PtrToStringUTF8(strPtr);
                WpeInterop.g_free(strPtr);
            }
        }
        WpeInterop.g_object_unref(jsValue);
        tcs.TrySetResult(strResult);
    }

    public void SizeChanged(PixelSize containerSize)
    {
        if (_backend is null || _disposed || containerSize.Width <= 0 || containerSize.Height <= 0) return;
        _currentSize = containerSize;

        // WPE expects dispatch_set_size in LOGICAL (CSS) pixels, not physical.
        // The device_scale_factor tells WPE to render at higher resolution internally.
        // containerSize is in physical pixels, so divide by scale to get logical.
        var scale = (float)(TopLevel.GetTopLevel(_parentControl!)?.RenderScaling ?? 1.0);
        var logicalWidth = (uint)(containerSize.Width / scale);
        var logicalHeight = (uint)(containerSize.Height / scale);
        _backend.SetSize(logicalWidth, logicalHeight);

        if (Math.Abs(scale - _currentDeviceScale) > 0.001f)
        {
            _currentDeviceScale = scale;
            _backend.SetScale(scale);
        }
    }

    public void SetParent(IPlatformHandle parent)
    {
        // Not applicable for offscreen rendering
    }

    public void SetVisible(bool visible)
    {
        if (_backend is null || _disposed) return;
        _backend.SetVisible(visible);
    }

    // --- IWebViewAdapterWithOffscreenBuffer ---

    public event Action? DrawRequested;

    public PixelFormat BufferPixelFormat => PixelFormats.Bgra8888;
    public AlphaFormat BufferAlphaFormat => AlphaFormat.Premul;

    public Task UpdateWriteableBitmap(PixelSize currentSize, FrameChainBase<WriteableBitmap, PixelSize>.IProducer producer)
    {
        if (currentSize == default || _backend is null || _disposed)
            return Task.CompletedTask;

        _backend.CopyFrame(producer);
        return Task.CompletedTask;
    }

    // --- IWebViewAdapterWithOffscreenInput ---

    private string? _lastKeySymbol; // Track last key press symbol to avoid double text input

    public bool KeyInput(bool press, PhysicalKey physical, string? symbol, KeyModifiers modifiers)
    {
        if (_backend is null || _disposed) return false;

        if (press)
            _lastKeySymbol = symbol;

        var xkbKeycode = PhysicalKeyToXkb(physical);
        if (xkbKeycode == 0) return false;

        // WPE's key_code field expects an XKB keysym.
        // For character keys, convert the Unicode codepoint via wpe_unicode_to_key_code.
        // For non-character keys (Enter, Backspace, arrows, etc.), use XKB keysym constants.
        var keysym = PhysicalKeyToKeysym(physical);
        if (keysym == 0 && symbol is { Length: > 0 })
        {
            var codepoint = (uint)char.ConvertToUtf32(symbol, 0);
            keysym = _backend.UnicodeToKeysym(codepoint);
        }

        _backend.KeyInput(press, keysym, xkbKeycode, MapModifiers(modifiers));
        return true;
    }

    public bool TextInput(string text)
    {
        if (_webView == IntPtr.Zero || _disposed) return false;

        // For regular key presses, the keyboard event already handled the character input.
        // Only inject via JS for IME-composed text that doesn't match the last key symbol.
        if (text == _lastKeySymbol)
        {
            _lastKeySymbol = null;
            return false;
        }
        _lastKeySymbol = null;

        // Inject composed text into the focused element via JavaScript.
        // Escape backslashes and single quotes for the JS string literal.
        var escaped = text.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "");
        var script = $"document.execCommand('insertText', false, '{escaped}')";
        WpeInterop.webkit_web_view_evaluate_javascript(
            _webView, script, -1, null, null, IntPtr.Zero, null, IntPtr.Zero);
        return true;
    }

    public bool PointerInput(PointerPoint point, int clickCount, double dpi, KeyModifiers modifiers)
    {
        if (_backend is null || _disposed) return false;
        _backend.PointerInput(point, clickCount, dpi, MapModifiers(modifiers));
        return true;
    }

    public bool PointerLeaveInput(PointerPoint point, double dpi, KeyModifiers modifiers)
    {
        if (_backend is null || _disposed) return false;
        return _backend.PointerLeaveInput(point, dpi, MapModifiers(modifiers));
    }

    public bool PointerWheelInput(Vector delta, PointerPoint point, double dpi, KeyModifiers modifiers)
    {
        if (_backend is null || _disposed) return false;
        _backend.PointerWheelInput(delta, point, dpi, MapModifiers(modifiers));
        return true;
    }

    // --- IWebViewAdapterWithCookieManager ---

    public void AddOrUpdateCookie(Cookie cookie)
    {
        if (_cookieManager == IntPtr.Zero || _disposed) return;

        var maxAge = cookie.Expires != default
            ? (int)(cookie.Expires - DateTime.UtcNow).TotalSeconds
            : -1;

        var soupCookie = WpeInterop.soup_cookie_new(
            cookie.Name, cookie.Value, cookie.Domain, cookie.Path, maxAge);

        if (soupCookie != IntPtr.Zero)
        {
            WpeInterop.soup_cookie_set_secure(soupCookie, cookie.Secure);
            WpeInterop.soup_cookie_set_http_only(soupCookie, cookie.HttpOnly);

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var tcsHandle = GCHandle.Alloc(tcs);

            WpeInterop.webkit_cookie_manager_add_cookie(
                _cookieManager, soupCookie, IntPtr.Zero,
                &OnAddCookieFinished, GCHandle.ToIntPtr(tcsHandle));

            WaitForCompletion(tcs.Task);

            // soup cookie is consumed by webkit
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static void OnAddCookieFinished(IntPtr sourceObject, IntPtr result, IntPtr userData)
    {
        var tcsHandle = GCHandle.FromIntPtr(userData);
        var tcs = (TaskCompletionSource<bool>)tcsHandle.Target!;
        tcsHandle.Free();

        IntPtr error = IntPtr.Zero;
        var ok = WpeInterop.webkit_cookie_manager_add_cookie_finish(sourceObject, result, &error);
        tcs.TrySetResult(ok && error == IntPtr.Zero);
    }

    public void DeleteCookie(string name, string domain, string path)
    {
        DeleteCookie(new Cookie(name, "", path, domain));
    }

    public void DeleteCookie(Cookie cookie)
    {
        if (_cookieManager == IntPtr.Zero || _disposed) return;

        var soupCookie = WpeInterop.soup_cookie_new(cookie.Name, cookie.Value, cookie.Domain, cookie.Path, 0);
        if (soupCookie != IntPtr.Zero)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var tcsHandle = GCHandle.Alloc(tcs);

            WpeInterop.webkit_cookie_manager_delete_cookie(
                _cookieManager, soupCookie, IntPtr.Zero,
                &OnDeleteCookieFinished, GCHandle.ToIntPtr(tcsHandle));

            WaitForCompletion(tcs.Task);

            WpeInterop.soup_cookie_free(soupCookie);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static void OnDeleteCookieFinished(IntPtr sourceObject, IntPtr result, IntPtr userData)
    {
        var tcsHandle = GCHandle.FromIntPtr(userData);
        var tcs = (TaskCompletionSource<bool>)tcsHandle.Target!;
        tcsHandle.Free();

        IntPtr error = IntPtr.Zero;
        var ok = WpeInterop.webkit_cookie_manager_delete_cookie_finish(sourceObject, result, &error);
        tcs.TrySetResult(ok && error == IntPtr.Zero);
    }

    private static void WaitForCompletion(Task task)
    {
        if (task.IsCompleted)
        {
            task.GetAwaiter().GetResult();
            return;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            WebViewDispatcher.PushFrameForTask(task);
        }

        task.GetAwaiter().GetResult();
    }

    public Task<IReadOnlyList<Cookie>> GetCookiesAsync()
    {
        if (_cookieManager == IntPtr.Zero || _disposed)
            return Task.FromResult<IReadOnlyList<Cookie>>(Array.Empty<Cookie>());

        var tcs = new TaskCompletionSource<IReadOnlyList<Cookie>>();
        var tcsHandle = GCHandle.Alloc(tcs);

        WpeInterop.webkit_cookie_manager_get_all_cookies(
            _cookieManager, IntPtr.Zero,
            &OnGetAllCookiesFinished, GCHandle.ToIntPtr(tcsHandle));

        return tcs.Task;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static void OnGetAllCookiesFinished(IntPtr sourceObject, IntPtr result, IntPtr userData)
    {
        var tcsHandle = GCHandle.FromIntPtr(userData);
        var tcs = (TaskCompletionSource<IReadOnlyList<Cookie>>)tcsHandle.Target!;
        tcsHandle.Free();

        IntPtr error = IntPtr.Zero;
        var glist = WpeInterop.webkit_cookie_manager_get_all_cookies_finish(sourceObject, result, &error);

        var cookies = new List<Cookie>();
        if (glist != IntPtr.Zero)
        {
            var length = WpeInterop.g_list_length(glist);
            for (uint i = 0; i < length; i++)
            {
                var soupCookie = WpeInterop.g_list_nth_data(glist, i);
                if (soupCookie == IntPtr.Zero) continue;

                var name = Marshal.PtrToStringUTF8(WpeInterop.soup_cookie_get_name(soupCookie)) ?? "";
                var value = Marshal.PtrToStringUTF8(WpeInterop.soup_cookie_get_value(soupCookie)) ?? "";
                var domain = Marshal.PtrToStringUTF8(WpeInterop.soup_cookie_get_domain(soupCookie)) ?? "";
                var path = Marshal.PtrToStringUTF8(WpeInterop.soup_cookie_get_path(soupCookie)) ?? "/";
                var secure = WpeInterop.soup_cookie_get_secure(soupCookie);
                var httpOnly = WpeInterop.soup_cookie_get_http_only(soupCookie);

                var cookie = new Cookie(name, value, path, domain)
                {
                    Secure = secure,
                    HttpOnly = httpOnly
                };

                var expiresPtr = WpeInterop.soup_cookie_get_expires(soupCookie);
                if (expiresPtr != IntPtr.Zero)
                {
                    var unixTime = WpeInterop.g_date_time_to_unix(expiresPtr);
                    cookie.Expires = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
                }

                cookies.Add(cookie);
            }
            // GList of SoupCookie is freed with the cookies
            WpeInterop.g_list_free(glist);
        }

        tcs.TrySetResult(cookies);
    }

    // --- ILinuxWpePlatformHandle ---

    IntPtr ILinuxWpePlatformHandle.WebKitWebView => _webView;
    IntPtr ILinuxWpePlatformHandle.WpeViewBackend => _backend?.WpeViewBackend ?? IntPtr.Zero;
    IntPtr ILinuxWpePlatformHandle.WpeView => _backend?.WpeView ?? IntPtr.Zero;

    // --- Events ---

    public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;
    public event EventHandler<WebViewNewWindowRequestedEventArgs>? NewWindowRequested;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;

    // --- Static info ---

    internal static DetailedWebViewAdapterInfo GetWpeInfo()
    {
        if (!IsAvailable())
        {
            return new DetailedWebViewAdapterInfo(
                WebViewAdapterType.WpeWebKit,
                WebViewEngine.WebKit,
                IsSupported: true,
                IsInstalled: false,
                Version: null,
                UnavailableReason: "WPE WebKit libraries are not installed.",
                SupportedScenarios: WebViewEmbeddingScenario.None);
        }

        var major = WpeInterop.webkit_get_major_version();
        var minor = WpeInterop.webkit_get_minor_version();
        var micro = WpeInterop.webkit_get_micro_version();
        var version = $"{major}.{minor}.{micro}";

        return new DetailedWebViewAdapterInfo(
            WebViewAdapterType.WpeWebKit,
            WebViewEngine.WebKit,
            IsSupported: true,
            IsInstalled: true,
            Version: version,
            UnavailableReason: null,
            SupportedScenarios: WebViewEmbeddingScenario.OffscreenRenderer);
    }

    // --- Dispose ---

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _backend?.ReleaseFrames();

        if (_webView != IntPtr.Zero)
        {
            // Destroying the web view will also destroy the exportable via the
            // GDestroyNotify callback we passed to webkit_web_view_backend_new.
            WpeInterop.g_object_unref(_webView);
            _webView = IntPtr.Zero;
        }

        if (_userContentManager != IntPtr.Zero)
        {
            WpeInterop.g_object_unref(_userContentManager);
            _userContentManager = IntPtr.Zero;
        }

        _backend?.Dispose();
        _backend = null;

        // Pump GLib iterations to let WebKit/WPE finalize Wayland resource cleanup
        // before we stop the GLib integration and tear down the context.
        var ctx = WpeInterop.g_main_context_default();
        for (int i = 0; i < 10; i++)
            WpeInterop.g_main_context_iteration(ctx, false);

        WpeGLibIntegration.Stop();

        if (_selfHandle.IsAllocated)
            _selfHandle.Free();
    }

    // --- Helper methods ---

    private static Uri? GetCurrentUri(IntPtr webView)
    {
        if (webView == IntPtr.Zero) return null;
        var uriPtr = WpeInterop.webkit_web_view_get_uri(webView);
        if (uriPtr == IntPtr.Zero) return null;
        var uriStr = Marshal.PtrToStringUTF8(uriPtr);
        return Uri.TryCreate(uriStr, UriKind.Absolute, out var uri) ? uri : null;
    }

    private static uint MapModifiers(KeyModifiers modifiers)
    {
        uint result = 0;
        if (modifiers.HasFlag(KeyModifiers.Control)) result |= WpeInputModifier.Control;
        if (modifiers.HasFlag(KeyModifiers.Shift)) result |= WpeInputModifier.Shift;
        if (modifiers.HasFlag(KeyModifiers.Alt)) result |= WpeInputModifier.Alt;
        if (modifiers.HasFlag(KeyModifiers.Meta)) result |= WpeInputModifier.Meta;
        return result;
    }

    /// <summary>
    /// Maps Avalonia PhysicalKey to XKB keysym for non-character keys.
    /// Returns 0 for character keys (handled via wpe_unicode_to_key_code).
    /// </summary>
    private static uint PhysicalKeyToKeysym(PhysicalKey key) => key switch
    {
        PhysicalKey.Escape => 0xff1b,
        PhysicalKey.Backspace => 0xff08,
        PhysicalKey.Tab => 0xff09,
        PhysicalKey.Enter => 0xff0d,
        PhysicalKey.NumPadEnter => 0xff0d,
        PhysicalKey.CapsLock => 0xffe5,
        PhysicalKey.ShiftLeft or PhysicalKey.ShiftRight => 0xffe1,
        PhysicalKey.ControlLeft or PhysicalKey.ControlRight => 0xffe3,
        PhysicalKey.AltLeft or PhysicalKey.AltRight => 0xffe9,
        PhysicalKey.MetaLeft or PhysicalKey.MetaRight => 0xffeb,
        PhysicalKey.Space => 0x0020,
        PhysicalKey.ArrowUp => 0xff52,
        PhysicalKey.ArrowDown => 0xff54,
        PhysicalKey.ArrowLeft => 0xff51,
        PhysicalKey.ArrowRight => 0xff53,
        PhysicalKey.Home => 0xff50,
        PhysicalKey.End => 0xff57,
        PhysicalKey.PageUp => 0xff55,
        PhysicalKey.PageDown => 0xff56,
        PhysicalKey.Insert => 0xff63,
        PhysicalKey.Delete => 0xffff,
        PhysicalKey.F1 => 0xffbe,
        PhysicalKey.F2 => 0xffbf,
        PhysicalKey.F3 => 0xffc0,
        PhysicalKey.F4 => 0xffc1,
        PhysicalKey.F5 => 0xffc2,
        PhysicalKey.F6 => 0xffc3,
        PhysicalKey.F7 => 0xffc4,
        PhysicalKey.F8 => 0xffc5,
        PhysicalKey.F9 => 0xffc6,
        PhysicalKey.F10 => 0xffc7,
        PhysicalKey.F11 => 0xffc8,
        PhysicalKey.F12 => 0xffc9,
        PhysicalKey.PrintScreen => 0xff61,
        PhysicalKey.ScrollLock => 0xff14,
        PhysicalKey.NumLock => 0xff7f,
        PhysicalKey.ContextMenu => 0xff67,
        _ => 0
    };

    /// <summary>
    /// Maps Avalonia PhysicalKey to XKB keycode (evdev keycode + 8).
    /// </summary>
    private static uint PhysicalKeyToXkb(PhysicalKey key)
    {
        // XKB keycode = evdev keycode + 8
        // evdev keycodes from linux/input-event-codes.h
        return key switch
        {
            PhysicalKey.Escape => 9,
            PhysicalKey.Digit1 => 10,
            PhysicalKey.Digit2 => 11,
            PhysicalKey.Digit3 => 12,
            PhysicalKey.Digit4 => 13,
            PhysicalKey.Digit5 => 14,
            PhysicalKey.Digit6 => 15,
            PhysicalKey.Digit7 => 16,
            PhysicalKey.Digit8 => 17,
            PhysicalKey.Digit9 => 18,
            PhysicalKey.Digit0 => 19,
            PhysicalKey.Minus => 20,
            PhysicalKey.Equal => 21,
            PhysicalKey.Backspace => 22,
            PhysicalKey.Tab => 23,
            PhysicalKey.Q => 24,
            PhysicalKey.W => 25,
            PhysicalKey.E => 26,
            PhysicalKey.R => 27,
            PhysicalKey.T => 28,
            PhysicalKey.Y => 29,
            PhysicalKey.U => 30,
            PhysicalKey.I => 31,
            PhysicalKey.O => 32,
            PhysicalKey.P => 33,
            PhysicalKey.BracketLeft => 34,
            PhysicalKey.BracketRight => 35,
            PhysicalKey.Enter => 36,
            PhysicalKey.ControlLeft => 37,
            PhysicalKey.A => 38,
            PhysicalKey.S => 39,
            PhysicalKey.D => 40,
            PhysicalKey.F => 41,
            PhysicalKey.G => 42,
            PhysicalKey.H => 43,
            PhysicalKey.J => 44,
            PhysicalKey.K => 45,
            PhysicalKey.L => 46,
            PhysicalKey.Semicolon => 47,
            PhysicalKey.Quote => 48,
            PhysicalKey.Backquote => 49,
            PhysicalKey.ShiftLeft => 50,
            PhysicalKey.Backslash => 51,
            PhysicalKey.Z => 52,
            PhysicalKey.X => 53,
            PhysicalKey.C => 54,
            PhysicalKey.V => 55,
            PhysicalKey.B => 56,
            PhysicalKey.N => 57,
            PhysicalKey.M => 58,
            PhysicalKey.Comma => 59,
            PhysicalKey.Period => 60,
            PhysicalKey.Slash => 61,
            PhysicalKey.ShiftRight => 62,
            PhysicalKey.NumPadMultiply => 63,
            PhysicalKey.AltLeft => 64,
            PhysicalKey.Space => 65,
            PhysicalKey.CapsLock => 66,
            PhysicalKey.F1 => 67,
            PhysicalKey.F2 => 68,
            PhysicalKey.F3 => 69,
            PhysicalKey.F4 => 70,
            PhysicalKey.F5 => 71,
            PhysicalKey.F6 => 72,
            PhysicalKey.F7 => 73,
            PhysicalKey.F8 => 74,
            PhysicalKey.F9 => 75,
            PhysicalKey.F10 => 76,
            PhysicalKey.NumLock => 77,
            PhysicalKey.ScrollLock => 78,
            PhysicalKey.NumPad7 => 79,
            PhysicalKey.NumPad8 => 80,
            PhysicalKey.NumPad9 => 81,
            PhysicalKey.NumPadSubtract => 82,
            PhysicalKey.NumPad4 => 83,
            PhysicalKey.NumPad5 => 84,
            PhysicalKey.NumPad6 => 85,
            PhysicalKey.NumPadAdd => 86,
            PhysicalKey.NumPad1 => 87,
            PhysicalKey.NumPad2 => 88,
            PhysicalKey.NumPad3 => 89,
            PhysicalKey.NumPad0 => 90,
            PhysicalKey.NumPadDecimal => 91,
            PhysicalKey.F11 => 95,
            PhysicalKey.F12 => 96,
            PhysicalKey.NumPadEnter => 104,
            PhysicalKey.ControlRight => 105,
            PhysicalKey.NumPadDivide => 106,
            PhysicalKey.PrintScreen => 107,
            PhysicalKey.AltRight => 108,
            PhysicalKey.Home => 110,
            PhysicalKey.ArrowUp => 111,
            PhysicalKey.PageUp => 112,
            PhysicalKey.ArrowLeft => 113,
            PhysicalKey.ArrowRight => 114,
            PhysicalKey.End => 115,
            PhysicalKey.ArrowDown => 116,
            PhysicalKey.PageDown => 117,
            PhysicalKey.Insert => 118,
            PhysicalKey.Delete => 119,
            PhysicalKey.MetaLeft => 133,
            PhysicalKey.MetaRight => 134,
            PhysicalKey.ContextMenu => 135,
            _ => 0
        };
    }
}
