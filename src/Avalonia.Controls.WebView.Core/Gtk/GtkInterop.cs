using System;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Gtk;

internal static unsafe partial class GtkInterop
{
    private const string LibGObject = "libgobject-2.0.so.0";
    private const string LibWebKit = "libwebkit2gtk-4.1.so.0";
    private const string LibGio = "libgio-2.0.so.0";
    private const string LibGtk = "libgtk-3.so.0";
    private const string LibGdk = "libgdk-3.so.0";

    [DllImport(LibWebKit)]
    internal static extern IntPtr webkit_web_view_get_uri(IntPtr webView);

    [DllImport(LibGio)]
    internal static extern void g_free(IntPtr ptr);

    [DllImport(LibWebKit)]
    internal static extern IntPtr webkit_web_view_new();

#if NET7_0_OR_GREATER
    [LibraryImport(LibWebKit, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void webkit_web_view_load_uri(IntPtr webView, string uri);

    [LibraryImport(LibWebKit, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void webkit_web_view_load_html(IntPtr webView, string content, string? baseUri);

    [LibraryImport(LibWebKit, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void webkit_web_view_run_javascript(IntPtr webView, string script, IntPtr cancellable, IntPtr callback, IntPtr userData);
#else
    [DllImport(LibWebKit)]
    internal static extern void webkit_web_view_load_uri(IntPtr webView, string uri);

    [DllImport(LibWebKit)]
    internal static extern void webkit_web_view_load_html(IntPtr webView, string content, string? baseUri);

    [DllImport(LibWebKit)]
    internal static extern void webkit_web_view_run_javascript(IntPtr webView, string script, IntPtr cancellable, IntPtr callback, IntPtr userData);
#endif

    [DllImport(LibWebKit)]
    internal static extern bool webkit_web_view_can_go_back(IntPtr webView);

    [DllImport(LibWebKit)]
    internal static extern bool webkit_web_view_can_go_forward(IntPtr webView);

    [DllImport(LibWebKit)]
    internal static extern void webkit_web_view_go_back(IntPtr webView);

    [DllImport(LibWebKit)]
    internal static extern void webkit_web_view_go_forward(IntPtr webView);

    [DllImport(LibWebKit)]
    internal static extern void webkit_web_view_reload(IntPtr webView);

    [DllImport(LibWebKit)]
    internal static extern void webkit_web_view_stop_loading(IntPtr webView);

    [DllImport(LibWebKit)]
    public static extern nint webkit_web_view_run_javascript_finish(IntPtr webView, IntPtr result, GError** error);

    [DllImport(LibWebKit)]
    public static extern void webkit_javascript_result_unref(IntPtr jsResult);

    [DllImport(LibWebKit)]
    public static extern nint webkit_javascript_result_get_js_value(IntPtr jsResult);

    [DllImport(LibWebKit)]
    public static extern nint jsc_value_to_string(IntPtr value);

    [DllImport(LibGtk)]
    internal static extern IntPtr gtk_scrolled_window_new(IntPtr hadjustment, IntPtr vadjustment);

    [DllImport(LibGtk)]
    internal static extern void gtk_container_add(IntPtr container, IntPtr widget);

    [DllImport(LibGtk)]
    internal static extern void gtk_widget_show_all(IntPtr widget);

    [DllImport(LibGtk)]
    internal static extern void gtk_widget_destroy(IntPtr widget);

    [DllImport(LibGObject)]
    internal static extern ulong g_error_free(GError* error);

    [DllImport(LibGtk)]
    internal static extern IntPtr gtk_window_new(int type);

    [DllImport(LibGtk)]
    internal static extern void gtk_window_resize(IntPtr window, int width, int height);

    [DllImport(LibGtk)]
    internal static extern void gtk_window_move(IntPtr window, int x, int y);

    [DllImport(LibGtk)]
    internal static extern void gtk_window_set_resizable(IntPtr window, bool value);

    [DllImport(LibGtk)]
    internal static extern bool gtk_window_get_resizable(IntPtr window);

    [DllImport(LibGtk)]
    internal static extern void gtk_window_set_position(IntPtr window, int positionType);

    [DllImport(LibGtk)]
    internal static extern void gtk_widget_realize(IntPtr gtkWidget);

    [DllImport(LibGtk)]
    internal static extern IntPtr gtk_widget_get_window(IntPtr gtkWidget);

    [DllImport(LibGdk)]
    internal static extern void gdk_window_set_transient_for(IntPtr window, IntPtr parent);

    [DllImport(LibGdk)]
    internal static extern IntPtr gdk_x11_window_foreign_new_for_display(IntPtr display, IntPtr xid);

    [DllImport(LibGdk)]
    internal static extern IntPtr gdk_display_get_default();

    [DllImport(LibGtk)]
    internal static extern IntPtr gtk_window_get_title(IntPtr window);

#if NET7_0_OR_GREATER
    [LibraryImport(LibGtk, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void gtk_window_set_title(IntPtr window, string title);
#else
    [DllImport(LibGtk)]
    internal static extern void gtk_window_set_title(IntPtr window, string title);
#endif

    [DllImport(LibGtk)]
    internal static extern void gtk_window_set_default_size(IntPtr window, int width, int height);

    [DllImport(LibGtk)]
    internal static extern void gtk_window_present(IntPtr window);

#if NET7_0_OR_GREATER
    [LibraryImport(LibGObject, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial ulong g_signal_connect_data(nint instance, string detailed_signal, nint c_handler, nint data, nint destroy_data, GConnectFlags connect_flags);
#else
    [DllImport(LibGObject)]
    internal static extern ulong g_signal_connect_data(nint instance, string detailed_signal, nint c_handler, nint data, nint destroy_data, GConnectFlags connect_flags);
#endif

    [DllImport(LibGObject)]
    internal static extern void g_signal_handler_disconnect(IntPtr instance, ulong handlerId);

    [DllImport(LibWebKit)]
    internal static extern IntPtr webkit_navigation_policy_decision_get_navigation_action(IntPtr decision);

    [DllImport(LibWebKit)]
    internal static extern IntPtr webkit_navigation_action_get_request(IntPtr navigation_action);

    [DllImport(LibWebKit)]
    internal static extern IntPtr webkit_uri_request_get_uri(IntPtr request);

    internal struct GError
    {
        public uint Domain;
        public int Code;
        public nint Message;
    }

    public enum GConnectFlags : int
    {
        AFTER,
        SWAPPED
    }
}
