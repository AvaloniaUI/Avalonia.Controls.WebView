using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls.Linux.Interop;
using Avalonia.Controls.Rendering;
using Avalonia.Input;
using Avalonia.Media.Imaging;

namespace Avalonia.Controls.Linux;

internal sealed unsafe class WpePlatformViewBackend : IWpeViewBackend
{
    private readonly IntPtr _display;
    private readonly PixelSize _initialSize;
    private IntPtr _view;
    private IntPtr _toplevel;
    private ulong _bufferRenderedHandler;
    private IntPtr _pendingBuffer;
    private GCHandle _selfHandle;
    private bool _pointerInside;
    private Point _lastPointerPosition;

    public WpePlatformViewBackend(IntPtr display, PixelSize initialSize)
    {
        _display = display;
        _initialSize = initialSize;
    }

    internal static IntPtr NewWithDisplay(IntPtr type, IntPtr display) =>
        WpeInterop.g_object_new_with_properties(type, 1, ["display"],
            [new GValue(WpePlatformInterop.wpe_display_get_type(), display)]);

    public string WebViewPropertyName => "display";
    public GValue WebViewPropertyValue => new(WpePlatformInterop.wpe_display_get_type(), _display);

    public IntPtr WpeViewBackend => IntPtr.Zero;
    public IntPtr WpeView => _view;

    public Func<bool>? FrameReady { get; set; }

    public void Attach(IntPtr webView, float scale)
    {
        _view = WpePlatformInterop.webkit_web_view_get_wpe_view(webView);
        if (_view == IntPtr.Zero)
            throw new InvalidOperationException("webkit_web_view_get_wpe_view failed.");

        _toplevel = WpePlatformInterop.wpe_view_get_toplevel(_view);
        if (_toplevel == IntPtr.Zero)
        {
            var toplevel = NewWithDisplay(WpePlatformInterop.wpe_toplevel_headless_get_type(), _display);
            if (toplevel == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create the WPE headless toplevel.");
            WpePlatformInterop.wpe_view_set_toplevel(_view, toplevel);
            WpeInterop.g_object_unref(toplevel);
            _toplevel = WpePlatformInterop.wpe_view_get_toplevel(_view);
        }

        _selfHandle = GCHandle.Alloc(this);
        _bufferRenderedHandler = WpeInterop.g_signal_connect_data(_view, "buffer-rendered",
            (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)&OnBufferRendered,
            GCHandle.ToIntPtr(_selfHandle), IntPtr.Zero, 0);

        WpePlatformInterop.wpe_toplevel_scale_changed(_toplevel, scale);
        WpePlatformInterop.wpe_toplevel_resize(_toplevel, _initialSize.Width, _initialSize.Height);
        WpePlatformInterop.wpe_view_set_visible(_view, true);
        WpePlatformInterop.wpe_view_focus_in(_view);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnBufferRendered(IntPtr view, IntPtr buffer, IntPtr userData)
    {
        var backend = (WpePlatformViewBackend)GCHandle.FromIntPtr(userData).Target!;
        if (backend.FrameReady is null || buffer == IntPtr.Zero)
            return;

        var previous = backend._pendingBuffer;
        backend._pendingBuffer = WpeInterop.g_object_ref(buffer);
        if (previous != IntPtr.Zero)
            WpeInterop.g_object_unref(previous);

        if (!backend.FrameReady())
            backend.ReleasePendingBuffer();
    }

    private void ReleasePendingBuffer()
    {
        if (_pendingBuffer == IntPtr.Zero)
            return;
        WpeInterop.g_object_unref(_pendingBuffer);
        _pendingBuffer = IntPtr.Zero;
    }

    public void ReleaseFrames()
    {
        if (_bufferRenderedHandler != 0)
        {
            WpePlatformInterop.g_signal_handler_disconnect(_view, _bufferRenderedHandler);
            _bufferRenderedHandler = 0;
        }

        ReleasePendingBuffer();
    }

    public void CopyFrame(FrameChainBase<WriteableBitmap, PixelSize>.IProducer producer)
    {
        var buffer = _pendingBuffer;
        if (buffer == IntPtr.Zero)
            return;
        _pendingBuffer = IntPtr.Zero;

        try
        {
            IntPtr error = IntPtr.Zero;
            var bytes = WpePlatformInterop.wpe_buffer_import_to_pixels(buffer, &error);
            if (bytes == IntPtr.Zero)
            {
                WpeRuntime.TakeErrorMessage(error);
                return;
            }

            nuint size;
            var data = (byte*)WpePlatformInterop.g_bytes_get_data(bytes, &size);
            var width = WpePlatformInterop.wpe_buffer_get_width(buffer);
            var height = WpePlatformInterop.wpe_buffer_get_height(buffer);
            if (data == null || width <= 0 || height <= 0)
                return;

            var stride = (int)(size / (nuint)height);
            if (stride < width * 4)
                return;

            var forceOpaque = WpePlatformInterop.g_type_check_instance_is_a(buffer, WpePlatformInterop.wpe_buffer_dma_buf_get_type())
                              && WpePlatformInterop.wpe_buffer_dma_buf_get_format(buffer) == DrmFourcc.Xrgb8888;

            using (producer.GetNextFrame(new PixelSize(width, height), out var frame))
            {
                using var locked = frame.Lock();
                var dstStride = locked.RowBytes;
                var copyBytes = Math.Min(width * 4, dstStride);

                for (var y = 0; y < height; y++)
                {
                    var dst = (byte*)locked.Address + (long)y * dstStride;
                    Buffer.MemoryCopy(data + (long)y * stride, dst, dstStride, copyBytes);

                    if (forceOpaque)
                    {
                        var pixels = (uint*)dst;
                        for (var x = 0; x < copyBytes / 4; x++)
                            pixels[x] |= 0xFF000000;
                    }
                }
            }
        }
        finally
        {
            WpeInterop.g_object_unref(buffer);
        }
    }

    public void SetSize(uint logicalWidth, uint logicalHeight) =>
        WpePlatformInterop.wpe_toplevel_resize(_toplevel, (int)logicalWidth, (int)logicalHeight);

    public void SetScale(float scale) =>
        WpePlatformInterop.wpe_toplevel_scale_changed(_toplevel, scale);

    public void SetVisible(bool visible) =>
        WpePlatformInterop.wpe_view_set_visible(_view, visible);

    public uint UnicodeToKeysym(uint codepoint) => WpePlatformInterop.wpe_unicode_to_keyval(codepoint);

    public void KeyInput(bool press, uint keysym, uint hardwareKeycode, uint modifiers)
    {
        var evt = WpePlatformInterop.wpe_event_keyboard_new(
            press ? WpeEventType.KeyboardKeyDown : WpeEventType.KeyboardKeyUp,
            _view, WpeInputSource.Keyboard, (uint)Environment.TickCount, modifiers,
            hardwareKeycode, keysym);
        DispatchEvent(evt);
    }

    public void PointerInput(PointerPoint point, int clickCount, double scale, uint modifiers)
    {
        var position = point.Position;
        var time = (uint)Environment.TickCount;
        modifiers |= PointerButtonModifiers(point.Properties);

        if (!_pointerInside)
        {
            _pointerInside = true;
            _lastPointerPosition = position;
            DispatchEvent(WpePlatformInterop.wpe_event_pointer_move_new(
                WpeEventType.PointerEnter, _view, WpeInputSource.Mouse, time, modifiers,
                position.X, position.Y, 0, 0));
        }

        var (type, button) = point.Properties.PointerUpdateKind switch
        {
            PointerUpdateKind.LeftButtonPressed => (WpeEventType.PointerDown, 1u),
            PointerUpdateKind.LeftButtonReleased => (WpeEventType.PointerUp, 1u),
            PointerUpdateKind.MiddleButtonPressed => (WpeEventType.PointerDown, 2u),
            PointerUpdateKind.MiddleButtonReleased => (WpeEventType.PointerUp, 2u),
            PointerUpdateKind.RightButtonPressed => (WpeEventType.PointerDown, 3u),
            PointerUpdateKind.RightButtonReleased => (WpeEventType.PointerUp, 3u),
            _ => (WpeEventType.PointerMove, 0u)
        };

        IntPtr evt;
        if (type == WpeEventType.PointerMove)
        {
            var delta = position - _lastPointerPosition;
            evt = WpePlatformInterop.wpe_event_pointer_move_new(
                type, _view, WpeInputSource.Mouse, time, modifiers,
                position.X, position.Y, delta.X, delta.Y);
        }
        else
        {
            var pressCount = type == WpeEventType.PointerDown ? (uint)Math.Max(clickCount, 1) : 0u;
            evt = WpePlatformInterop.wpe_event_pointer_button_new(
                type, _view, WpeInputSource.Mouse, time, modifiers,
                button, position.X, position.Y, pressCount);
        }

        _lastPointerPosition = position;
        DispatchEvent(evt);
    }

    public bool PointerLeaveInput(PointerPoint point, double scale, uint modifiers)
    {
        if (!_pointerInside)
            return true;

        _pointerInside = false;
        DispatchEvent(WpePlatformInterop.wpe_event_pointer_move_new(
            WpeEventType.PointerLeave, _view, WpeInputSource.Mouse, (uint)Environment.TickCount,
            modifiers | PointerButtonModifiers(point.Properties), -1, -1, 0, 0));
        return true;
    }

    public void PointerWheelInput(Vector delta, PointerPoint point, double scale, uint modifiers)
    {
        var evt = WpePlatformInterop.wpe_event_scroll_new(
            _view, WpeInputSource.Mouse, (uint)Environment.TickCount,
            modifiers | PointerButtonModifiers(point.Properties),
            delta.X, delta.Y, false, false, point.Position.X, point.Position.Y);
        DispatchEvent(evt);
    }

    private void DispatchEvent(IntPtr evt)
    {
        if (evt == IntPtr.Zero)
            return;
        WpePlatformInterop.wpe_view_event(_view, evt);
        WpePlatformInterop.wpe_event_unref(evt);
    }

    private static uint PointerButtonModifiers(PointerPointProperties properties)
    {
        uint result = 0;
        if (properties.IsLeftButtonPressed) result |= WpePlatformModifier.PointerButton1;
        if (properties.IsMiddleButtonPressed) result |= WpePlatformModifier.PointerButton2;
        if (properties.IsRightButtonPressed) result |= WpePlatformModifier.PointerButton3;
        return result;
    }

    public void Dispose()
    {
        ReleasePendingBuffer();
        _bufferRenderedHandler = 0;
        _view = IntPtr.Zero;
        _toplevel = IntPtr.Zero;

        if (_selfHandle.IsAllocated)
            _selfHandle.Free();
    }
}
