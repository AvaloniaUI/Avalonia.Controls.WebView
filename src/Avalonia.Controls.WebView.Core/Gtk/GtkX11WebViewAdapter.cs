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
    private PixelSize _sizeRequest;

    private GtkX11WebViewAdapter(GtkWebViewEnvironmentRequestedEventArgs environmentArgs) : base(environmentArgs)
    {
        _windowHandle = gtk_window_new(0 /* GTK_WINDOW_TOPLEVEL */);
        gtk_container_add(_windowHandle, WebViewHandle);
        gtk_widget_show_all(WebViewHandle);
        gtk_widget_realize(_windowHandle);

        var gdkWindow = gtk_widget_get_window(_windowHandle);
        _x11Window = gdk_x11_window_get_xid(gdkWindow);

        // Reparenting this window into the Avalonia window takes it away from the window manager,
        // so the _NET_WM_FRAME_DRAWN replies GDK throttles drawing on never arrive.
        // Without this GTK paints one frame and then waits forever.
        gdk_x11_window_set_frame_sync_enabled(gdkWindow, false);
    }

    public static Task<WebViewAdapter.NativeWebViewAdapterBuilder> CreateBuilder(
        GtkWebViewEnvironmentRequestedEventArgs environmentArgs)
    {
        WebViewAdapter.NativeWebViewAdapterBuilder builder = (parent, _) =>
        {
            WebViewDispatcher.VerifyAccess();
            using var backendScope = PrepareGdkBackendForGtkInit(environmentArgs.ForceX11GdkBackend);
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

    public override void SizeChanged(PixelSize containerSize)
    {
        if (containerSize.Width <= 0 || containerSize.Height <= 0)
            return;

        _sizeRequest = containerSize;
        RunOnGlibThreadAsync(() =>
        {
            if (_windowHandle == IntPtr.Zero)
                return;

            gtk_window_resize(_windowHandle, _sizeRequest.Width, _sizeRequest.Height);
        });
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
