using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.Interop;

namespace Avalonia.Controls.Win.WebView1.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("3df44bbb-a124-46d5-a083-d02cacdff5ad")]
internal partial interface IWebViewControlNewWindowRequestedEventArgs : IInspectable
{    IUriRuntimeClass get_Uri();
    IUriRuntimeClass get_Referrer();
    [return: MarshalAs(UnmanagedType.Bool)]
    bool get_Handled();
    void put_Handled([MarshalAs(UnmanagedType.Bool)] bool value);
}
