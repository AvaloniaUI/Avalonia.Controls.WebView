using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("453E667F-12C7-49D4-BE6D-DDBE7956F57A")]
internal partial interface ICoreWebView2WebResourceRequestedEventArgs
{
    ICoreWebView2WebResourceRequest GetRequest();

    IntPtr GetResponse();
    void SetResponse(IntPtr response);

    IntPtr GetDeferral();

    int GetResourceContext();
}
