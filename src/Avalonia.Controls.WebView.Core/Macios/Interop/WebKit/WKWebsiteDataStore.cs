using System;

namespace Avalonia.Controls.Macios.Interop.WebKit;

internal class WKWebsiteDataStore(IntPtr handle, bool owns) : NSObject(handle, owns)
{
    private static readonly IntPtr s_class = Libobjc.objc_getClass("WKWebsiteDataStore");
    private static readonly IntPtr s_httpCookieStore = Libobjc.sel_getUid("httpCookieStore");
    private static readonly IntPtr s_defaultDataStore = Libobjc.sel_getUid("defaultDataStore");
    private static readonly IntPtr s_nonPersistentDataStore = Libobjc.sel_getUid("nonPersistentDataStore");
    private static readonly IntPtr s_dataStoreForIdentifier = Libobjc.sel_getUid("dataStoreForIdentifier:");

    public WKHTTPCookieStore HttpCookieStore => new(Libobjc.intptr_objc_msgSend(Handle, s_httpCookieStore), false);

    public static WKWebsiteDataStore Default =>
        new(Libobjc.intptr_objc_msgSend(s_class, s_defaultDataStore), false);
    public static WKWebsiteDataStore NonPersistent =>
        new(Libobjc.intptr_objc_msgSend(s_class, s_nonPersistentDataStore), false);
    public static WKWebsiteDataStore ForIdentifier(Guid identifier)
    {
        using var nsIdentifier = NSUUID.Create(identifier);
        return new WKWebsiteDataStore(
            Libobjc.intptr_objc_msgSend(s_class, s_dataStoreForIdentifier, nsIdentifier.Handle), true);
    }
}
