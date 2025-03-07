using System;

namespace Avalonia.Controls.Macios.Interop.WebKit;

internal class WKContentWorld
{
    private static readonly IntPtr s_class = WebKit.objc_getClass("WKContentWorld");

    public static IntPtr DefaultClientWorld { get; } =
        Libobjc.intptr_objc_msgSend(s_class, Libobjc.sel_getUid("defaultClientWorld"));
}
