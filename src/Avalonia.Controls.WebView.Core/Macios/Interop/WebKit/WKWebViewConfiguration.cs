using System;

namespace Avalonia.Controls.Macios.Interop.WebKit;

internal class WKWebViewConfiguration : NSObject
{
    private static readonly IntPtr s_class = Libobjc.objc_getClass("WKWebViewConfiguration");
    private static readonly IntPtr s_defaultWebpagePreferences = Libobjc.sel_getUid("defaultWebpagePreferences");
    private static readonly IntPtr s_setAllowsContentJavaScript = Libobjc.sel_getUid("setAllowsContentJavaScript:");

    private static readonly IntPtr s_websiteDataStore = Libobjc.sel_getUid("websiteDataStore");
    private static readonly IntPtr s_setWebsiteDataStore = Libobjc.sel_getUid("setWebsiteDataStore:");
    private static readonly IntPtr s_applicationNameForUserAgent = Libobjc.sel_getUid("applicationNameForUserAgent");
    private static readonly IntPtr s_setApplicationNameForUserAgent = Libobjc.sel_getUid("setApplicationNameForUserAgent:");
    private static readonly IntPtr s_upgradeKnownHostsToHTTPS = Libobjc.sel_getUid("upgradeKnownHostsToHTTPS");
    private static readonly IntPtr s_setUpgradeKnownHostsToHTTPS = Libobjc.sel_getUid("setUpgradeKnownHostsToHTTPS:");
    private static readonly IntPtr s_limitsNavigationsToAppBoundDomains = Libobjc.sel_getUid("limitsNavigationsToAppBoundDomains");
    private static readonly IntPtr s_setLimitsNavigationsToAppBoundDomains = Libobjc.sel_getUid("setLimitsNavigationsToAppBoundDomains:");
    private static readonly IntPtr s_userContentController = Libobjc.sel_getUid("userContentController");
    private static readonly IntPtr s_contentAddScriptMessageHandler = Libobjc.sel_getUid("addScriptMessageHandler:name:");
    private static readonly IntPtr s_contentRemoveScriptMessageHandlerForName = Libobjc.sel_getUid("removeScriptMessageHandlerForName:");

    public WKWebViewConfiguration() : base(s_class)
    {
        Init();
        Preferences = new WKPreferences(Libobjc.intptr_objc_msgSend(Handle, Libobjc.sel_getUid("preferences")));
    }

    public WKWebsiteDataStore WebsiteDataStore
    {
        get => new(Libobjc.intptr_objc_msgSend(Handle, s_websiteDataStore), false);
        set => Libobjc.void_objc_msgSend(Handle, s_setWebsiteDataStore, value.Handle);
    }

    public NSString ApplicationNameForUserAgent
    {
        get => NSString.FromHandle(Libobjc.intptr_objc_msgSend(Handle, s_applicationNameForUserAgent));
        set => Libobjc.void_objc_msgSend(Handle, s_setApplicationNameForUserAgent, value.Handle);
    }

    public bool UpgradeKnownHostsToHTTPS
    {
        get => Libobjc.int_objc_msgSend(Handle, s_upgradeKnownHostsToHTTPS) == 1;
        set => Libobjc.void_objc_msgSend(Handle, s_setUpgradeKnownHostsToHTTPS, value ? 1 : 0);
    }

    public bool LimitsNavigationsToAppBoundDomains
    {
        get => Libobjc.int_objc_msgSend(Handle, s_limitsNavigationsToAppBoundDomains) == 1;
        set => Libobjc.void_objc_msgSend(Handle, s_setLimitsNavigationsToAppBoundDomains, value ? 1 : 0);
    }

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

    public WKPreferences Preferences { get; }
}
