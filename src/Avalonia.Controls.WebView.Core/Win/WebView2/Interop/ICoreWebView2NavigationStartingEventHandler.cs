using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("9ADBE429-F36D-432B-9DDC-F8881FBD76E3")]
internal partial interface ICoreWebView2NavigationStartingEventHandler
{
    void Invoke([MarshalAs(UnmanagedType.Interface)] ICoreWebView2 sender, [MarshalAs(UnmanagedType.Interface)] ICoreWebView2NavigationStartingEventArgs args);
}
