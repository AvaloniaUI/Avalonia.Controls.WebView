using System;
using System.Collections.Generic;

namespace AppleInterop.WebKit;

internal class WKWebViewConfiguration : NSObject
{
    private static readonly IntPtr s_class = WebKit.objc_getClass("WKWebViewConfiguration");
    private static readonly IntPtr s_defaultWebpagePreferences = Libobjc.sel_getUid("defaultWebpagePreferences");
    private static readonly IntPtr s_setAllowsContentJavaScript = Libobjc.sel_getUid("setAllowsContentJavaScript:");

    private static readonly IntPtr s_userContentController = Libobjc.sel_getUid("userContentController");
    private static readonly IntPtr s_contentAddScriptMessageHandler = Libobjc.sel_getUid("addScriptMessageHandler:name:");

    private readonly Dictionary<NSString, WKScriptMessageHandler> _handlers = [];

    public WKWebViewConfiguration() : base(s_class)
    {
        Init();
    }

    public bool JavaScriptEnabled
    {
        set
        {
            var defaultPreferences = Libobjc.intptr_objc_msgSend(Handle, s_defaultWebpagePreferences);
            Libobjc.void_objc_msgSend(defaultPreferences, s_setAllowsContentJavaScript, value ? 1 : 0);
        }
    }

    public void AddScriptMessageHandler(WKScriptMessageHandler scriptHandler, string postAvWebViewMessageName)
    {
        var str = NSString.Create(postAvWebViewMessageName);
        var controllerPtr = Libobjc.intptr_objc_msgSend(Handle, s_userContentController);
        _handlers.Add(str, scriptHandler); // root these objects
        Libobjc.void_objc_msgSend(controllerPtr, s_contentAddScriptMessageHandler, scriptHandler.Handle, str.Handle);
    }

    protected override void Dispose(bool disposing)
    {
        foreach (var key in _handlers.Keys)
        {
            key.Dispose();
        }
        _handlers.Clear();
        base.Dispose(disposing);
    }
}
