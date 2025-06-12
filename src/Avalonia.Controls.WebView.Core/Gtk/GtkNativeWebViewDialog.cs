using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Threading;
using static Avalonia.Controls.Gtk.GtkInterop;
using static Avalonia.Controls.Gtk.AvaloniaGtk;

namespace Avalonia.Controls.Gtk;

internal sealed class GtkNativeWebViewDialog : INativeWebViewDialog, IGtkWebViewPlatformHandle
{
    private static readonly unsafe IntPtr s_deleteEventCallback = new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, bool>)&DeleteEvent);
    private GtkWebViewAdapter? _nativeWebView;
    private IntPtr _windowHandle;
    private bool _disposed;
    private GtkSignal? _signal;

    private bool _canUserResizeTemp = true;
    private bool _isShown;

    public GtkNativeWebViewDialog(GtkWebViewEnvironmentRequestedEventArgs args)
    {
        _windowHandle = RunOnGlibThread(() =>
        {
            var window = gtk_window_new(0 /* GTK_WINDOW_TOPLEVEL */);
            gtk_window_set_default_size(window, 800, 600);
            return window;
        });

        var nativeWebView = new GtkWebViewAdapter(args);

        RunOnGlibThreadAsync(() =>
        {
            var window = _windowHandle;
            if (window == IntPtr.Zero)
                return;

            _signal = new GtkSignal(window, "delete-event", s_deleteEventCallback, this);
            var scrolled = gtk_scrolled_window_new(IntPtr.Zero, IntPtr.Zero);
            gtk_container_add(scrolled, nativeWebView.WebViewHandle);
            gtk_container_add(window, scrolled);
            _nativeWebView = nativeWebView;
            Dispatcher.UIThread.InvokeAsync(() =>
                AdapterInitialized?.Invoke(this, new WebViewAdapterEventArgs(_nativeWebView)));
        });
    }

    public bool CanUserResize
    {
        get => _isShown ? RunOnGlibThread(() => gtk_window_get_resizable(_windowHandle)) : _canUserResizeTemp;
        set
        {
            if (_isShown)
            {
                RunOnGlibThread(() => gtk_window_set_resizable(_windowHandle, value));
            }
            else
            {
                _canUserResizeTemp = value;
            }
        }
    }

    public event EventHandler? Closing;

    public IWebViewAdapter? TryGetAdapter() => _nativeWebView;

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
        set => RunOnGlibThread(() => gtk_window_set_title(_windowHandle, value ?? string.Empty));
    }

    public void Show() => RunOnGlibThread(() =>
    {
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

        return RunOnGlibThread(() =>
        {
            var xid = owner.Handle;
            var parent = gdk_x11_window_foreign_new_for_display(gdk_display_get_default(), xid);
            gtk_widget_realize(_windowHandle);
            var window = gtk_widget_get_window(_windowHandle);
            if (parent != IntPtr.Zero)
            {
                gdk_window_set_transient_for(window, parent);
            }
            gtk_window_set_position(_windowHandle, 4);
            gtk_widget_show_all(_windowHandle);
            gtk_window_present(_windowHandle);
            if (!_canUserResizeTemp)
            {
                gtk_window_set_resizable(_windowHandle, false);
            }
            _isShown = true;

            return true;
        });
    }

    public void Close()
    {
        Dispose(true);
    }

    public bool Resize(int width, int height)
    {
        RunOnGlibThread(() => gtk_window_resize(_windowHandle, width, height));
        return true;
    }

    public bool Move(int x, int y)
    {
        RunOnGlibThread(() => gtk_window_move(_windowHandle, x, y));
        return true;
    }

    public event EventHandler<WebViewAdapterEventArgs>? AdapterInitialized;
    public event EventHandler<WebViewAdapterEventArgs>? AdapterDestroyed;

    public IPlatformHandle? TryGetPlatformHandle() => this;

    private void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;

            if (Interlocked.Exchange(ref _windowHandle, IntPtr.Zero) is var windowHandle
                && windowHandle != IntPtr.Zero)
            {
                RunOnGlibThread(() => gtk_widget_destroy(windowHandle));
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~GtkNativeWebViewDialog()
    {
        Dispose(false);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static bool DeleteEvent(IntPtr windowHandle, IntPtr gdkEvent, IntPtr data)
    {
        if (data == IntPtr.Zero || GCHandle.FromIntPtr(data).Target is not GtkNativeWebViewDialog dialog)
        {
            return false;
        }

        var cancel = new CancelEventArgs();
        dialog.Closing?.Invoke(dialog, cancel);
        return cancel.Cancel;
    }

    IntPtr IGtkWebViewPlatformHandle.WebKitWebView => _nativeWebView?.WebViewHandle ?? IntPtr.Zero;
    IntPtr IPlatformHandle.Handle => _windowHandle;
    string? IPlatformHandle.HandleDescriptor => "GtkWindow";
}
