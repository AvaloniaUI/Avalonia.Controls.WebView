using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.Interop;

namespace Avalonia.Controls.Win.WebView1.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("3e1fe603-f897-5263-b328-0806426b8a79")]
internal partial interface IAsyncOperation_HString : IInspectable
{
    void put_Completed(IAsyncOperationCompletedHandler_HString handler);

    IAsyncOperationCompletedHandler_HString get_Completed();

    IntPtr GetResults();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("b79a741f-7fb5-50ae-9e99-911201ec3d41")]
internal partial interface IAsyncOperationCompletedHandler_HString
{
    void Invoke(IAsyncOperation_HString asyncInfo, AsyncStatus asyncStatus);
}
