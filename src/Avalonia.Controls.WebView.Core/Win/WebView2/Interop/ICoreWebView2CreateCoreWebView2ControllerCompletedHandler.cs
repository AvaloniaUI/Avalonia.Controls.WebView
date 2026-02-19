using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("6C4819F3-C9B7-4260-8127-C9F5BDE7F68C")]
internal partial interface ICoreWebView2CreateCoreWebView2ControllerCompletedHandler
{
    void Invoke(int errorCode, [MarshalAs(UnmanagedType.Interface)] ICoreWebView2Controller result);
}
