using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView1.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("57a87c53-47a5-5864-9881-fd4c00f230a9")]
internal partial interface IWebViewControlNavigationCompletedHandler
{
    void Invoke(IntPtr sender, IWebViewControlNavigationCompletedEventArgs args);
}
