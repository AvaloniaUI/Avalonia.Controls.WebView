using System;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Macios.Interop.WebKit;

internal partial class WebKit
{
    private const string WebKitFramework = "/System/Library/Frameworks/WebKit.framework/WebKit";

    private static bool? s_isLoaded;

    public static bool PreloadWebKit()
        => s_isLoaded ??= objc_getClass("WKWebView") != IntPtr.Zero;

    [LibraryImport(WebKitFramework, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr objc_getClass(string className);
}
