using System;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Linux.Interop;

internal static unsafe partial class WpePlatformInterop
{
    private const string LibWpeWebKit = "libWPEWebKit-2.0.so.1";

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr wpe_display_get_type();

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr wpe_display_headless_new();

    [LibraryImport(LibWpeWebKit)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool wpe_display_connect(IntPtr display, IntPtr* error);

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr wpe_view_headless_get_type();

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr wpe_toplevel_headless_get_type();

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr webkit_web_view_get_wpe_view(IntPtr webView);

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr wpe_view_get_toplevel(IntPtr view);

    [LibraryImport(LibWpeWebKit)]
    public static partial void wpe_view_set_toplevel(IntPtr view, IntPtr toplevel);

    [LibraryImport(LibWpeWebKit)]
    public static partial void wpe_view_set_visible(IntPtr view, [MarshalAs(UnmanagedType.Bool)] bool visible);

    [LibraryImport(LibWpeWebKit)]
    public static partial void wpe_view_focus_in(IntPtr view);

    [LibraryImport(LibWpeWebKit)]
    public static partial void wpe_view_event(IntPtr view, IntPtr @event);

    [LibraryImport(LibWpeWebKit)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool wpe_toplevel_resize(IntPtr toplevel, int width, int height);

    [LibraryImport(LibWpeWebKit)]
    public static partial void wpe_toplevel_scale_changed(IntPtr toplevel, double scale);

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr wpe_event_pointer_button_new(
        WpeEventType type, IntPtr view, WpeInputSource source, uint time, uint modifiers,
        uint button, double x, double y, uint pressCount);

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr wpe_event_pointer_move_new(
        WpeEventType type, IntPtr view, WpeInputSource source, uint time, uint modifiers,
        double x, double y, double deltaX, double deltaY);

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr wpe_event_scroll_new(
        IntPtr view, WpeInputSource source, uint time, uint modifiers,
        double deltaX, double deltaY,
        [MarshalAs(UnmanagedType.Bool)] bool preciseDeltas,
        [MarshalAs(UnmanagedType.Bool)] bool isStop,
        double x, double y);

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr wpe_event_keyboard_new(
        WpeEventType type, IntPtr view, WpeInputSource source, uint time, uint modifiers,
        uint keycode, uint keyval);

    [LibraryImport(LibWpeWebKit)]
    public static partial void wpe_event_unref(IntPtr @event);

    [LibraryImport(LibWpeWebKit)]
    public static partial uint wpe_unicode_to_keyval(uint wc);

    [LibraryImport(LibWpeWebKit)]
    public static partial int wpe_buffer_get_width(IntPtr buffer);

    [LibraryImport(LibWpeWebKit)]
    public static partial int wpe_buffer_get_height(IntPtr buffer);

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr wpe_buffer_import_to_pixels(IntPtr buffer, IntPtr* error);

    [LibraryImport(LibWpeWebKit)]
    public static partial IntPtr wpe_buffer_dma_buf_get_type();

    [LibraryImport(LibWpeWebKit)]
    public static partial uint wpe_buffer_dma_buf_get_format(IntPtr buffer);

    private const string LibGObject = "libgobject-2.0.so.0";
    private const string LibGLib = "libglib-2.0.so.0";

    [LibraryImport(LibGObject)]
    public static partial void g_type_query(IntPtr type, GTypeQuery* query);

    [LibraryImport(LibGObject, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr g_type_register_static_simple(
        IntPtr parentType, string typeName, uint classSize,
        delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> classInit,
        uint instanceSize, IntPtr instanceInit, int flags);

    [LibraryImport(LibGObject)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool g_type_check_instance_is_a(IntPtr instance, IntPtr type);

    [LibraryImport(LibGObject)]
    public static partial void g_signal_handler_disconnect(IntPtr instance, ulong handlerId);

    [LibraryImport(LibGLib)]
    public static partial IntPtr g_bytes_get_data(IntPtr bytes, nuint* size);

    [LibraryImport(LibGLib)]
    public static partial void g_error_free(IntPtr error);
}
