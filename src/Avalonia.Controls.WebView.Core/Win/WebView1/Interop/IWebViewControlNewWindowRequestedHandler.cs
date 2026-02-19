using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView1.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("317111df-10c6-559c-85a1-847eb0a1b2d5")]
internal partial interface IWebViewControlNewWindowRequestedHandler
{
    void Invoke(IntPtr sender, IWebViewControlNewWindowRequestedEventArgs args);
}
