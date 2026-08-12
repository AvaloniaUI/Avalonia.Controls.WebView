using System;
using System.ComponentModel;
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

        if (Libobjc.objc_getProtocol("WKScriptMessageHandler") is var protocol and > 0)
        {
            AddProtocol(delegateClass, protocol);   
        }

        var willPresentNotificationSel = Libobjc.sel_getUid("userContentController:didReceiveScriptMessage:");
        AddMethod(delegateClass, willPresentNotificationSel, new IntPtr(s_didReceiveScriptMessage), "v@:@@");

        RegisterManagedMembers(delegateClass);

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
