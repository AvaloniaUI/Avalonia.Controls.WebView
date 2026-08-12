using System;

namespace Avalonia.Controls.Macios.Interop.WebKit;

internal class WKPDFConfiguration : NSObject
{
    private static readonly IntPtr s_class = Libobjc.objc_getClass("WKPDFConfiguration");
    private static readonly IntPtr s_rect = Libobjc.sel_getUid("rect");
    private static readonly IntPtr s_setRect = Libobjc.sel_getUid("setRect:");
    private static readonly IntPtr s_allowTransparentBackground = Libobjc.sel_getUid("allowsTransparentBackground");
    private static readonly IntPtr s_setAllowTransparentBackground = Libobjc.sel_getUid("setAllowsTransparentBackground:");

    public WKPDFConfiguration() : base(s_class)
    {
        Init();
    }

    public CGRect Rect
    {
        get => Libobjc.CGRect_objc_msgSend(Handle, s_rect);
        set => Libobjc.void_objc_msgSend(Handle, s_setRect, value);
    }

    public bool AllowTransparentBackground
    {
        get => Libobjc.byte_objc_msgSend(Handle, s_allowTransparentBackground) != 0;
        set => Libobjc.void_objc_msgSend(Handle, s_setAllowTransparentBackground, value ? 1 : 0);
    }
}
