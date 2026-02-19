using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.Interop;

namespace Avalonia.Controls.Win.WebView1.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("ac3d28ac-8362-51c6-b2cc-16f3672758f1")]
internal partial interface IAsyncOperation_WebViewControl : IInspectable
{    void put_Completed(IAsyncOperationCompletedHandler_WebViewControl handler);

    IAsyncOperationCompletedHandler_WebViewControl get_Completed();

    IWebViewControl GetResults();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("d61963d6-806d-50a8-a81c-75d9356ad5d7")]
internal partial interface IAsyncOperationCompletedHandler_WebViewControl
{
    void Invoke(IAsyncOperation_WebViewControl asyncInfo, AsyncStatus asyncStatus);
}
