using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("2FDE08A8-1E9A-4766-8C05-95A9CEB9D1C5")]
internal partial interface ICoreWebView2EnvironmentOptions
{
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string? GetAdditionalBrowserArguments();

    void SetAdditionalBrowserArguments([MarshalAs(UnmanagedType.LPWStr)] string additionalBrowserArguments);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string? GetLanguage();

    void SetLanguage([MarshalAs(UnmanagedType.LPWStr)] string language);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetTargetCompatibleBrowserVersion();

    void SetTargetCompatibleBrowserVersion([MarshalAs(UnmanagedType.LPWStr)] string targetCompatibleBrowserVersion);

    int GetAllowSingleSignOnUsingOSPrimaryAccount();

    void SetAllowSingleSignOnUsingOSPrimaryAccount(int allowSingleSignOnUsingOSPrimaryAccount);
}
