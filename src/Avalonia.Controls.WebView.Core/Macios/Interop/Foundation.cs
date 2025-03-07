using System;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Macios.Interop;

internal static partial class Foundation
{
    private const string Framework = "/System/Library/Frameworks/Foundation.framework/Foundation";

#if NET7_0_OR_GREATER
    [LibraryImport(Framework, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr objc_getClass(string className);
    [LibraryImport(Framework, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr objc_getProtocol(string name);
#else
    [DllImport(Framework, CharSet = CharSet.Ansi)]
    public static extern IntPtr objc_getClass(string className);
    [DllImport(Framework, CharSet = CharSet.Ansi)]
    public static extern IntPtr objc_getProtocol(string name);
#endif
}
