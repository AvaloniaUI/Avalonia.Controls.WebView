using System;
using System.Collections.Generic;

namespace AppleInterop;

internal class NSDictionary : NSObject
{
    private static readonly IntPtr s_class = Libobjc.objc_getClass("NSDictionary");
    private static readonly IntPtr s_dictionaryWithObjects = Libobjc.sel_getUid("dictionaryWithObjects:forKeys:count:");

    private NSDictionary(IntPtr handle, bool owns) : base(handle, owns)
    {
    }

    public static unsafe NSDictionary WithObjects(
        IReadOnlyList<NSObject> objects,
        IReadOnlyList<NSObject> keys,
        uint count)
    {
        var objPtrs = stackalloc IntPtr[objects.Count];
        for (var i = 0; i < objects.Count; i++)
        {
            objPtrs[i] = objects[i].Handle;
        }
        var keyPtrs = stackalloc IntPtr[keys.Count];
        for (var i = 0; i < keys.Count; i++)
        {
            keyPtrs[i] = keys[i].Handle;
        }

        var handle = Libobjc.intptr_objc_msgSend(s_class, s_dictionaryWithObjects, new IntPtr(objPtrs), new IntPtr(keyPtrs), (int)count);
        return new NSDictionary(handle, true);
    }

    public static unsafe NSDictionary WithObjects(
        IntPtr[] objects,
        IntPtr[] keys,
        uint count)
    {
        fixed (void* objPtrs = objects)
        fixed (void* keyPtrs = keys)
        {
            var handle = Libobjc.intptr_objc_msgSend(s_class, s_dictionaryWithObjects, new IntPtr(objPtrs),
                new IntPtr(keyPtrs), (int)count);
            return new NSDictionary(handle, true);
        }
    }
}
