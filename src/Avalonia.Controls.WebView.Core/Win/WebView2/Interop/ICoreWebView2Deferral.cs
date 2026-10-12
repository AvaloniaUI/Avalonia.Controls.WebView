using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("C10E7F7B-B585-46F0-A623-8BEFBF3E4EE0")]
internal partial interface ICoreWebView2Deferral
{
    void Complete();
}
