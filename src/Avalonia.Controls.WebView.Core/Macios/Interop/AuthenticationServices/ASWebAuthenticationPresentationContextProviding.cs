using System;
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
        AuthenticationServices.PreloadAuthenticationServices();

        var delegateClass = AllocateClassPair("ManagedASWebAuthenticationPresentationContextProviding");

        if (Libobjc.objc_getProtocol("ASWebAuthenticationPresentationContextProviding") is var protocol and > 0)
        {
            AddProtocol(delegateClass, protocol);   
        }

        AddMethod(delegateClass,
            Libobjc.sel_getUid("presentationAnchorForWebAuthenticationSession:"),
            new IntPtr(s_presentationAnchorForWebAuthenticationSession),
            "@@:@");

        RegisterManagedMembers(delegateClass);

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
