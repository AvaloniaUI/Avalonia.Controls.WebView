using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("B99369F3-9B11-47B5-BC6F-8E7895FCEA17")]
internal partial interface ICoreWebView2AddScriptToExecuteOnDocumentCreatedCompletedHandler
{
    void Invoke([MarshalAs(UnmanagedType.Error)] int errorCode, [MarshalAs(UnmanagedType.LPWStr)] string result);
}
