using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("D60AC92C-37A6-4B26-A39E-95CFE59047BB")]
internal partial interface ICoreWebView2CustomSchemeRegistration
{
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetSchemeName();

    int GetTreatAsSecure();

    void SetTreatAsSecure(int treatAsSecure);

    void GetAllowedOrigins(out uint allowedOriginsCount, out IntPtr allowedOrigins);

    void SetAllowedOrigins(uint allowedOriginsCount, IntPtr allowedOrigins);

    int GetHasAuthorityComponent();

    void SetHasAuthorityComponent(int hasAuthorityComponent);
}
