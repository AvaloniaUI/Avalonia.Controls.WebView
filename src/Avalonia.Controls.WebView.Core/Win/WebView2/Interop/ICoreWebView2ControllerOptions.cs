using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("12AAE616-8CCB-44EC-BCB3-EB1831881635")]
internal partial interface ICoreWebView2ControllerOptions
{
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetProfileName();
    void SetProfileName([MarshalAs(UnmanagedType.LPWStr)] string value);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetIsInPrivateModeEnabled();
    void SetIsInPrivateModeEnabled([MarshalAs(UnmanagedType.Bool)] bool value);
}
