using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Macios.Interop.WebKit;

internal unsafe class WKScriptMessageHandler : NSManagedObjectBase
{
    private static readonly IntPtr s_class;

    private static readonly IntPtr s_messageName = Libobjc.sel_getUid("name");
    private static readonly IntPtr s_messageBody = Libobjc.sel_getUid("body");

    private static readonly delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, void>
        s_didReceiveScriptMessage = &OnDidReceiveScriptMessage;

    static WKScriptMessageHandler()
    {
        var delegateClass = AllocateClassPair("ManagedWKScriptMessageHandler");

        var protocol = WebKit.objc_getProtocol("WKScriptMessageHandler");
        var result = Libobjc.class_addProtocol(delegateClass, protocol);
        Debug.Assert(result == 1);

        var willPresentNotificationSel = Libobjc.sel_getUid("userContentController:didReceiveScriptMessage:");
        result = Libobjc.class_addMethod(delegateClass, willPresentNotificationSel, s_didReceiveScriptMessage, "v@:@@");
        Debug.Assert(result == 1);

        result = RegisterManagedMembers(delegateClass) ? 1 : 0;
        Debug.Assert(result == 1);

        Libobjc.objc_registerClassPair(delegateClass);
        s_class = delegateClass;
    }

    public WKScriptMessageHandler() : base(s_class)
    {
        Init();
    }

    public event EventHandler<ScriptMessageEventArgs>? DidReceiveScriptMessage;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnDidReceiveScriptMessage(IntPtr self, IntPtr sel, IntPtr controller, IntPtr messagePtr)
    {
        var managed = ReadManagedSelf<WKScriptMessageHandler>(self);
        var messageName = NSString.GetString(Libobjc.intptr_objc_msgSend(messagePtr, s_messageName));
        managed?.DidReceiveScriptMessage?.Invoke(managed, new ScriptMessageEventArgs
        {
            Name = messageName,
            Body = Libobjc.intptr_objc_msgSend(messagePtr, s_messageBody)
        });
    }

    public class ScriptMessageEventArgs : CancelEventArgs
    {
        public string? Name { get; init; }
        public IntPtr Body { get; init; }
    }
}
