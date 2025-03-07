using System;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Macios.Interop.WebKit;

internal partial class WebKit
{
    private const string WebKitFramework = "/System/Library/Frameworks/WebKit.framework/WebKit";

#if NET7_0_OR_GREATER
    [LibraryImport(WebKitFramework, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr objc_getClass(string className);
    [LibraryImport(WebKitFramework, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr objc_getProtocol(string name);
#else
    [DllImport(WebKitFramework, CharSet = CharSet.Ansi)]
    public static extern IntPtr objc_getClass(string className);
    [DllImport(WebKitFramework, CharSet = CharSet.Ansi)]
    public static extern IntPtr objc_getProtocol(string name);
#endif
}
