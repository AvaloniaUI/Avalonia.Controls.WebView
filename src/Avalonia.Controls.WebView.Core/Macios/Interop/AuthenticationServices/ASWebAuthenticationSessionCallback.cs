using System;
using System.Runtime.Versioning;

namespace Avalonia.Controls.Macios.Interop.AuthenticationServices;

[SupportedOSPlatform("macos14.4")]
[SupportedOSPlatform("ios17.4")]
internal class ASWebAuthenticationSessionCallback : NSObject
{
    private static readonly IntPtr s_class = AuthenticationServices.objc_getClass("ASWebAuthenticationSessionCallback");
    private static readonly IntPtr s_callbackWithCustomScheme = Libobjc.sel_getUid("callbackWithCustomScheme:");

    private ASWebAuthenticationSessionCallback(IntPtr handle, bool owns) : base(handle, owns)
    {
    }

    public static ASWebAuthenticationSessionCallback FromCustomScheme(NSString customScheme)
    {
        var handle = Libobjc.intptr_objc_msgSend(s_class, s_callbackWithCustomScheme, customScheme.Handle);
        return new ASWebAuthenticationSessionCallback(handle, false);
    }
}
