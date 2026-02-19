using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("4E8A3389-C9D8-4BD2-B6B5-124FEE6CC14D")]
internal partial interface ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler
{
    void Invoke(int errorCode, [MarshalAs(UnmanagedType.Interface)] ICoreWebView2Environment result);
}
