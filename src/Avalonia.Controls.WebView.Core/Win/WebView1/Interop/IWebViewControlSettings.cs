using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.Interop;

namespace Avalonia.Controls.Win.WebView1.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("C9967FBF-5E98-4CFD-8CCE-27B0911E3DE8")]
internal partial interface IWebViewControlSettings : IInspectable
{    void put_IsJavaScriptEnabled([MarshalAs(UnmanagedType.Bool)] bool value);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool get_IsJavaScriptEnabled();

    void put_IsIndexedDBEnabled([MarshalAs(UnmanagedType.Bool)] bool value);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool get_IsIndexedDBEnabled();

    void put_IsScriptNotifyAllowed([MarshalAs(UnmanagedType.Bool)] bool value);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool get_IsScriptNotifyAllowed();
}
