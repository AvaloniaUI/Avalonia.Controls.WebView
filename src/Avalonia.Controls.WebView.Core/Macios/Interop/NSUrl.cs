using System;

namespace Avalonia.Controls.Macios.Interop;

internal class NSUrl : NSObject
{
    private static readonly IntPtr s_class = Foundation.objc_getClass("NSURL");
    private static readonly IntPtr s_createWithUrl = Libobjc.sel_getUid("URLWithString:");
    private static readonly IntPtr s_absoluteString = Libobjc.sel_getUid("absoluteString");

    public NSUrl(IntPtr handle, bool owns) : base(handle, owns)
    {
    }

    public NSUrl(NSString nsString) : this(Libobjc.intptr_objc_msgSend(s_class, s_createWithUrl, nsString.Handle), true)
    {
    }

    public string? AbsoluteString
    {
        get
        {
            var nsString = Libobjc.intptr_objc_msgSend(Handle, s_absoluteString);
            return NSString.GetString(nsString);
        }
    }
}
