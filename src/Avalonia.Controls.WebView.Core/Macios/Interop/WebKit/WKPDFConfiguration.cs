using System;
using System.Runtime.InteropServices;

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
        get
        {
            if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
                return Libobjc.CGRect_objc_msgSend(Handle, s_rect);

            CGRect result;
            unsafe { Libobjc.CGRect_objc_msgSend_stret(&result, Handle, s_rect); }
            return result;
        }
        set => Libobjc.void_objc_msgSend(Handle, s_setRect, value);
    }

    public bool AllowTransparentBackground
    {
        get => Libobjc.byte_objc_msgSend(Handle, s_allowTransparentBackground) != 0;
        set => Libobjc.void_objc_msgSend(Handle, s_setAllowTransparentBackground, value ? 1 : 0);
    }
}
