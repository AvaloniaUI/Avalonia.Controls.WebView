using System;

namespace Avalonia.Controls.Macios.Interop;

internal class NSURLRequest : NSObject
{
    private static readonly IntPtr s_class = Libobjc.objc_getClass("NSURLRequest");
    private static readonly IntPtr s_requestWithURL = Libobjc.sel_getUid("requestWithURL:");
    private static readonly IntPtr s_allHTTPHeaderFields = Libobjc.sel_getUid("allHTTPHeaderFields");
    private static readonly IntPtr s_valueForHTTPHeaderField = Libobjc.sel_getUid("valueForHTTPHeaderField:");
    private static readonly IntPtr s_url = Libobjc.sel_getUid("URL");
    private static readonly IntPtr s_HTTPMethod = Libobjc.sel_getUid("HTTPMethod");

    protected NSURLRequest(IntPtr handle, bool owns) : base(handle, owns)
    {
    }

    public NSUrl Url => new(Libobjc.intptr_objc_msgSend(Handle, s_url), false);

    public NSString HTTPMethod => NSString.FromHandle(Libobjc.intptr_objc_msgSend(Handle, s_HTTPMethod));

    public static NSURLRequest FromHandle(IntPtr handle)
    {
        return RespondsToSelector(handle, NSMutableURLRequest.s_setValueForHTTPHeaderField) ?
            new NSMutableURLRequest(handle, false) :
            new NSURLRequest(handle, false);
    }

    public static NSURLRequest FromUri(Uri uri)
    {
        using var nsStr = NSString.Create(uri.AbsoluteUri);
        using var nsUrl = new NSUrl(nsStr);
        var handle = Libobjc.intptr_objc_msgSend(s_class, s_requestWithURL, nsUrl.Handle);
        if (handle == default)
        {
            throw new ArgumentException($"Unable to create NSURLRequest from Uri (Host: {uri.Host})");
        }

        return new NSMutableURLRequest(handle, false);
    }

    public NSDictionary AllHTTPHeaderFields =>
        NSDictionary.FromHandle(Libobjc.intptr_objc_msgSend(Handle, s_allHTTPHeaderFields));

    public string this[string key]
    {
        get
        {
            using var keyStr = NSString.Create(key);
            return NSString.GetString(Libobjc.intptr_objc_msgSend(Handle, s_valueForHTTPHeaderField, keyStr.Handle))
                ?? "";
        }
    }
}
