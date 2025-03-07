using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Macios.Interop;

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

    public static unsafe Dictionary<string, object?> AsStringDictionary(IntPtr handle)
    {
        var dictionary = new Dictionary<string, object?>();

        if (handle != default
            && CFDictionaryGetCount(handle) is var count and > 0)
        {
            var keys = new IntPtr[count];
            var values = new IntPtr[count];
            fixed (IntPtr* keysPtr = keys)
            fixed (IntPtr* valuesPtr = values)
            {
                CFDictionaryGetKeysAndValues(handle, keysPtr, valuesPtr);
            }

            for (var i = 0; i < count; i++)
            {
                var key = NSString.GetString(keys[i])!;
                if (NSString.TryGetString(values[i]) is { } strVal)
                    dictionary.Add(key, strVal);
                else if (NSDate.TryAsDateTimeOffset(values[i]) is { } dateVal)
                    dictionary.Add(key, dateVal);
                else if (NSNumber.TryAsStringValue(values[i]) is { } numberVal)
                    dictionary.Add(key, numberVal);
                else 
                    dictionary.Add(key, GetDescription(values[i]));
            }
        }

        return dictionary;
    }

    private const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    [DllImport(CoreFoundationLibrary)]
    private static extern long CFDictionaryGetCount(IntPtr dict);
    [DllImport(CoreFoundationLibrary)]
    private static extern unsafe void CFDictionaryGetKeysAndValues(IntPtr dict, IntPtr* keys, IntPtr* values);
}
