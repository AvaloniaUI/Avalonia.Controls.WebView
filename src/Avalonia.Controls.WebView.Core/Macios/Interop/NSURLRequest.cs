using System;

namespace Avalonia.Controls.Macios.Interop;

internal class NSURLRequest : NSObject
{
    private static readonly IntPtr s_class = Libobjc.objc_getClass("NSURLRequest");
    private static readonly IntPtr s_requestWithURL = Libobjc.sel_getUid("requestWithURL:");
    private static readonly IntPtr s_url = Libobjc.sel_getUid("URL");

    public NSURLRequest(IntPtr handle, bool owns) : base(handle, owns)
    {
    }

    public NSUrl Url => new(Libobjc.intptr_objc_msgSend(Handle, s_url), false);

    public static NSURLRequest FromUri(Uri uri)
    {
        using var nsStr = NSString.Create(uri.ToString());
        using var nsUrl = new NSUrl(nsStr);
        var handle = Libobjc.intptr_objc_msgSend(s_class, s_requestWithURL, nsUrl.Handle);
        return new NSURLRequest(handle, false);
    }
}
