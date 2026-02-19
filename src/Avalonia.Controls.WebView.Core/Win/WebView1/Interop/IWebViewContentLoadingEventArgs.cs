using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.Interop;

namespace Avalonia.Controls.Win.WebView1.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("6E2980BB-98B8-413E-8129-971C6F7E4C8A")]
internal partial interface IWebViewContentLoadingEventArgs : IInspectable
{    IUriRuntimeClass get_Uri();
}
