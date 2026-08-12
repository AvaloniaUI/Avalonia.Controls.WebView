using System;
using System.ComponentModel;
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

        if (Libobjc.objc_getProtocol("WKNavigationDelegate") is var protocol and > 0)
        {
            AddProtocol(delegateClass, protocol);   
        }

        var willPresentNotificationSel = Libobjc.sel_getUid("webView:didFinishNavigation:");
        AddMethod(delegateClass, willPresentNotificationSel, new IntPtr(s_willPresentNotification), "v@:@@");

        var didReceiveNotificationResponse = Libobjc.sel_getUid("webView:decidePolicyForNavigationAction:decisionHandler:");
        AddMethod(delegateClass, didReceiveNotificationResponse, new IntPtr(s_decidePolicyForNavigationAction), "v@:@@@");

        RegisterManagedMembers(delegateClass);

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
    private static readonly IntPtr s_navigationType = Libobjc.sel_getUid("navigationType");
    private static readonly IntPtr s_targetFrame = Libobjc.sel_getUid("targetFrame");

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnDecidePolicyForNavigationAction(IntPtr self, IntPtr sel, IntPtr webView, IntPtr navigationAction, IntPtr decisionHandler)
    {
        var managed = ReadManagedSelf<WKNavigationDelegate>(self);

        using var request = NSURLRequest.FromHandle(Libobjc.intptr_objc_msgSend(navigationAction, s_actionRequest));

        var args = new DecidePolicyNavigationEventArgs
        {
            Request = request,
            TargetFrame = Libobjc.intptr_objc_msgSend(navigationAction, s_targetFrame)
        };
        managed?.DecidePolicyNavigation?.Invoke(managed, args);

        var callback = (delegate* unmanaged[Cdecl]<IntPtr, long, void>)BlockLiteral.GetCallback(decisionHandler);
        callback(decisionHandler, args.Cancel ? 0 : 1);
    }

    public class DecidePolicyNavigationEventArgs : CancelEventArgs
    {
        public required NSURLRequest Request { get; init; }
        public IntPtr TargetFrame { get; init; }
    }
}
