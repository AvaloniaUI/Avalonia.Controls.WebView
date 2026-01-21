using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Platform;
using static Avalonia.Controls.Gtk.GtkInterop;
using static Avalonia.Controls.Gtk.AvaloniaGtk;

namespace Avalonia.Controls.Gtk;

internal sealed class GtkNativeWebViewDialog : INativeWebViewDialog, IGtkWebViewPlatformHandle
{
    private static readonly unsafe IntPtr s_deleteEventCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, int>)&DeleteEvent);

    private GtkWebViewAdapter? _nativeWebView;
    private IntPtr _windowHandle;
    private bool _disposed;
    private GtkSignal? _signal;

    private bool _canUserResizeTemp = true;
    private bool _isShown;

    private sealed class DialogGtkWebViewAdapter(GtkWebViewEnvironmentRequestedEventArgs args)
        : GtkWebViewAdapter(args);

    private GtkNativeWebViewDialog(GtkWebViewEnvironmentRequestedEventArgs args)
    {
        _windowHandle = gtk_window_new(0 /* GTK_WINDOW_TOPLEVEL */);
        gtk_window_set_default_size(_windowHandle, 800, 600);
        // Type hint will be set in Show() based on CanUserResize
        _signal = new GtkSignal(_windowHandle, "delete-event", s_deleteEventCallback, this);
        var scrolled = gtk_scrolled_window_new(IntPtr.Zero, IntPtr.Zero);

        var nativeWebView = new DialogGtkWebViewAdapter(args);
        gtk_container_add(scrolled, nativeWebView.WebViewHandle);
        gtk_container_add(_windowHandle, scrolled);
        _nativeWebView = nativeWebView;
    }

    public static async Task<INativeWebViewDialog> CreateAsync(
        Action<WebViewEnvironmentRequestedEventArgs> environmentRequested)
    {
        var deferralManager = new DeferralManager();
        var args = new GtkWebViewEnvironmentRequestedEventArgs(deferralManager);
        environmentRequested(args);
        await deferralManager.WaitForDeferralsAsync();
        if (CheckAccess())
            return new GtkNativeWebViewDialog(args);
        return await RunOnGlibThreadAsync(() => new GtkNativeWebViewDialog(args));
    }

    public bool CanUserResize
    {
        get => _isShown ? RunOnGlibThread(() => gtk_window_get_resizable(_windowHandle)) : _canUserResizeTemp;
        set
        {
            if (_isShown)
            {
                RunOnGlibThreadAsync(() =>
                {
                    gtk_window_set_resizable(_windowHandle, value);
                    var window = gtk_widget_get_window(_windowHandle);

                    if (!value)
                    {
                        ApplyNonResizableSettings(window);
                    }
                    else
                    {
                        // Restore all decorations and functions
                        gdk_window_set_decorations(window, GdkWMDecoration.GDK_DECOR_ALL);
                        gdk_window_set_functions(window, GdkWMFunction.GDK_FUNC_ALL);
                    }
                });
            }
            else
            {
                _canUserResizeTemp = value;
            }
        }
    }

    public IWebViewAdapter? TryGetAdapter() => _nativeWebView;

    public event EventHandler? Closing;
    public event EventHandler<WebViewAdapterEventArgs>? AdapterCreated;
    public event EventHandler<WebViewAdapterEventArgs>? AdapterDestroyed;

    public Color DefaultBackground
    {
        set
        {
            // Transparency doesn't seem to work well
            // var screen = gtk_window_get_screen (_windowHandle);
            // var rgba_visual = gdk_screen_get_rgba_visual (screen);
            //
            // if (rgba_visual == IntPtr.Zero)
            //     return;
            //
            // gtk_widget_set_visual (_windowHandle, rgba_visual);
            // gtk_widget_set_app_paintable (_windowHandle, true);

            _nativeWebView!.DefaultBackground = value;
        }
    }

    public string? Title
    {
        get => RunOnGlibThread(() =>
        {
            var titlePtr = gtk_window_get_title(_windowHandle);
            if (titlePtr == IntPtr.Zero)
            {
                return null;
            }

#if NET5_0_OR_GREATER
            return Marshal.PtrToStringUTF8(titlePtr);
#else
            // Custom UTF8 conversion
            var length = 0;
            while (Marshal.ReadByte(titlePtr, length) != 0)
            {
                length++;
            }

            var buffer = new byte[length];
            Marshal.Copy(titlePtr, buffer, 0, length);
            return System.Text.Encoding.UTF8.GetString(buffer);
#endif
        });
        set => RunOnGlibThreadAsync(() => gtk_window_set_title(_windowHandle, value ?? string.Empty));
    }

    public void Show() => RunOnGlibThreadAsync(() =>
    {
        // Always use DIALOG hint for proper focus/input
        gtk_window_set_type_hint(_windowHandle, 3); // GDK_WINDOW_TYPE_HINT_DIALOG

        if (!_canUserResizeTemp)
        {
            gtk_window_set_resizable(_windowHandle, false);
        }

        gtk_widget_realize(_windowHandle);
        var window = gtk_widget_get_window(_windowHandle);

        if (!_canUserResizeTemp)
        {
            ApplyNonResizableSettings(window);
        }

        gtk_widget_show_all(_windowHandle);
        gtk_window_present(_windowHandle);
        _isShown = true;
    });

    public bool Show(IPlatformHandle owner)
    {
        if (owner.HandleDescriptor != "XID")
        {
            return false;
        }

        RunOnGlibThreadAsync(() =>
        {
            var xid = owner.Handle;
            var parent = gdk_x11_window_foreign_new_for_display(gdk_display_get_default(), xid);

            // Set window properties BEFORE realizing the window
            gtk_window_set_position(_windowHandle, 4); // GTK_WIN_POS_CENTER_ON_PARENT
            // Always use DIALOG hint for proper focus/input
            gtk_window_set_type_hint(_windowHandle, 3); // GDK_WINDOW_TYPE_HINT_DIALOG
            gtk_window_set_modal(_windowHandle, false); // Set to true for modal dialogs

            if (!_canUserResizeTemp)
            {
                gtk_window_set_resizable(_windowHandle, false);
            }

            gtk_widget_realize(_windowHandle);
            var window = gtk_widget_get_window(_windowHandle);
            if (parent != IntPtr.Zero)
            {
                gdk_window_set_transient_for(window, parent);
            }

            // Apply decorations and geometry hints after window is realized
            if (!_canUserResizeTemp)
            {
                ApplyNonResizableSettings(window);
            }

            gtk_widget_show_all(_windowHandle);
            gtk_window_present(_windowHandle);
            _isShown = true;
        });
        return true;
    }

    public void Close()
    {
        // Closing is invoked in two places for the GTK webview:
        // 1. Here, on explicit Close call. We notify about Closing before even sending close request to the GTK.
        // 2. In the `DeleteEvent` handler, which is raised when GTK window is being closed.
        // If GTK window was closed due to window dispose (like in Close call), it would be too late for any cancellation
        // So we raise this event here early.
        var cancel = new CancelEventArgs();
        Closing?.Invoke(this, cancel);
        if (cancel.Cancel)
            return;

        Dispose(true);
    }

    public bool Resize(int width, int height)
    {
        RunOnGlibThreadAsync(() =>
        {
            // If window is non-resizable, update geometry hints to allow the new size
            if (!CanUserResize && _isShown)
            {
                var geom = new GdkGeometry
                {
                    min_width = width,
                    min_height = height,
                    max_width = width,
                    max_height = height,
                    base_width = width,
                    base_height = height
                };
                gtk_window_set_geometry_hints(_windowHandle, IntPtr.Zero, ref geom,
                    GdkWindowHints.GDK_HINT_MIN_SIZE | GdkWindowHints.GDK_HINT_MAX_SIZE |
                    GdkWindowHints.GDK_HINT_BASE_SIZE);
            }

            gtk_window_resize(_windowHandle, width, height);
        });
        return true;
    }

    public bool Move(int x, int y)
    {
        RunOnGlibThreadAsync(() => gtk_window_move(_windowHandle, x, y));
        return true;
    }

    public IPlatformHandle? TryGetPlatformHandle() => this;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~GtkNativeWebViewDialog()
    {
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;

            if (Interlocked.Exchange(ref _windowHandle, IntPtr.Zero) is var windowHandle
                && windowHandle != IntPtr.Zero)
            {
                RunOnGlibThreadAsync(() => gtk_widget_destroy(windowHandle));
            }

            Interlocked.Exchange(ref _signal, null)?.Dispose();

            try
            {
                AdapterDestroyed?.Invoke(this, new WebViewAdapterEventArgs(_nativeWebView));
            }
            finally
            {
                Interlocked.Exchange(ref _nativeWebView, null)?.Dispose();
            }
        }
    }

    private void ApplyNonResizableSettings(IntPtr gdkWindow)
    {
        // Get the current size
        gtk_window_get_size(_windowHandle, out var width, out var height);

        // Remove maximize and minimize buttons and resize handle from decorations
        gdk_window_set_decorations(gdkWindow,
            GdkWMDecoration.GDK_DECOR_BORDER |
            GdkWMDecoration.GDK_DECOR_TITLE |
            GdkWMDecoration.GDK_DECOR_MENU |
            (_canUserResizeTemp ? GdkWMDecoration.GDK_DECOR_MAXIMIZE : 0));

        // Disable maximize, minimize, and resize functions
        gdk_window_set_functions(gdkWindow,
            GdkWMFunction.GDK_FUNC_MOVE |
            GdkWMFunction.GDK_FUNC_CLOSE |
            (_canUserResizeTemp ? GdkWMFunction.GDK_FUNC_MAXIMIZE : 0));

        // Apply geometry hints to enforce fixed size
        var geom = new GdkGeometry
        {
            min_width = width,
            min_height = height,
            max_width = width,
            max_height = height,
            base_width = width,
            base_height = height
        };
        gtk_window_set_geometry_hints(_windowHandle, IntPtr.Zero, ref geom,
            GdkWindowHints.GDK_HINT_MIN_SIZE | GdkWindowHints.GDK_HINT_MAX_SIZE | GdkWindowHints.GDK_HINT_BASE_SIZE);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int DeleteEvent(IntPtr windowHandle, IntPtr gdkEvent, IntPtr data)
    {
        if (data == IntPtr.Zero || GCHandle.FromIntPtr(data).Target is not GtkNativeWebViewDialog dialog
                                || dialog._disposed)
        {
            return False;
        }

        var cancel = new CancelEventArgs();
        WebViewDispatcher.Invoke(() => dialog.Closing?.Invoke(dialog, cancel));
        return cancel.Cancel ? True : False;
    }


    IntPtr IGtkWebViewPlatformHandle.WebKitWebView => _nativeWebView?.WebViewHandle ?? IntPtr.Zero;
    IntPtr IPlatformHandle.Handle => _windowHandle;
    string? IPlatformHandle.HandleDescriptor => "GtkWindow";
}
