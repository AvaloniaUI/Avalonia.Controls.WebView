using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("5A648006-843A-4DA9-865B-9D26E5DFAD7B")]
internal partial interface IAsyncAction : IInspectable
{
    void SetCompleted(IAsyncActionCompletedHandler handler);
    IAsyncActionCompletedHandler GetCompleted();
    IntPtr GetResults();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("A4ED5C81-76C9-40BD-8BE6-B1D90FB20AE7")]
internal partial interface IAsyncActionCompletedHandler
{
    void Invoke(IAsyncAction asyncInfo, AsyncStatus asyncStatus);
}
