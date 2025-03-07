using System;

namespace Avalonia.Controls.Macios.Interop;

internal class NSArray(IntPtr handle, bool owns) : NSObject(handle, owns)
{
    private static readonly IntPtr s_count = Libobjc.sel_getUid("count");
    private static readonly IntPtr s_getObjects = Libobjc.sel_getUid("getObjects:range:");

    public nint Count => Libobjc.intptr_objc_msgSend(Handle, s_count);

    public void GetObjects(IntPtr objects, nint from, nint length)
    {
        Libobjc.void_objc_msgSend(Handle, s_getObjects, objects, from, length);
    }
}
