using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using IPlatformHandle = Avalonia.Platform.IPlatformHandle;
using PlatformHandle = Avalonia.Platform.PlatformHandle;
using static AvaloniaUI.WebView.Gtk.GtkInterop;
using static AvaloniaUI.WebView.Gtk.AvaloniaGtk;

namespace AvaloniaUI.WebView.Gtk;

internal sealed class GtkNativeWebViewDialog : INativeWebViewDialog
{
    private static readonly unsafe IntPtr s_closeRequestCallback = new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, bool>)&WindowCloseRequest);
    private readonly GtkWebViewAdapter _nativeWebView;
    private IntPtr _windowHandle;
    private bool _disposed;
    private GtkSignal? _signal;

    public GtkNativeWebViewDialog()
    {
        _windowHandle = RunOnGlibThread(static () =>
        {
            var window = gtk_window_new(0 /* GTK_WINDOW_TOPLEVEL */);
            gtk_window_set_default_size(window, 800, 600);
            return window;
        });

        _nativeWebView = new GtkWebViewAdapter();

        _ = RunOnGlibThread(() =>
        {
            _signal = new GtkSignal(_windowHandle, "close", s_closeRequestCallback, this);
            var scrolled = gtk_scrolled_window_new(IntPtr.Zero, IntPtr.Zero);
            gtk_container_add(scrolled, _nativeWebView.Handle);
            gtk_container_add(_windowHandle, scrolled);
            return 0;
        });
    }

    public IWebView WebView => _nativeWebView;
    public event EventHandler? Closing;

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
            gtk_widget_show_all(_windowHandle);
            gtk_window_present(_windowHandle);

            return true;
        });
    }

    public void Close()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            RunOnGlibThread(() => gtk_widget_destroy(_windowHandle));
            _windowHandle = IntPtr.Zero;
        }

        _nativeWebView.Dispose();
    }

    public IPlatformHandle? TryGetPlatformHandle() => new PlatformHandle(_windowHandle, "GtkWindow");

    private void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _signal?.Dispose();
            Close();
            _nativeWebView.Dispose();
            _disposed = true;
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
    private static bool WindowCloseRequest(IntPtr windowHandle, IntPtr data)
    {
        if (data == IntPtr.Zero || GCHandle.FromIntPtr(data).Target is not GtkNativeWebViewDialog dialog)
        {
            return false;
        }

        var cancel = new CancelEventArgs();
        dialog.Closing?.Invoke(dialog, cancel);
        return cancel.Cancel;
    }
}
