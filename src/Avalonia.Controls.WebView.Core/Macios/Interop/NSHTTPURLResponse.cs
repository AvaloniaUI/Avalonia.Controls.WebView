using System;
using System.Collections.Generic;

namespace Avalonia.Controls.Macios.Interop;

internal sealed class NSHTTPURLResponse : NSObject
{
    private static readonly IntPtr s_class = Libobjc.objc_getClass("NSHTTPURLResponse");
    private static readonly IntPtr s_initWithURL =
        Libobjc.sel_getUid("initWithURL:statusCode:HTTPVersion:headerFields:");

    private NSHTTPURLResponse(IntPtr handle, bool owns) : base(handle, owns)
    {
    }

    public static NSHTTPURLResponse Create(
        NSUrl url,
        int statusCode,
        string httpVersion,
        IReadOnlyDictionary<string, string> headers)
    {
        var keys = new List<NSObject>(headers.Count);
        var values = new List<NSObject>(headers.Count);
        foreach (var pair in headers)
        {
            keys.Add(NSString.Create(pair.Key));
            values.Add(NSString.Create(pair.Value));
        }

        using var headerDict = NSDictionary.WithObjects(values, keys, (uint)keys.Count);
        using var version = NSString.Create(httpVersion);

        var allocated = Libobjc.intptr_objc_msgSend(s_class, Libobjc.sel_getUid("alloc"));
        var handle = Libobjc.intptr_objc_msgSend(
            allocated,
            s_initWithURL,
            url.Handle,
            (nint)statusCode,
            version.Handle,
            headerDict.Handle);

        return new NSHTTPURLResponse(handle, true);
    }
}
