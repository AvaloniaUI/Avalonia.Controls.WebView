using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView1.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("4D3C06F9-C8DF-41CC-8BD5-2A947B204503")]
internal partial interface IWebViewControl2 : IWebViewControl
{
    void AddInitializeScript(IntPtr script);
}
