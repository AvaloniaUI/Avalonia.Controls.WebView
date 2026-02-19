using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.Interop;

namespace Avalonia.Controls.Win.WebView1.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("47B65CF9-A2D2-453C-B097-F6779D4B8E02")]
internal partial interface IWebViewControlProcessFactory : IInspectable
{    IWebViewControlProcess CreateWithOptions(IWebViewControlProcessOptions processOptions);
}
