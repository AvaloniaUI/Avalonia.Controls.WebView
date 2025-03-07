using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Macios.Interop.WebKit;

internal unsafe class WKNavigationDelegate : NSManagedObjectBase
{
    private static readonly IntPtr s_class;

    private static readonly delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, void>
        s_willPresentNotification = &OnDidFinishNavigation;
    private static readonly delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, void>
        s_decidePolicyForNavigationAction = &OnDecidePolicyForNavigationAction;

    static WKNavigationDelegate()
    {
        var delegateClass = AllocateClassPair("ManagedWKNavigationDelegate");

        var protocol = WebKit.objc_getProtocol("WKNavigationDelegate");
        var result = Libobjc.class_addProtocol(delegateClass, protocol);
        Debug.Assert(result == 1);

        var willPresentNotificationSel = Libobjc.sel_getUid("webView:didFinishNavigation:");
        result = Libobjc.class_addMethod(delegateClass, willPresentNotificationSel, s_willPresentNotification, "v@:@@");
        Debug.Assert(result == 1);

        var didReceiveNotificationResponse = Libobjc.sel_getUid("webView:decidePolicyForNavigationAction:decisionHandler:");
        result = Libobjc.class_addMethod(delegateClass, didReceiveNotificationResponse, s_decidePolicyForNavigationAction, "v@:@@@");
        Debug.Assert(result == 1);

        result = RegisterManagedMembers(delegateClass) ? 1 : 0;
        Debug.Assert(result == 1);

        Libobjc.objc_registerClassPair(delegateClass);
        s_class = delegateClass;
    }

    public WKNavigationDelegate() : base(s_class)
    {
        Init();
    }

    public event EventHandler? DidFinishNavigation;
    public event EventHandler<DecidePolicyNavigationEventArgs>? DecidePolicyNavigation;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnDidFinishNavigation(IntPtr self, IntPtr sel, IntPtr webView, IntPtr navigation)
    {
        var managed = ReadManagedSelf<WKNavigationDelegate>(self);
        managed?.DidFinishNavigation?.Invoke(managed, EventArgs.Empty);
    }

    private static readonly IntPtr s_actionRequest = Libobjc.sel_getUid("request");

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnDecidePolicyForNavigationAction(IntPtr self, IntPtr sel, IntPtr webView, IntPtr navigationAction, IntPtr decisionHandler)
    {
        var managed = ReadManagedSelf<WKNavigationDelegate>(self);

        using var request = new NSURLRequest(Libobjc.intptr_objc_msgSend(navigationAction, s_actionRequest), false);
        using var nsUrl = request.Url;

        var args = new DecidePolicyNavigationEventArgs { Request = new Uri(nsUrl.AbsoluteString!) };
        managed?.DecidePolicyNavigation?.Invoke(managed, args);

        var callback = (delegate* unmanaged[Cdecl]<IntPtr, long, void>)BlockLiteral.GetCallback(decisionHandler);
        callback(decisionHandler, args.Cancel ? 0 : 1);
    }

    public class DecidePolicyNavigationEventArgs : CancelEventArgs
    {
        public Uri Request { get; init; }
    }
}
