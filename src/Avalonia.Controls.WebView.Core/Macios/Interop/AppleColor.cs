using System;

namespace Avalonia.Controls.Macios.Interop;

// NSColor or UIColor
internal class AppleColor : NSObject
{
    private static readonly IntPtr s_class = OperatingSystem.IsMacOS() ?
        Libobjc.objc_getClass("NSColor") :
        Libobjc.objc_getClass("UIColor");

    private static readonly IntPtr s_colorWithRedGreenBlueAlpha = Libobjc.sel_getUid("colorWithRed:green:blue:alpha:");

    private AppleColor(IntPtr handle, bool owns) : base(handle, owns)
    {
    }

    public static AppleColor FromHandle(IntPtr handle) => new(handle, false);

    public static AppleColor FromRGBA(double red, double green, double blue, double alpha)
    {
        return new AppleColor(Libobjc.intptr_objc_msgSend(s_class, s_colorWithRedGreenBlueAlpha, red, green, blue,
            alpha), true);
    }
}
