using System;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform;
using static Avalonia.Controls.Gtk.GtkInterop;
using static Avalonia.Controls.Gtk.AvaloniaGtk;
using static Avalonia.Controls.Gtk.X11Interop;

namespace Avalonia.Controls.Gtk;

internal sealed class GtkX11WebViewAdapter : GtkWebViewAdapter, IPlatformHandle
{
    private static readonly IntPtr s_display = XOpenDisplay(IntPtr.Zero);

    private readonly IntPtr _x11Window;
    private readonly IntPtr _windowHandle;
    private IntPtr _currentParent;

    private GtkX11WebViewAdapter(GtkWebViewEnvironmentRequestedEventArgs environmentArgs) : base(environmentArgs)
    {
        _windowHandle = gtk_window_new(0 /* GTK_WINDOW_TOPLEVEL */);
        gtk_container_add(_windowHandle, WebViewHandle);
        gtk_widget_show_all(WebViewHandle);
        gtk_widget_realize(_windowHandle);
        _x11Window = gdk_x11_window_get_xid(gtk_widget_get_window(_windowHandle));
    }

    public static async Task<WebViewAdapter.NativeWebViewAdapterBuilder> CreateBuilder(
        GtkWebViewEnvironmentRequestedEventArgs environmentArgs)
    {
        using var _ = EnsureX11GdkBackendForGtkInit();
        var adapter = await RunOnGlibThreadAsync(() => new GtkX11WebViewAdapter(environmentArgs));
        return (parent, _) =>
        {
            WebViewDispatcher.VerifyAccess();
            adapter.SetParent(parent);
            return new WebViewAdapter.AdapterWrapper(adapter, Task.FromResult<IWebViewAdapter>(adapter));
        };
    }

    public override void SetParent(IPlatformHandle parent)
    {
        if (parent.HandleDescriptor != "XID")
            throw new InvalidOperationException("Parent is not supported");

        if (s_display == IntPtr.Zero)
            throw new Exception("XOpenDisplay failed");

        if (_currentParent != parent.Handle)
        {
            _currentParent = parent.Handle;

            XReparentWindow(s_display, _x11Window, parent.Handle, 0, 0);
            _ = XFlush(s_display);
            XSync(s_display, false);

            _ = XMapWindow(s_display, _x11Window);
            _ = XRaiseWindow(s_display, parent.Handle);

            RunOnGlibThreadAsync(() => gtk_widget_show_all(_windowHandle));
        }
    }

    public override Color DefaultBackground
    {
        set
        {
            // Transparency doesn't seem to work well
            // var screen = gtk_window_get_screen (_windowHandle);
            // var rgbaVisual = gdk_screen_get_rgba_visual (screen);
            //
            // if (rgbaVisual == IntPtr.Zero)
            //     return;
            //
            // gtk_widget_set_visual (_windowHandle, rgbaVisual);
            // gtk_widget_set_app_paintable (_windowHandle, true);

            base.DefaultBackground = value;
        }
    }

    IntPtr IPlatformHandle.Handle => _x11Window;
    string IPlatformHandle.HandleDescriptor => "XID";
}
