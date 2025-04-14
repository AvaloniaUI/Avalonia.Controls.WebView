/*
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Macios.Interop.WebKit;

internal unsafe class WKUIDelegate : NSManagedObjectBase
{
    private static readonly IntPtr s_class;

    private static readonly delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, void>
        s_createWebViewWithConfiguration = &OnDidFinishNavigation;

    static WKUIDelegate()
    {
        var delegateClass = AllocateClassPair("ManagedWKNavigationDelegate");

        var protocol = WebKit.objc_getProtocol("WKNavigationDelegate");
        var result = Libobjc.class_addProtocol(delegateClass, protocol);
        Debug.Assert(result == 1);

        var willPresentNotificationSel = Libobjc.sel_getUid("webView:didFinishNavigation:");
        result = Libobjc.class_addMethod(delegateClass, willPresentNotificationSel, s_willPresentNotification, "v@:@@");
        Debug.Assert(result == 1);

        var didReceiveNotificationResponse =
            Libobjc.sel_getUid("webView:decidePolicyForNavigationAction:decisionHandler:");
        result = Libobjc.class_addMethod(delegateClass, didReceiveNotificationResponse,
            s_decidePolicyForNavigationAction, "v@:@@@");
        Debug.Assert(result == 1);

        result = RegisterManagedMembers(delegateClass) ? 1 : 0;
        Debug.Assert(result == 1);

        Libobjc.objc_registerClassPair(delegateClass);
        s_class = delegateClass;
    }

    public WKUIDelegate() : base(s_class)
    {
        Init();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr CreateWebViewWithConfiguration(IntPtr self, IntPtr sel, IntPtr webView, IntPtr navigation)
    {
        var managed = ReadManagedSelf<WKNavigationDelegate>(self);
        managed?.DidFinishNavigation?.Invoke(managed, EventArgs.Empty);
    }
}
*/
