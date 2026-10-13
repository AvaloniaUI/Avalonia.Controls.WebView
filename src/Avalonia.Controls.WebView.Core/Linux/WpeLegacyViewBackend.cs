using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls.Linux.Interop;
using Avalonia.Controls.Rendering;
using Avalonia.Input;
using Avalonia.Media.Imaging;

namespace Avalonia.Controls.Linux;

internal sealed unsafe class WpeLegacyViewBackend : IWpeViewBackend
{
    private static readonly Lazy<IntPtr> s_fdoDestroyFn = new(() =>
        NativeLibrary.GetExport(
            NativeLibrary.Load("libWPEBackend-fdo-1.0.so.1"),
            "wpe_view_backend_exportable_fdo_destroy"));

    private static bool s_backendLibraryConfigured;

    private IntPtr _exportable;
    private IntPtr _viewBackend;
    private IntPtr _wkBackend;
    private bool _exportableOwnedByWebKit;
    private IntPtr _pendingShmExportedBuffer;
    private GCHandle _shmClientHandle;
    private GCHandle _selfHandle;

    public WpeLegacyViewBackend(PixelSize initialSize)
    {
        EnsureBackendLibrary();

        if (!WpeInterop.wpe_fdo_initialize_shm())
            throw new InvalidOperationException("wpe_fdo_initialize_shm() failed.");

        _selfHandle = GCHandle.Alloc(this);

        var shmClientArray = new WpeViewBackendExportableFdoClient[1];
        shmClientArray[0] = new WpeViewBackendExportableFdoClient
        {
            ExportBufferResource = IntPtr.Zero,
            ExportDmabufResource = IntPtr.Zero,
            ExportShmBuffer = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)&OnExportShmBuffer,
            Reserved0 = IntPtr.Zero,
            Reserved1 = IntPtr.Zero
        };
        _shmClientHandle = GCHandle.Alloc(shmClientArray, GCHandleType.Pinned);
        var shmClientPtr = (WpeViewBackendExportableFdoClient*)_shmClientHandle.AddrOfPinnedObject();

        _exportable = WpeInterop.wpe_view_backend_exportable_fdo_create(
            shmClientPtr, GCHandle.ToIntPtr(_selfHandle), (uint)initialSize.Width, (uint)initialSize.Height);
        if (_exportable == IntPtr.Zero)
            throw new InvalidOperationException("wpe_view_backend_exportable_fdo_create failed for SHM path.");

        _viewBackend = WpeInterop.wpe_view_backend_exportable_fdo_get_view_backend(_exportable);
        if (_viewBackend == IntPtr.Zero)
            throw new InvalidOperationException("wpe_view_backend_exportable_fdo_get_view_backend failed.");

        _wkBackend = WpeInterop.webkit_web_view_backend_new(_viewBackend, s_fdoDestroyFn.Value, _exportable);
        _exportableOwnedByWebKit = true;
        if (_wkBackend == IntPtr.Zero)
            throw new InvalidOperationException("webkit_web_view_backend_new failed.");
    }

    public static void EnsureBackendLibrary()
    {
        if (s_backendLibraryConfigured) return;
        s_backendLibraryConfigured = true;

        // libwpe lazily loads "libWPEBackend-default.so" the first time any WPE
        // library function is called. On most Linux distros only libWPEBackend-fdo
        // is available. We must call wpe_loader_init() with the correct library
        // name BEFORE any other WPE library is loaded or used.
        //
        // wpe_loader_init is in libwpe-1.0 which has no dependency on the backend,
        // so calling it via the separate WpeLoader P/Invoke class is safe.
        string[] candidates =
        [
            "libWPEBackend-fdo-1.0.so.1",
            "libWPEBackend-fdo-1.0.so"
        ];
        string[] searchPaths =
        [
            "/usr/lib",
            "/usr/lib64",
            "/usr/local/lib",
            "/usr/lib/x86_64-linux-gnu",
            "/usr/lib/aarch64-linux-gnu"
        ];

        foreach (var candidate in candidates)
        {
            foreach (var dir in searchPaths)
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(dir, candidate)))
                {
                    WpeLoader.wpe_loader_init(candidate);
                    return;
                }
            }
        }

        // Last resort: try the soname and hope the dynamic linker can find it
        WpeLoader.wpe_loader_init("libWPEBackend-fdo-1.0.so.1");
    }

    public string WebViewPropertyName => "backend";
    public GValue WebViewPropertyValue => new(WpeInterop.webkit_web_view_backend_get_type(), _wkBackend);

    public IntPtr WpeViewBackend => _viewBackend;
    public IntPtr WpeView => IntPtr.Zero;

    public Func<bool>? FrameReady { get; set; }

    public void Attach(IntPtr webView, float scale)
    {
        WpeInterop.wpe_view_backend_add_activity_state(_viewBackend,
            WpeViewActivityState.Visible | WpeViewActivityState.Focused | WpeViewActivityState.InWindow);
        WpeInterop.wpe_view_backend_dispatch_set_device_scale_factor(_viewBackend, scale);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnExportShmBuffer(IntPtr data, IntPtr shmExportedBuffer)
    {
        var backend = (WpeLegacyViewBackend)GCHandle.FromIntPtr(data).Target!;

        var prev = backend._pendingShmExportedBuffer;
        if (prev != IntPtr.Zero && prev != shmExportedBuffer)
            backend.ReleaseShmBuffer(prev);

        backend._pendingShmExportedBuffer = shmExportedBuffer;
        if (backend.FrameReady?.Invoke() != true)
            backend.ReleaseShmBuffer(shmExportedBuffer);
    }

    private void ReleaseShmBuffer(IntPtr shmExportedBuffer)
    {
        if (_exportable == IntPtr.Zero || shmExportedBuffer == IntPtr.Zero) return;
        WpeInterop.wpe_view_backend_exportable_fdo_dispatch_release_shm_exported_buffer(_exportable, shmExportedBuffer);
        WpeInterop.wpe_view_backend_exportable_fdo_dispatch_frame_complete(_exportable);
        if (_pendingShmExportedBuffer == shmExportedBuffer)
            _pendingShmExportedBuffer = IntPtr.Zero;
    }

    public void ReleaseFrames()
    {
        // Release any pending SHM buffer back to WPE before destroying the
        // exportable, otherwise the wl_shm proxy stays attached to the Wayland
        // display queue and Mesa warns on teardown.
        if (_pendingShmExportedBuffer != IntPtr.Zero)
        {
            ReleaseShmBuffer(_pendingShmExportedBuffer);
            _pendingShmExportedBuffer = IntPtr.Zero;
        }
    }

    public void CopyFrame(FrameChainBase<WriteableBitmap, PixelSize>.IProducer producer)
    {
        var shmBufPtr = _pendingShmExportedBuffer;
        if (shmBufPtr == IntPtr.Zero)
            return;

        // Read the exported buffer struct to get the wl_shm_buffer pointer
        var exported = *(WpeFdoShmExportedBuffer*)shmBufPtr;
        var shmBuffer = exported.ShmBuffer;
        if (shmBuffer == IntPtr.Zero)
        {
            ReleaseShmBuffer(shmBufPtr);
            return;
        }

        var width = WpeInterop.wl_shm_buffer_get_width(shmBuffer);
        var height = WpeInterop.wl_shm_buffer_get_height(shmBuffer);
        var stride = WpeInterop.wl_shm_buffer_get_stride(shmBuffer);

        if (width <= 0 || height <= 0)
        {
            ReleaseShmBuffer(shmBufPtr);
            return;
        }

        WpeInterop.wl_shm_buffer_begin_access(shmBuffer);
        try
        {
            var dataPtr = WpeInterop.wl_shm_buffer_get_data(shmBuffer);
            var bitmapSize = new PixelSize(width, height);

            using (producer.GetNextFrame(bitmapSize, out var frame))
            {
                using var buf = frame.Lock();
                var dstStride = buf.RowBytes;

                if (stride == dstStride)
                {
                    // Strides match — single memcpy
                    Buffer.MemoryCopy((void*)dataPtr, (void*)buf.Address, (long)height * dstStride, (long)height * stride);
                }
                else
                {
                    // Row-by-row copy when strides differ
                    var copyBytes = Math.Min(stride, dstStride);
                    for (int y = 0; y < height; y++)
                    {
                        Buffer.MemoryCopy(
                            (byte*)dataPtr + (long)y * stride,
                            (byte*)buf.Address + (long)y * dstStride,
                            dstStride, copyBytes);
                    }
                }
            }
        }
        finally
        {
            WpeInterop.wl_shm_buffer_end_access(shmBuffer);
        }

        ReleaseShmBuffer(shmBufPtr);
    }

    public void SetSize(uint logicalWidth, uint logicalHeight) =>
        WpeInterop.wpe_view_backend_dispatch_set_size(_viewBackend, logicalWidth, logicalHeight);

    public void SetScale(float scale) =>
        WpeInterop.wpe_view_backend_dispatch_set_device_scale_factor(_viewBackend, scale);

    public void SetVisible(bool visible)
    {
        if (visible)
        {
            WpeInterop.wpe_view_backend_add_activity_state(_viewBackend,
                WpeViewActivityState.Visible | WpeViewActivityState.InWindow);
        }
        else
        {
            WpeInterop.wpe_view_backend_remove_activity_state(_viewBackend,
                WpeViewActivityState.Visible);
        }
    }

    public uint UnicodeToKeysym(uint codepoint) => WpeInterop.wpe_unicode_to_key_code(codepoint);

    public void KeyInput(bool press, uint keysym, uint hardwareKeycode, uint modifiers)
    {
        var evt = new WpeInputKeyboardEvent
        {
            Time = (uint)Environment.TickCount,
            KeyCode = keysym,
            HardwareKeyCode = hardwareKeycode,
            Pressed = press ? 1 : 0,
            Modifiers = modifiers
        };

        WpeInterop.wpe_view_backend_dispatch_keyboard_event(_viewBackend, &evt);
    }

    public void PointerInput(PointerPoint point, int clickCount, double scale, uint modifiers)
    {
        // scale is the render scaling factor (e.g. 1.0, 1.5, 2.0).
        // Multiply logical coordinates directly by this factor.
        var x = (int)(point.Position.X * scale);
        var y = (int)(point.Position.Y * scale);

        // Use PointerUpdateKind to distinguish press, release, and move.
        // Checking IsLeftButtonPressed etc. doesn't work because on release
        // those flags are already false, so WPE would never see the button up.
        var (type, button, state) = point.Properties.PointerUpdateKind switch
        {
            PointerUpdateKind.LeftButtonPressed => (WpeInputPointerEventType.Button, 1u, 1u),
            PointerUpdateKind.LeftButtonReleased => (WpeInputPointerEventType.Button, 1u, 0u),
            PointerUpdateKind.MiddleButtonPressed => (WpeInputPointerEventType.Button, 2u, 1u),
            PointerUpdateKind.MiddleButtonReleased => (WpeInputPointerEventType.Button, 2u, 0u),
            PointerUpdateKind.RightButtonPressed => (WpeInputPointerEventType.Button, 3u, 1u),
            PointerUpdateKind.RightButtonReleased => (WpeInputPointerEventType.Button, 3u, 0u),
            _ => (WpeInputPointerEventType.Motion, 0u, 0u)
        };

        var evt = new WpeInputPointerEvent
        {
            Type = type,
            Time = (uint)Environment.TickCount,
            X = x,
            Y = y,
            Button = button,
            State = state,
            Modifiers = modifiers
        };

        WpeInterop.wpe_view_backend_dispatch_pointer_event(_viewBackend, &evt);
    }

    public bool PointerLeaveInput(PointerPoint point, double scale, uint modifiers)
    {
        // WPE doesn't have explicit pointer leave — just track motion out
        return false;
    }

    public void PointerWheelInput(Vector delta, PointerPoint point, double scale, uint modifiers)
    {
        var x = (int)(point.Position.X * scale);
        var y = (int)(point.Position.Y * scale);

        // Use 2D axis event for smooth scrolling
        var baseEvt = new WpeInputAxisEvent
        {
            Type = WpeInputAxisEventType.MotionSmooth | WpeInputAxisEventType.Mask2D,
            Time = (uint)Environment.TickCount,
            X = x,
            Y = y,
            Axis = 0,
            Value = 0,
            Modifiers = modifiers
        };

        var evt = new WpeInputAxis2DEvent
        {
            Base = baseEvt,
            // Avalonia delta is in "lines", WPE expects pixels; approximate 1 line = ~40px
            XAxis = delta.X * 40.0,
            YAxis = delta.Y * 40.0
        };

        WpeInterop.wpe_view_backend_dispatch_axis_event(_viewBackend, &evt.Base);
    }

    public void Dispose()
    {
        if (!_exportableOwnedByWebKit && _exportable != IntPtr.Zero)
            WpeInterop.wpe_view_backend_exportable_fdo_destroy(_exportable);
        _exportable = IntPtr.Zero;
        _viewBackend = IntPtr.Zero;
        _wkBackend = IntPtr.Zero;

        if (_shmClientHandle.IsAllocated)
            _shmClientHandle.Free();

        if (_selfHandle.IsAllocated)
            _selfHandle.Free();
    }
}
