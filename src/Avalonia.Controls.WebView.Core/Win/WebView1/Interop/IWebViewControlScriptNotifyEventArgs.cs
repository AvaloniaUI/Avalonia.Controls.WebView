using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.Interop;

namespace Avalonia.Controls.Win.WebView1.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("491de57b-6f49-41bb-b591-51b85b817037")]
internal partial interface IWebViewControlScriptNotifyEventArgs : IInspectable
{    IUriRuntimeClass get_Uri();
    IntPtr get_Value();
}
