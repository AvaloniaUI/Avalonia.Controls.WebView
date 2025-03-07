using System;

namespace Avalonia.Controls.Macios.Interop;

internal sealed class NSNumber : NSValue
{
    private static readonly IntPtr s_class = Libobjc.objc_getClass("NSNumber");
    private static readonly IntPtr s_numberWithBool = Libobjc.sel_getUid("numberWithBool:");
    private static readonly IntPtr s_stringValue = Libobjc.sel_getUid("stringValue");

    public static NSNumber Yes { get; } = new(true);
    public static NSNumber No { get; } = new(false);

    private NSNumber(bool value) : base(Libobjc.intptr_objc_msgSend(s_class, s_numberWithBool, value ? 1 : 0), true)
    {
    }

    public static string? AsStringValue(IntPtr handle)
    {
        return NSString.GetString(Libobjc.intptr_objc_msgSend(handle, s_stringValue));
    }

    public static string? TryAsStringValue(IntPtr handle)
    {
        if (RespondsToSelector(handle, s_stringValue))
            return AsStringValue(handle);
        return null;
    }
}
