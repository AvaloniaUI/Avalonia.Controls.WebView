using System;
using System.Collections.Generic;

namespace Avalonia.Controls.Macios.Interop;

internal sealed class NSHTTPURLResponse : NSObject
{
    private static readonly IntPtr s_class = Libobjc.objc_getClass("NSHTTPURLResponse");
    private static readonly IntPtr s_alloc = Libobjc.sel_getUid("alloc");
    private static readonly IntPtr s_initWithURL =
        Libobjc.sel_getUid("initWithURL:statusCode:HTTPVersion:headerFields:");

    private NSHTTPURLResponse(IntPtr handle) : base(handle, true)
    {
        // Balances alloc, Dispose releases the last reference.
        Release();
    }

    public static NSHTTPURLResponse Create(NSUrl url, int statusCode, ICollection<KeyValuePair<string, string>> headers)
    {
        var keys = new List<NSObject>(headers.Count);
        var values = new List<NSObject>(headers.Count);
        try
        {
            foreach (var header in headers)
            {
                keys.Add(NSString.Create(header.Key));
                values.Add(NSString.Create(header.Value));
            }

            // The dictionary retains its keys and values.
            using var headerFields = NSDictionary.WithObjects(values, keys, (uint)keys.Count);
            using var httpVersion = NSString.Create("HTTP/1.1");
            var allocated = Libobjc.intptr_objc_msgSend(s_class, s_alloc);
            return new NSHTTPURLResponse(Libobjc.intptr_objc_msgSend(
                allocated, s_initWithURL, url.Handle, (IntPtr)statusCode, httpVersion.Handle, headerFields.Handle));
        }
        finally
        {
            keys.ForEach(key => key.Dispose());
            values.ForEach(value => value.Dispose());
        }
    }
}
