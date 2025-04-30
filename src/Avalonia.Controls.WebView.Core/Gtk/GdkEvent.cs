// Based on
// https://github.com/reignstudios/Orbital-Framework/blob/ad747081ccc2e993325cf9dfad37a0acd916cde7/Platforms/Lin/Shared/Orbital.Host.GTK3/GTK3.cs#L62

using System;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace Avalonia.Controls.Gtk;

internal enum GdkEventType
{
    GDK_NOTHING		= -1,

    GDK_MOTION_NOTIFY = 3,
    GDK_BUTTON_PRESS = 4,
    GDK_2BUTTON_PRESS = 5,
    GDK_DOUBLE_BUTTON_PRESS = GDK_2BUTTON_PRESS,
    GDK_3BUTTON_PRESS = 6,
    GDK_TRIPLE_BUTTON_PRESS = GDK_3BUTTON_PRESS,
    GDK_BUTTON_RELEASE = 7,
    GDK_KEY_PRESS = 8,
    GDK_KEY_RELEASE = 9,
    GDK_ENTER_NOTIFY = 10,
    GDK_LEAVE_NOTIFY = 11,
    GDK_SCROLL            = 31,
}

[StructLayout(LayoutKind.Explicit)]
internal struct GdkEvent
{
    [FieldOffset(0)] public GdkEventType Type;
    [FieldOffset(0)] public GdkEventAny any;

    [FieldOffset(0)] public GdkEventMotion motion;
    [FieldOffset(0)] public GdkEventButton button;
    [FieldOffset(0)] public GdkEventScroll scroll;
    [FieldOffset(0)] public GdkEventKey key;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GdkEventAny
{
    public GdkEventType type;
    public IntPtr window;
    public SByte send_event;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct GdkEventButton
{
    public GdkEventType type;
    public IntPtr window;
    public SByte send_event;
    public UInt32 time;
    public Double x;
    public Double y;
    public Double *axes;
    public GdkModifierType state;
    public UInt32 button;
    public IntPtr device;
    public Double x_root, y_root;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct GdkEventMotion
{
    public GdkEventType type;
    public IntPtr window;
    public SByte send_event;
    public UInt32 time;
    public Double x;
    public Double y;
    public Double *axes;
    public GdkModifierType state;
    public Int16 is_hint;
    public IntPtr device;
    public Double x_root, y_root;
}

[StructLayout(LayoutKind.Sequential)]
internal struct GdkEventScroll
{
    public GdkEventType type;
    public IntPtr window;
    public SByte send_event;
    public UInt32 time;
    public Double x;
    public Double y;
    public GdkModifierType state;
    public GdkScrollDirection direction;
    public IntPtr device;
    public Double x_root, y_root;
    public Double delta_x;
    public Double delta_y;
    public bool is_stop;//public guint is_stop : 1;
}

[Flags]
internal enum GdkModifierType : uint
{
    GDK_NO_MODIFIER_MASK = 0,
    GDK_SHIFT_MASK = 1, // SHIFT
    GDK_LOCK_MASK = 2, // CAPS LOCK
    GDK_CONTROL_MASK = 4, // CTRL
    GDK_ALT_MASK = 8, // ALT
    GDK_BUTTON1_MASK = 256,
    GDK_BUTTON2_MASK = 512,
    GDK_BUTTON3_MASK = 1024,
    GDK_BUTTON4_MASK = 2048,
    GDK_BUTTON5_MASK = 4096,
    GDK_META_MASK = 268435456, // WINDOWS

    ALL_ACCESS_MASK = GDK_CONTROL_MASK | GDK_SHIFT_MASK | GDK_ALT_MASK | GDK_LOCK_MASK
}

public enum GdkScrollDirection
{
    GDK_SCROLL_UP,
    GDK_SCROLL_DOWN,
    GDK_SCROLL_LEFT,
    GDK_SCROLL_RIGHT,
    GDK_SCROLL_SMOOTH
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct GdkEventKey
{
    public GdkEventType type;
    public IntPtr window;
    public SByte send_event;
    public UInt32 time;
    public GdkModifierType state;
    public UInt32 keyval;
    public Int32 length;
    public Byte *_string;
    public UInt16 hardware_keycode;
    public Byte group;
    public bool is_modifier;//public guint is_modifier : 1;
}
