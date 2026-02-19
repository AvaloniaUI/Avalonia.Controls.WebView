using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("02FAB84B-1428-4FB7-AD45-1B2E64736184")]
internal partial interface ICoreWebView2CreateCoreWebView2CompositionControllerCompletedHandler
{
    void Invoke(int errorCode, [MarshalAs(UnmanagedType.Interface)] ICoreWebView2CompositionController result);
}
