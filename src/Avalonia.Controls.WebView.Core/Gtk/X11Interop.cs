using System;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Gtk;

public static partial class X11Interop
{
    private const string libX11 = "libX11.so.6";

#if NET7_0_OR_GREATER
    [LibraryImport(libX11)]
    public static partial int XMapWindow(IntPtr display, IntPtr window);
    [LibraryImport(libX11)]
    public static partial int XRaiseWindow(IntPtr display, IntPtr window);
    [LibraryImport(libX11)]
    public static partial int XReparentWindow(IntPtr display, IntPtr window, IntPtr parent, int x, int y);
    [LibraryImport(libX11)]
    public static partial int XFlush(IntPtr display);
    [LibraryImport(libX11)]
    public static partial int XSync(IntPtr display, [MarshalAs(UnmanagedType.Bool)] bool discard);
#else
    [DllImport(libX11)]
    public static extern int XMapWindow(IntPtr display, IntPtr window);

    [DllImport(libX11)]
    public static extern int XRaiseWindow(IntPtr display, IntPtr window);

    [DllImport(libX11)]
    public static extern int XReparentWindow(IntPtr display, IntPtr window, IntPtr parent, int x, int y);

    [DllImport(libX11)]
    public static extern int XFlush(IntPtr display);

    [DllImport(libX11)]
    public static extern int XSync(IntPtr display, [MarshalAs(UnmanagedType.Bool)] bool discard);
#endif
}
