using System;

namespace Avalonia.Controls.Macios.Interop.WebKit;

internal class WKWebsiteDataStore(IntPtr handle, bool owns) : NSObject(handle, owns)
{
    private static readonly IntPtr s_httpCookieStore = Libobjc.sel_getUid("httpCookieStore");

    public WKHTTPCookieStore HttpCookieStore => new(Libobjc.intptr_objc_msgSend(Handle, s_httpCookieStore), false);
}
