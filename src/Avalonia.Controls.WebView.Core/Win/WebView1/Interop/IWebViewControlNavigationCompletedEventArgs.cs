using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.Interop;

namespace Avalonia.Controls.Win.WebView1.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("20409918-4a15-4c46-a55d-f79edb0bde8b")]
internal partial interface IWebViewControlNavigationCompletedEventArgs : IInspectable
{    IUriRuntimeClass get_Uri();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool get_IsSuccess();
}
