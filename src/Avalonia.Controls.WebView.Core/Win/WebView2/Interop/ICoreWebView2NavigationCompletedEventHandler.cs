using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("D33A35BF-1C49-4F98-93AB-006E0533FE1C")]
internal partial interface ICoreWebView2NavigationCompletedEventHandler
{
    void Invoke([MarshalAs(UnmanagedType.Interface)] ICoreWebView2 sender, [MarshalAs(UnmanagedType.Interface)] ICoreWebView2NavigationCompletedEventArgs args);
}
