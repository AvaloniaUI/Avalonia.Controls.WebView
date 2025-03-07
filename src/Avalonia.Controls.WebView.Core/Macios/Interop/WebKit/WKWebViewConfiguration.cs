using System;

namespace Avalonia.Controls.Macios.Interop.WebKit;

internal class WKWebViewConfiguration : NSObject
{
    private static readonly IntPtr s_class = WebKit.objc_getClass("WKWebViewConfiguration");
    private static readonly IntPtr s_defaultWebpagePreferences = Libobjc.sel_getUid("defaultWebpagePreferences");
    private static readonly IntPtr s_setAllowsContentJavaScript = Libobjc.sel_getUid("setAllowsContentJavaScript:");

    private static readonly IntPtr s_websiteDataStore = Libobjc.sel_getUid("websiteDataStore");
    private static readonly IntPtr s_userContentController = Libobjc.sel_getUid("userContentController");
    private static readonly IntPtr s_contentAddScriptMessageHandler = Libobjc.sel_getUid("addScriptMessageHandler:name:");
    private static readonly IntPtr s_contentRemoveScriptMessageHandlerForName = Libobjc.sel_getUid("removeScriptMessageHandlerForName:");

    public WKWebViewConfiguration() : base(s_class)
    {
        Init();
    }

    public WKWebsiteDataStore WebsiteDataStore => new(Libobjc.intptr_objc_msgSend(Handle, s_websiteDataStore), false);

    public bool JavaScriptEnabled
    {
        set
        {
            var defaultPreferences = Libobjc.intptr_objc_msgSend(Handle, s_defaultWebpagePreferences);
            Libobjc.void_objc_msgSend(defaultPreferences, s_setAllowsContentJavaScript, value ? 1 : 0);
        }
    }

    public void AddScriptMessageHandler(WKScriptMessageHandler scriptHandler, NSString handlerName)
    {
        var controllerPtr = Libobjc.intptr_objc_msgSend(Handle, s_userContentController);
        Libobjc.void_objc_msgSend(controllerPtr, s_contentAddScriptMessageHandler, scriptHandler.Handle, handlerName.Handle);
    }

    public void RemoveScriptMessageHandler(NSString handlerName)
    {
        var controllerPtr = Libobjc.intptr_objc_msgSend(Handle, s_userContentController);
        Libobjc.void_objc_msgSend(controllerPtr, s_contentRemoveScriptMessageHandlerForName, handlerName.Handle);
    }

    public void EnableDeveloperExtras()
    {
        var preferences = Libobjc.intptr_objc_msgSend(Handle, Libobjc.sel_getUid("preferences"));
        using var key = NSString.Create("developerExtrasEnabled");
        Libobjc.void_objc_msgSend(
            preferences,
            Libobjc.sel_getUid("setValue:forKey:"),
            NSNumber.Yes.Handle,
            key.Handle);
    }
}
