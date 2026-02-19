using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.Interop;

namespace Avalonia.Controls.Win.WebView1.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("44A9796F-723E-4FDF-A218-033E75B0C084")]
internal partial interface IUriRuntimeClassFactory : IInspectable
{    IUriRuntimeClass CreateUri(IntPtr uri);

    IUriRuntimeClass CreateWithRelativeUri(IntPtr baseUri, IntPtr relativeUri);
}
