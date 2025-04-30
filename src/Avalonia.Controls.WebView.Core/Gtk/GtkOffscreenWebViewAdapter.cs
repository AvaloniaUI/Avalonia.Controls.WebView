using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Rendering;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using static Avalonia.Controls.Gtk.GtkInterop;
using static Avalonia.Controls.Gtk.AvaloniaGtk;

namespace Avalonia.Controls.Gtk;

/// <param name="useGtkOffscreen">Debug only, useful for testing.</param>
internal unsafe class GtkOffscreenWebViewAdapter(bool useGtkOffscreen = true) : GtkWebViewAdapter,
    IWebViewAdapterWithOffscreenBuffer, IWebViewAdapterWithOffscreenInput
{
    private static readonly IntPtr s_drawCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr*, IntPtr, bool>)&DrawCallback);

    private IntPtr _windowHandle;
    private PixelSize _sizeRequest;
    private GtkSignal? _drawSignal;

    public event Action? DrawRequested;

    public Task UpdateWriteableBitmap(FrameChainBase<WriteableBitmap, PixelSize>.IProducer producer)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return Task.CompletedTask;
        }

        return RunOnGlibThreadAsync(() =>
        {
            if (_windowHandle == IntPtr.Zero)
            {
                return;
            }

            IntPtr pixbuf;
            if (useGtkOffscreen)
            {
                pixbuf = gtk_offscreen_window_get_pixbuf(_windowHandle);
            }
            else
            {
                var gdkWindow = gtk_widget_get_window(Handle);
                int wWidth = gtk_widget_get_allocated_width(_windowHandle);
                int wHeight = gtk_widget_get_allocated_height(_windowHandle);

                pixbuf = gdk_pixbuf_get_from_window(gdkWindow, 0, 0, wWidth, wHeight);
                if (pixbuf != IntPtr.Zero && gdk_pixbuf_get_n_channels(pixbuf) == 3)
                {
                    var pixbufRgba = gdk_pixbuf_add_alpha(pixbuf, false, 0, 0, 0);
                    pixbuf = pixbufRgba;
                }
            }

            if (pixbuf == IntPtr.Zero)
            {
                return;
            }

            try
            {
                var width = gdk_pixbuf_get_width(pixbuf);
                var height = gdk_pixbuf_get_height(pixbuf);
                var stride = gdk_pixbuf_get_rowstride(pixbuf);
                var channels = gdk_pixbuf_get_n_channels(pixbuf);
                var pixelsPtr = gdk_pixbuf_get_pixels(pixbuf);

                var size = new PixelSize(width, height);

                if (channels == 4)
                {
                    using (producer.GetNextFrame(size, out var frame))
                    {
                        using var buf = frame.Lock();
                        var bytesPerRow = Math.Min(stride, buf.RowBytes);
                        var totalBytes = bytesPerRow * height;

                        Buffer.MemoryCopy(
                            source: (void*)pixelsPtr,
                            destination: (void*)buf.Address,
                            destinationSizeInBytes: buf.RowBytes * height,
                            sourceBytesToCopy: totalBytes
                        );
                    }
                }
            }
            finally
            {
                g_object_unref(pixbuf);
            }
        });
    }

    public override void SizeChanged(PixelSize containerSize)
    {
        if (_windowHandle == IntPtr.Zero)
            return;

        _sizeRequest = containerSize;
        RunOnGlibThreadAsync(() =>
        {
            if (useGtkOffscreen)
                gtk_window_set_default_size(_windowHandle, _sizeRequest.Width, _sizeRequest.Height);
            else
                gtk_window_resize(_windowHandle, _sizeRequest.Width, _sizeRequest.Height);
        });
    }

    public bool KeyInput(bool press, PhysicalKey physical, string? _, KeyModifiers modifiers)
    {
        var keycode = KeyTransform.ScanCodeFromPhysicalKey(physical);
        if (keycode == 0)
            return false;

        return RunOnGlibThread(() =>
        {
            var gdisplay = gdk_display_get_default();
            var seat = gdk_display_get_default_seat(gdisplay);
            var gdevice = gdk_seat_get_keyboard(seat);
            var keymap = gdk_keymap_get_for_display(gdisplay);
            var keymapState = gdk_keymap_get_modifier_state(keymap);

            using var state = new EventSendState(press ? GdkEventType.GDK_KEY_PRESS : GdkEventType.GDK_KEY_RELEASE, Handle);
            var ev = state.Event;

            gdk_keymap_translate_keyboard_state(keymap, keycode, keymapState ^ GdkModifierType.ALL_ACCESS_MASK, 0,
                out var keyval, out var effectiveGroup, out var level, out var consumedModifiers);
            gdk_event_set_device(new IntPtr(ev), gdevice);

            ev->key.keyval = keyval;
            ev->key.group = (byte)effectiveGroup;
            ev->key.hardware_keycode = keycode;
            ev->key.state = ToGtk(modifiers, null); 
            ev->key.time = 0;

            return state.Send();
        });
    }

    public bool PointerInput(PointerPoint point, KeyModifiers modifiers)
    {
        var (eventType, button) = point.Properties.PointerUpdateKind switch
        {
            PointerUpdateKind.LeftButtonPressed => (GdkEventType.GDK_BUTTON_PRESS, 1u),
            PointerUpdateKind.MiddleButtonPressed => (GdkEventType.GDK_BUTTON_PRESS, 2u),
            PointerUpdateKind.RightButtonPressed => (GdkEventType.GDK_BUTTON_PRESS, 3u),
            PointerUpdateKind.XButton1Pressed => (GdkEventType.GDK_BUTTON_PRESS, 4u),
            PointerUpdateKind.XButton2Pressed => (GdkEventType.GDK_BUTTON_PRESS, 5u),
            PointerUpdateKind.LeftButtonReleased => (GdkEventType.GDK_BUTTON_RELEASE, 1u),
            PointerUpdateKind.MiddleButtonReleased => (GdkEventType.GDK_BUTTON_RELEASE, 2u),
            PointerUpdateKind.RightButtonReleased => (GdkEventType.GDK_BUTTON_RELEASE, 3u),
            PointerUpdateKind.XButton1Released => (GdkEventType.GDK_BUTTON_RELEASE, 4u),
            PointerUpdateKind.XButton2Released => (GdkEventType.GDK_BUTTON_RELEASE, 5u),
            PointerUpdateKind.Other => (GdkEventType.GDK_MOTION_NOTIFY, 0u),
            _ => (GdkEventType.GDK_NOTHING, 0u)
        };

        if (eventType == GdkEventType.GDK_NOTHING)
        {
            return false;
        }

        return RunOnGlibThread(() =>
        {
            var gdisplay = gdk_display_get_default();
            var seat = gdk_display_get_default_seat(gdisplay);
            var gdevice = gdk_seat_get_pointer(seat);

            using var state = new EventSendState(eventType, Handle);
            var ev = state.Event;

            if (eventType == GdkEventType.GDK_MOTION_NOTIFY)
            {
                ev->motion.time = 0;
                ev->motion.x = point.Position.X;
                ev->motion.y = point.Position.Y;
                ev->motion.state = ToGtk(modifiers, point.Properties);
                ev->motion.device = gdevice;
            }
            else
            {
                ev->button.time = 0;
                ev->button.x = point.Position.X;
                ev->button.y = point.Position.Y;
                ev->button.button = button;
                ev->button.state = ToGtk(modifiers, point.Properties);
                ev->button.device = gdevice;
            }

            return state.Send();
        });
    }

    public bool PointerWheelInput(Vector delta, PointerPoint point, KeyModifiers modifiers)
    {
        return RunOnGlibThread(() =>
        {
            var gdisplay = gdk_display_get_default();
            var seat = gdk_display_get_default_seat(gdisplay);
            var gdevice = gdk_seat_get_pointer(seat);

            var x = point.Position.X;
            var y = point.Position.X;

            using var state = new EventSendState(GdkEventType.GDK_SCROLL, Handle);
            var ev = state.Event;
            ev->scroll.x = x;
            ev->scroll.y = y;
            ev->scroll.time = 0;
            ev->scroll.device = gdevice;
            ev->scroll.state = ToGtk(modifiers, point.Properties);

            ev->scroll.delta_x = delta.X;
            ev->scroll.delta_y = -delta.Y;
            ev->scroll.direction = GdkScrollDirection.GDK_SCROLL_SMOOTH;

            return state.Send();
        });
    }

    private static GdkModifierType ToGtk(KeyModifiers modifiers, PointerPointProperties? pointProperties)
    {
        var output = GdkModifierType.GDK_NO_MODIFIER_MASK;
        if (modifiers.HasFlag(KeyModifiers.Shift))
            output |= GdkModifierType.GDK_SHIFT_MASK;
        if (modifiers.HasFlag(KeyModifiers.Control))
            output |= GdkModifierType.GDK_CONTROL_MASK;
        if (modifiers.HasFlag(KeyModifiers.Alt))
            output |= GdkModifierType.GDK_ALT_MASK;
        if (modifiers.HasFlag(KeyModifiers.Meta))
            output |= GdkModifierType.GDK_META_MASK;
        if (pointProperties?.IsLeftButtonPressed == true)
            output |= GdkModifierType.GDK_BUTTON1_MASK;
        if (pointProperties?.IsMiddleButtonPressed == true)
            output |= GdkModifierType.GDK_BUTTON2_MASK;
        if (pointProperties?.IsRightButtonPressed == true)
            output |= GdkModifierType.GDK_BUTTON3_MASK;
        if (pointProperties?.IsXButton1Pressed == true)
            output |= GdkModifierType.GDK_BUTTON4_MASK;
        if (pointProperties?.IsXButton2Pressed == true)
            output |= GdkModifierType.GDK_BUTTON5_MASK;
        return output;
    }

    protected override void InitializeSafe()
    {
        base.InitializeSafe();

        _windowHandle = useGtkOffscreen ? gtk_offscreen_window_new() : gtk_window_new(0 /* GTK_WINDOW_TOPLEVEL */);
        g_object_ref_sink(_windowHandle);
        gtk_window_set_default_size(_windowHandle, 100, 100);

        gtk_container_add(_windowHandle, Handle);
        gtk_widget_set_has_window(Handle, true);
        gtk_widget_realize(Handle);
        gtk_widget_show_all(_windowHandle);

        _drawSignal = new GtkSignal(Handle, "draw", s_drawCallback, this);
    }

    protected override void DisposeSafe(bool disposing)
    {
        if (disposing)
        {
            Interlocked.Exchange(ref _drawSignal, null)?.Dispose();
        }

        var window = Interlocked.Exchange(ref _windowHandle, IntPtr.Zero);
        if (window != IntPtr.Zero)
        {
            gtk_container_remove(window, Handle);
        }

        // Let nested control to be destroyed first. 
        base.DisposeSafe(disposing);

        if (window != IntPtr.Zero)
        {
            g_object_unref(window);
            gtk_widget_destroy(window);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe bool DrawCallback(IntPtr widget, IntPtr* cairoTex, IntPtr data)
    {
        if (data == IntPtr.Zero || GCHandle.FromIntPtr(data).Target is not GtkOffscreenWebViewAdapter adapter)
        {
            return false;
        }

        Dispatcher.UIThread.InvokeAsync(() => adapter.DrawRequested?.Invoke());
        return false;
    }

    private readonly ref struct EventSendState : IDisposable
    {
        private readonly IntPtr _evPtr;

        public EventSendState(GdkEventType eventType, IntPtr handle)
        {
            _evPtr = gdk_event_new(eventType);
            var ev = (GdkEvent*)_evPtr.ToPointer();
            ev->any.window = gtk_widget_get_window(handle); // gdk window
            ev->any.send_event = 1;
            g_object_ref(ev->any.window);
        }

        public GdkEvent* Event => (GdkEvent*)_evPtr.ToPointer();

        public bool Send()
        {
            gdk_event_put(_evPtr);
            return true;
        }

        public void Dispose()
        {
            if (_evPtr != IntPtr.Zero)
            {
                gdk_event_free(_evPtr);
            }
        }
    }
}
