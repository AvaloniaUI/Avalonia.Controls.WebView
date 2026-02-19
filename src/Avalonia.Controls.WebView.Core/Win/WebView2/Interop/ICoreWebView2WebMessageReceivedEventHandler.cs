using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("57213F19-00E6-49FA-8E07-898EA01ECBD2")]
internal partial interface ICoreWebView2WebMessageReceivedEventHandler
{
    void Invoke([MarshalAs(UnmanagedType.Interface)] ICoreWebView2 sender, [MarshalAs(UnmanagedType.Interface)] ICoreWebView2WebMessageReceivedEventArgs args);
}
