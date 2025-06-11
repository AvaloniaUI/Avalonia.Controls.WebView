using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Avalonia.Platform;
using static Avalonia.Controls.Gtk.GtkInterop;
using static Avalonia.Controls.Gtk.AvaloniaGtk;
using static Avalonia.Controls.Gtk.X11Interop;

namespace Avalonia.Controls.Gtk;

internal class GtkX11WebViewAdapter(IPlatformHandle parent) : GtkWebViewAdapter, IPlatformHandle
{
    private IntPtr _x11Window;
    private IntPtr _windowHandle;

    // GTK thread
    protected override void InitializeSafe()
    {
        _windowHandle = gtk_window_new(0 /* GTK_WINDOW_TOPLEVEL */);
        base.InitializeSafe(); // creates WebKitWebView object and subscribes to signals

        gtk_container_add(_windowHandle, WebViewHandle);
        gtk_widget_show_all(WebViewHandle);
        gtk_widget_realize(_windowHandle);
        _x11Window = gdk_x11_window_get_xid(gtk_widget_get_window(_windowHandle));
    }

    // Avalonia UI thread
    protected override void OnInitialized()
    {
        if (parent.HandleDescriptor != "XID")
            throw new InvalidOperationException("Parent is not supported");

        var display = GetDisplay(parent);

        XReparentWindow(display, _x11Window, parent.Handle, 0, 0);
        _ = XFlush(display);
        XSync(display, false);

        _ = XMapWindow(display, _x11Window);
        _ = XRaiseWindow(display, parent.Handle);

        RunOnGlibThreadAsync(() => gtk_widget_show_all(_windowHandle));
        base.OnInitialized();
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicFields, "X11NativeControlHost+DumbWindow", "Avalonia.X11")]
    private static IntPtr GetDisplay(IPlatformHandle handle) => (IntPtr)handle.GetType()
        .GetField("_display", BindingFlags.Instance | BindingFlags.NonPublic)?
        .GetValue(handle)!;

    IntPtr IPlatformHandle.Handle => _x11Window;
    string IPlatformHandle.HandleDescriptor => "XID";
}
