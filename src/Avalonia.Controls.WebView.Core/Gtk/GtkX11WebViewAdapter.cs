using System;
using System.Threading;
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
    private IntPtr _windowHandle;
    private IntPtr _currentParent;

    private GtkX11WebViewAdapter(GtkWebViewEnvironmentRequestedEventArgs environmentArgs) : base(environmentArgs)
    {
        _windowHandle = gtk_window_new(0 /* GTK_WINDOW_TOPLEVEL */);
        gtk_container_add(_windowHandle, WebViewHandle);
        gtk_widget_show_all(WebViewHandle);
        gtk_widget_realize(_windowHandle);
        _x11Window = gdk_x11_window_get_xid(gtk_widget_get_window(_windowHandle));
    }

    public static Task<WebViewAdapter.NativeWebViewAdapterBuilder> CreateBuilder(
        GtkWebViewEnvironmentRequestedEventArgs environmentArgs)
    {
        WebViewAdapter.NativeWebViewAdapterBuilder builder = (parent, _) =>
        {
            WebViewDispatcher.VerifyAccess();
            // The scope has to wrap the RunOnGlibThread call itself: GTK is initialized lazily by the
            // first call, which now happens here rather than in CreateBuilder.
            using var backendScope = EnsureX11GdkBackendForGtkInit();
            var adapter = RunOnGlibThread(() => new GtkX11WebViewAdapter(environmentArgs));
            adapter.SetParent(parent);
            return new WebViewAdapter.AdapterWrapper(adapter, Task.FromResult<IWebViewAdapter>(adapter));
        };

        return Task.FromResult(builder);
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

    protected override void DisposeSafe(bool disposing)
    {
        var window = Interlocked.Exchange(ref _windowHandle, IntPtr.Zero);
        if (window != IntPtr.Zero)
        {
            gtk_container_remove(window, WebViewHandle);
        }

        base.DisposeSafe(disposing);

        if (window != IntPtr.Zero)
        {
            gtk_widget_destroy(window);
        }
    }

    IntPtr IPlatformHandle.Handle => _x11Window;
    string IPlatformHandle.HandleDescriptor => "XID";
}
