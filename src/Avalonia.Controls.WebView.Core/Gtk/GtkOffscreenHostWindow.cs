using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Avalonia.Controls.Gtk.GtkInterop;

namespace Avalonia.Controls.Gtk;

internal static unsafe class GtkOffscreenHostWindow
{
    private const string TypeName = "AvaloniaWebViewOffscreenHostWindow";

    private static IntPtr s_type;
    private static bool s_registrationFailed;
    
    public static IntPtr TryCreate()
    {
        var type = EnsureRegistered();
        return type == IntPtr.Zero ? IntPtr.Zero : g_object_new_with_properties(type, 0, IntPtr.Zero, IntPtr.Zero);
    }
    
    public static IntPtr GetPixbuf(IntPtr window)
    {
        var gdkWindow = gtk_widget_get_window(window);
        if (gdkWindow == IntPtr.Zero)
            return IntPtr.Zero;

        var surface = gdk_offscreen_window_get_surface(gdkWindow);
        if (surface == IntPtr.Zero)
            return IntPtr.Zero;

        return gdk_pixbuf_get_from_surface(surface, 0, 0, gdk_window_get_width(gdkWindow), gdk_window_get_height(gdkWindow));
    }

    private static IntPtr EnsureRegistered()
    {
        if (s_type != IntPtr.Zero || s_registrationFailed)
            return s_type;

        try
        {
            var existing = g_type_from_name(TypeName);
            if (existing != IntPtr.Zero)
                return s_type = existing;

            // Both parents must be fully initialized before the class_init below reads them.
            g_type_class_ref(gtk_window_get_type());
            g_type_class_ref(gtk_offscreen_window_get_type());

            GTypeQuery windowQuery;
            g_type_query(gtk_window_get_type(), &windowQuery);
            if (windowQuery.class_size == 0 || windowQuery.instance_size == 0)
            {
                s_registrationFailed = true;
                return IntPtr.Zero;
            }

            s_type = g_type_register_static_simple(gtk_window_get_type(), TypeName, windowQuery.class_size,
                (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)&ClassInit, windowQuery.instance_size,
                IntPtr.Zero, 0);
        }
        catch (EntryPointNotFoundException)
        {
            s_type = IntPtr.Zero;
        }
        catch (DllNotFoundException)
        {
            s_type = IntPtr.Zero;
        }

        s_registrationFailed = s_type == IntPtr.Zero;
        return s_type;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ClassInit(IntPtr klass, IntPtr data)
    {
        var windowClass = (IntPtr*)g_type_class_peek(gtk_window_get_type());
        var offscreenClass = (IntPtr*)g_type_class_peek(gtk_offscreen_window_get_type());
        var ownClass = (IntPtr*)klass;

        // Skip the GObjectClass header: its property lists and bookkeeping are per class and must stay ours.
        GTypeQuery objectQuery, windowQuery;
        g_type_query(g_initially_unowned_get_type(), &objectQuery);
        g_type_query(gtk_window_get_type(), &windowQuery);

        var first = (int)(objectQuery.class_size / (uint)IntPtr.Size);
        var end = (int)(windowQuery.class_size / (uint)IntPtr.Size);

        for (var i = first; i < end; i++)
        {
            // Inherited-unchanged here but overridden by GtkOffscreenWindow: one of its vfuncs. A slot that already
            // differs from GtkWindow's is per-class state (GtkWidgetClass.priv), which we must not share.
            if (ownClass[i] == windowClass[i] && offscreenClass[i] != windowClass[i])
                ownClass[i] = offscreenClass[i];
        }
    }
}
