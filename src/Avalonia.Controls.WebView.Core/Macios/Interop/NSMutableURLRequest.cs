using System;

namespace Avalonia.Controls.Macios.Interop;

internal class NSMutableURLRequest : NSURLRequest
{
    private static readonly IntPtr s_class = Libobjc.objc_getClass("NSMutableURLRequest");
    internal static readonly IntPtr s_setValueForHTTPHeaderField = Libobjc.sel_getUid("setValue:forHTTPHeaderField:");

    public NSMutableURLRequest(IntPtr handle, bool owns) : base(handle, owns)
    {
    }

    public new string this[string key]
    {
        get => base[key];
        set
        {
            using var valueStr = NSString.Create(value);
            using var keyStr = NSString.Create(key);
            Libobjc.intptr_objc_msgSend(Handle, s_setValueForHTTPHeaderField, valueStr.Handle, keyStr.Handle);
        }
    }
}
