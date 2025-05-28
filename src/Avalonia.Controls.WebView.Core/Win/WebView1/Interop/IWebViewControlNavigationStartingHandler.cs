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
[Guid("e92e0bcc-9ae9-5b9b-a684-83dd8ee57775")]
internal partial interface IWebViewControlNavigationStartingHandler
{
    void Invoke(IntPtr sender, IWebViewControlNavigationStartingEventArgs args);
}
