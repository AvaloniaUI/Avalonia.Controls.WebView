using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView1.Interop;

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("57a87c53-47a5-5864-9881-fd4c00f230a9")]
internal partial interface IWebViewControlNavigationCompletedHandler
{
    void Invoke(IntPtr sender, IWebViewControlNavigationCompletedEventArgs args);
}
