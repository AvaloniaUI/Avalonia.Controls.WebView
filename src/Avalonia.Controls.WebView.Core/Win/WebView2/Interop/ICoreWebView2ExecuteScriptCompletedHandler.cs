using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("49511172-CC67-4BCA-9923-137112F4C4CC")]
internal partial interface ICoreWebView2ExecuteScriptCompletedHandler
{
    void Invoke(int errorCode, [MarshalAs(UnmanagedType.LPWStr)] string result);
}
