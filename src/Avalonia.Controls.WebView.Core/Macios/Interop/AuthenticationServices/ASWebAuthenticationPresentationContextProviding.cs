using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Macios.Interop.AuthenticationServices;

internal unsafe class ASWebAuthenticationPresentationContextProviding(IntPtr windowHandle) : NSManagedObjectBase(s_class)
{
    private static readonly IntPtr s_class;

    private static readonly delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr>
        s_presentationAnchorForWebAuthenticationSession = &OnPresentationAnchorForWebAuthenticationSession;

    static ASWebAuthenticationPresentationContextProviding()
    {
        var delegateClass = AllocateClassPair("ManagedASWebAuthenticationPresentationContextProviding");

        var protocol = AuthenticationServices.objc_getProtocol("ASWebAuthenticationPresentationContextProviding");
        var result = Libobjc.class_addProtocol(delegateClass, protocol);
        Debug.Assert(result == 1);

        result = Libobjc.class_addMethod(delegateClass,
            Libobjc.sel_getUid("presentationAnchorForWebAuthenticationSession:"),
            s_presentationAnchorForWebAuthenticationSession,
            "@@:@");
        Debug.Assert(result == 1);

        result = RegisterManagedMembers(delegateClass) ? 1 : 0;
        Debug.Assert(result == 1);

        Libobjc.objc_registerClassPair(delegateClass);
        s_class = delegateClass;
    }

    private readonly IntPtr _windowHandle = windowHandle;
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr OnPresentationAnchorForWebAuthenticationSession(IntPtr self, IntPtr sel, IntPtr session)
    {
        var managedSelf = ReadManagedSelf<ASWebAuthenticationPresentationContextProviding>(self);
        return managedSelf?._windowHandle ?? default;
    }
}
