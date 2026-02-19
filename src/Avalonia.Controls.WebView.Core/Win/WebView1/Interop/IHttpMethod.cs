using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.Interop;

namespace Avalonia.Controls.Win.WebView1.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("728D4022-700D-4FE0-AFA5-40299C58DBFD")]
internal partial interface IHttpMethod : IInspectable
{
    IntPtr Method();
}
