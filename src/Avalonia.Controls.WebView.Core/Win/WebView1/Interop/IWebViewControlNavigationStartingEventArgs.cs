using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.Interop;

namespace Avalonia.Controls.Win.WebView1.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("0c9057c5-0a08-41c7-863b-71e3a9549137")]
internal partial interface IWebViewControlNavigationStartingEventArgs : IInspectable
{    IUriRuntimeClass get_Uri();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool get_Cancel();

    void put_Cancel([MarshalAs(UnmanagedType.Bool)] bool value);
}
