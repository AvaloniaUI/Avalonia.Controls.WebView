using System;

namespace Avalonia.Controls.Macios.Interop;

internal class NSPrintInfo : NSObject
{
    private static readonly IntPtr s_class = Foundation.objc_getClass("NSPrintInfo");

    public NSPrintInfo() : base(s_class)
    {
        Init();
    }
}
