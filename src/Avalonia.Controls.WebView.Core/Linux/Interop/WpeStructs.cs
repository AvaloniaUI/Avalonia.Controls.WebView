using System;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Linux.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct WpeInputKeyboardEvent
{
    public uint Time;
    public uint KeyCode;
    public uint HardwareKeyCode;
    public int Pressed; // bool in C is int-sized
    public uint Modifiers;
}

internal enum WpeInputPointerEventType
{
    Null = 0,
    Motion = 1,
    Button = 2
}

[StructLayout(LayoutKind.Sequential)]
internal struct WpeInputPointerEvent
{
    public WpeInputPointerEventType Type;
    public uint Time;
    public int X;
    public int Y;
    public uint Button;
    public uint State;
    public uint Modifiers;
}

internal enum WpeInputAxisEventType
{
    Null = 0,
    Motion = 1,
    MotionSmooth = 2,
    Mask2D = 1 << 16
}

[StructLayout(LayoutKind.Sequential)]
internal struct WpeInputAxisEvent
{
    public WpeInputAxisEventType Type;
    public uint Time;
    public int X;
    public int Y;
    public uint Axis;
    public int Value;
    public uint Modifiers;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WpeInputAxis2DEvent
{
    public WpeInputAxisEvent Base;
    public double XAxis;
    public double YAxis;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WebKitColor
{
    public double Red;
    public double Green;
    public double Blue;
    public double Alpha;
}

internal static class WpeInputModifier
{
    public const uint Control = 1 << 0;
    public const uint Shift = 1 << 1;
    public const uint Alt = 1 << 2;
    public const uint Meta = 1 << 3;
}

internal static class WpeViewActivityState
{
    public const uint Visible = 1 << 0;
    public const uint Focused = 1 << 1;
    public const uint InWindow = 1 << 2;
}

/// <summary>
/// Matches GLib's GPollFD and POSIX struct pollfd on Linux.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct GPollFD
{
    public int Fd;
    public ushort Events;
    public ushort Revents;
}

/// <summary>
/// SHM buffer exported by WPEBackend-fdo via the generic (non-EGL) export path.
/// Maps to struct wpe_fdo_shm_exported_buffer.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WpeFdoShmExportedBuffer
{
    public IntPtr Resource;    // struct wl_resource*
    public IntPtr ShmBuffer;   // struct wl_shm_buffer*
}

/// <summary>
/// Generic FDO export client callbacks (supports SHM export).
/// Maps to struct wpe_view_backend_exportable_fdo_client.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct WpeViewBackendExportableFdoClient
{
    public IntPtr ExportBufferResource;   // void (*)(void*, struct wl_resource*)
    public IntPtr ExportDmabufResource;   // void (*)(void*, struct wpe_view_backend_exportable_fdo_dmabuf_resource*)
    public IntPtr ExportShmBuffer;        // void (*)(void*, struct wpe_fdo_shm_exported_buffer*)
    public IntPtr Reserved0;
    public IntPtr Reserved1;
}
