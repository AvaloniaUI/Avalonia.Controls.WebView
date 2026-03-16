using System;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Linux.Interop;

internal static unsafe partial class WpeInterop
{
    // --- libwpe-1.0 ---

    private const string LibWpe = "libwpe-1.0.so.1";

    [LibraryImport(LibWpe)]
    public static partial void wpe_view_backend_dispatch_keyboard_event(
        IntPtr viewBackend, WpeInputKeyboardEvent* @event);

    [LibraryImport(LibWpe)]
    public static partial void wpe_view_backend_dispatch_pointer_event(
        IntPtr viewBackend, WpeInputPointerEvent* @event);

    [LibraryImport(LibWpe)]
    public static partial void wpe_view_backend_dispatch_axis_event(
        IntPtr viewBackend, WpeInputAxisEvent* @event);

    [LibraryImport(LibWpe)]
    public static partial void wpe_view_backend_add_activity_state(
        IntPtr viewBackend, uint state);

    [LibraryImport(LibWpe)]
    public static partial void wpe_view_backend_remove_activity_state(
        IntPtr viewBackend, uint state);

    [LibraryImport(LibWpe)]
    public static partial void wpe_view_backend_dispatch_set_size(
        IntPtr viewBackend, uint width, uint height);

    [LibraryImport(LibWpe)]
    public static partial void wpe_view_backend_dispatch_set_device_scale_factor(
        IntPtr viewBackend, float factor);

    [LibraryImport(LibWpe)]
    public static partial uint wpe_unicode_to_key_code(uint unicode);

    // --- libWPEBackend-fdo-1.0 ---

    private const string LibWpeBackendFdo = "libWPEBackend-fdo-1.0.so.1";

    [LibraryImport(LibWpeBackendFdo)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool wpe_fdo_initialize_shm();

    [LibraryImport(LibWpeBackendFdo)]
    public static partial IntPtr wpe_view_backend_exportable_fdo_create(
        WpeViewBackendExportableFdoClient* client, IntPtr userData, uint width, uint height);

    [LibraryImport(LibWpeBackendFdo)]
    public static partial IntPtr wpe_view_backend_exportable_fdo_get_view_backend(
        IntPtr exportable);

    [LibraryImport(LibWpeBackendFdo)]
    public static partial void wpe_view_backend_exportable_fdo_dispatch_frame_complete(
        IntPtr exportable);

    [LibraryImport(LibWpeBackendFdo)]
    public static partial void wpe_view_backend_exportable_fdo_dispatch_release_shm_exported_buffer(
        IntPtr exportable, IntPtr buffer);

    [LibraryImport(LibWpeBackendFdo)]
    public static partial void wpe_view_backend_exportable_fdo_destroy(
        IntPtr exportable);

    // --- libWPEWebKit-2.0 ---

    private const string LibWpeWebKit = "libWPEWebKit-2.0.so.1";

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr webkit_web_view_backend_new(
        IntPtr viewBackend, IntPtr notifyFunc, IntPtr userData);

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr webkit_web_view_new(IntPtr backend);

    [LibraryImport(LibWpeWebKit, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void webkit_web_view_load_uri(IntPtr webView, string uri);

    [LibraryImport(LibWpeWebKit, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void webkit_web_view_load_html(
        IntPtr webView, string content, string? baseUri);

    [LibraryImport(LibWpeWebKit)]
    public static partial void webkit_web_view_go_back(IntPtr webView);

    [LibraryImport(LibWpeWebKit)]
    public static partial void webkit_web_view_go_forward(IntPtr webView);

    [LibraryImport(LibWpeWebKit)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool webkit_web_view_can_go_back(IntPtr webView);

    [LibraryImport(LibWpeWebKit)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool webkit_web_view_can_go_forward(IntPtr webView);

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr webkit_web_view_get_uri(IntPtr webView);

    [LibraryImport(LibWpeWebKit)]
    public static partial void webkit_web_view_stop_loading(IntPtr webView);

    [LibraryImport(LibWpeWebKit)]
    public static partial void webkit_web_view_reload(IntPtr webView);

    [LibraryImport(LibWpeWebKit, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void webkit_web_view_evaluate_javascript(
        IntPtr webView, string script, nint length,
        string? worldName, string? sourceUri,
        IntPtr cancellable,
        delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void> callback,
        IntPtr userData);

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr webkit_web_view_evaluate_javascript_finish(
        IntPtr webView, IntPtr result, IntPtr* error);

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr webkit_web_view_get_settings(IntPtr webView);

    [LibraryImport(LibWpeWebKit)]
    public static partial void webkit_web_view_set_background_color(
        IntPtr webView, WebKitColor* color);

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr webkit_web_view_get_user_content_manager(IntPtr webView);

    [LibraryImport(LibWpeWebKit, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool webkit_user_content_manager_register_script_message_handler(
        IntPtr manager, string name, string? worldName);

    [LibraryImport(LibWpeWebKit, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void webkit_settings_set_user_agent(IntPtr settings, string? userAgent);

    [LibraryImport(LibWpeWebKit)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool webkit_settings_set_enable_developer_extras(
        IntPtr settings, [MarshalAs(UnmanagedType.Bool)] bool enabled);

    [LibraryImport(LibWpeWebKit)]
    public static partial uint webkit_get_major_version();

    [LibraryImport(LibWpeWebKit)]
    public static partial uint webkit_get_minor_version();

    [LibraryImport(LibWpeWebKit)]
    public static partial uint webkit_get_micro_version();

    // Navigation policy decision
    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr webkit_navigation_policy_decision_get_navigation_action(IntPtr decision);

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr webkit_navigation_action_get_request(IntPtr action);

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr webkit_uri_request_get_uri(IntPtr request);

    [LibraryImport(LibWpeWebKit)]
    public static partial void webkit_policy_decision_ignore(IntPtr decision);

    [LibraryImport(LibWpeWebKit)]
    public static partial void webkit_policy_decision_use(IntPtr decision);

    // Network session and cookie manager
    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr webkit_network_session_get_default();

    [LibraryImport(LibWpeWebKit, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr webkit_network_session_new(string? dataDirectory, string? cacheDirectory);

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr webkit_network_session_get_cookie_manager(IntPtr session);

    [LibraryImport(LibWpeWebKit)]
    public static partial void webkit_cookie_manager_add_cookie(
        IntPtr cookieManager, IntPtr cookie, IntPtr cancellable,
        delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void> callback, IntPtr userData);

    [LibraryImport(LibWpeWebKit)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool webkit_cookie_manager_add_cookie_finish(
        IntPtr cookieManager, IntPtr result, IntPtr* error);

    [LibraryImport(LibWpeWebKit)]
    public static partial void webkit_cookie_manager_get_all_cookies(
        IntPtr cookieManager, IntPtr cancellable,
        delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void> callback, IntPtr userData);

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr webkit_cookie_manager_get_all_cookies_finish(
        IntPtr cookieManager, IntPtr result, IntPtr* error);

    [LibraryImport(LibWpeWebKit)]
    public static partial void webkit_cookie_manager_delete_cookie(
        IntPtr cookieManager, IntPtr cookie, IntPtr cancellable,
        delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void> callback, IntPtr userData);

    [LibraryImport(LibWpeWebKit)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool webkit_cookie_manager_delete_cookie_finish(
        IntPtr cookieManager, IntPtr result, IntPtr* error);

    // --- GLib/GObject ---

    private const string LibGObject = "libgobject-2.0.so.0";
    private const string LibGLib = "libglib-2.0.so.0";

    [LibraryImport(LibGObject, StringMarshalling = StringMarshalling.Utf8)]
    public static partial ulong g_signal_connect_data(
        IntPtr instance, string detailedSignal,
        IntPtr handler, IntPtr data,
        IntPtr destroyData, int connectFlags);

    [LibraryImport(LibGObject)]
    public static partial void g_object_unref(IntPtr obj);

    [LibraryImport(LibGObject)]
    public static partial IntPtr g_object_ref(IntPtr obj);

    [LibraryImport(LibGLib)]
    public static partial IntPtr g_main_context_default();

    [LibraryImport(LibGLib)]
    public static partial IntPtr g_main_context_ref_thread_default();

    [LibraryImport(LibGLib)]
    public static partial void g_main_context_unref(IntPtr context);

    [LibraryImport(LibGLib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool g_main_context_iteration(IntPtr context, [MarshalAs(UnmanagedType.Bool)] bool mayBlock);

    [LibraryImport(LibGLib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool g_main_context_pending(IntPtr context);

    [LibraryImport(LibGLib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool g_main_context_acquire(IntPtr context);

    [LibraryImport(LibGLib)]
    public static partial void g_main_context_release(IntPtr context);

    [LibraryImport(LibGLib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool g_main_context_prepare(IntPtr context, int* priority);

    [LibraryImport(LibGLib)]
    public static partial int g_main_context_query(IntPtr context, int maxPriority, int* timeout, GPollFD* fds, int nFds);

    [LibraryImport(LibGLib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool g_main_context_check(IntPtr context, int maxPriority, GPollFD* fds, int nFds);

    [LibraryImport(LibGLib)]
    public static partial void g_main_context_dispatch(IntPtr context);

    [LibraryImport(LibGLib)]
    public static partial void g_free(IntPtr mem);

    [LibraryImport(LibGLib)]
    public static partial IntPtr g_list_nth_data(IntPtr list, uint n);

    [LibraryImport(LibGLib)]
    public static partial uint g_list_length(IntPtr list);

    [LibraryImport(LibGLib)]
    public static partial void g_list_free(IntPtr list);

    // --- JSC ---

    private const string LibJsc = "libjavascriptcoregtk-4.1.so.0"; // WPE WebKit 2.0 uses JSC 4.1

    // Try loading from the WPE WebKit library itself first
    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr jsc_value_to_string(IntPtr value);

    [LibraryImport(LibWpeWebKit)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool jsc_value_is_undefined(IntPtr value);

    [LibraryImport(LibWpeWebKit)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool jsc_value_is_null(IntPtr value);

    [LibraryImport(LibWpeWebKit)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool jsc_value_is_string(IntPtr value);

    // --- libsoup-3.0 ---

    private const string LibSoup = "libsoup-3.0.so.0";

    [LibraryImport(LibSoup, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr soup_cookie_new(
        string name, string value, string domain, string path, int maxAge);

    [LibraryImport(LibSoup)]
    public static partial void soup_cookie_free(IntPtr cookie);

    [LibraryImport(LibSoup)]
    public static partial IntPtr soup_cookie_get_name(IntPtr cookie);

    [LibraryImport(LibSoup)]
    public static partial IntPtr soup_cookie_get_value(IntPtr cookie);

    [LibraryImport(LibSoup)]
    public static partial IntPtr soup_cookie_get_domain(IntPtr cookie);

    [LibraryImport(LibSoup)]
    public static partial IntPtr soup_cookie_get_path(IntPtr cookie);

    [LibraryImport(LibSoup)]
    public static partial IntPtr soup_cookie_get_expires(IntPtr cookie);

    [LibraryImport(LibSoup)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool soup_cookie_get_secure(IntPtr cookie);

    [LibraryImport(LibSoup)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool soup_cookie_get_http_only(IntPtr cookie);

    [LibraryImport(LibSoup)]
    public static partial void soup_cookie_set_secure(
        IntPtr cookie, [MarshalAs(UnmanagedType.Bool)] bool secure);

    [LibraryImport(LibSoup)]
    public static partial void soup_cookie_set_http_only(
        IntPtr cookie, [MarshalAs(UnmanagedType.Bool)] bool httpOnly);

    // --- GDateTime (for soup cookie expires) ---

    [LibraryImport(LibGLib)]
    public static partial long g_date_time_to_unix(IntPtr datetime);

    [LibraryImport(LibGLib)]
    public static partial void g_date_time_unref(IntPtr datetime);

    // --- Wayland SHM buffer accessors (libwayland-server) ---

    private const string LibWaylandServer = "libwayland-server.so.0";

    [LibraryImport(LibWaylandServer)]
    public static partial IntPtr wl_shm_buffer_get_data(IntPtr buffer);

    [LibraryImport(LibWaylandServer)]
    public static partial int wl_shm_buffer_get_stride(IntPtr buffer);

    [LibraryImport(LibWaylandServer)]
    public static partial int wl_shm_buffer_get_width(IntPtr buffer);

    [LibraryImport(LibWaylandServer)]
    public static partial int wl_shm_buffer_get_height(IntPtr buffer);

    [LibraryImport(LibWaylandServer)]
    public static partial uint wl_shm_buffer_get_format(IntPtr buffer);

    [LibraryImport(LibWaylandServer)]
    public static partial void wl_shm_buffer_begin_access(IntPtr buffer);

    [LibraryImport(LibWaylandServer)]
    public static partial void wl_shm_buffer_end_access(IntPtr buffer);
}
