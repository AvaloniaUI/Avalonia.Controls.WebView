using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("AB00B74C-15F1-4646-80E8-E76341D25D71")]
internal partial interface ICoreWebView2WebResourceRequestedEventHandler
{
    void Invoke([MarshalAs(UnmanagedType.Interface)] ICoreWebView2 sender, [MarshalAs(UnmanagedType.Interface)]  ICoreWebView2WebResourceRequestedEventArgs args);
}
