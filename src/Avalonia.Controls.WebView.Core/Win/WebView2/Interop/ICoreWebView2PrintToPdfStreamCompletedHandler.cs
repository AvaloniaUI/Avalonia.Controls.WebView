using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.Interop;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("4C9F8229-8F93-444F-A711-2C0DFD6359D5")]
internal partial interface ICoreWebView2PrintToPdfStreamCompletedHandler
{
    void Invoke(int errorCode, IComStream result);
}
