using System;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Macios.Interop;

internal static class CoreFoundation
{
    private const string Framework = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    [DllImport(Framework)]
    public static extern IntPtr CFBridgingRelease(IntPtr ptr);
    [DllImport(Framework)]
    public static extern IntPtr CFBridgingRetain(IntPtr ptr);
}
