using System;

namespace Avalonia.Controls.Macios.Interop;

internal class NSArray(IntPtr handle, bool owns) : NSObject(handle, owns)
{
    private static readonly IntPtr s_class = Libobjc.objc_getClass("NSArray");
    private static readonly IntPtr s_count = Libobjc.sel_getUid("count");
    private static readonly IntPtr s_getObjects = Libobjc.sel_getUid("getObjects:range:");
    private static readonly IntPtr s_arrayWithObject = Libobjc.sel_getUid("arrayWithObject:");

    public nint Count => Libobjc.intptr_objc_msgSend(Handle, s_count);

    public void GetObjects(IntPtr objects, nint from, nint length)
    {
        Libobjc.void_objc_msgSend(Handle, s_getObjects, objects, from, length);
    }

    public static NSArray FromObject(IntPtr @object)
    {
        return new NSArray(Libobjc.intptr_objc_msgSend(s_class, s_arrayWithObject, @object), false);
    }
}
