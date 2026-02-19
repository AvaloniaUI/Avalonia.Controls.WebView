using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("22f34e66-50db-4e36-a98d-61c01b384d20")]
internal partial interface IDispatcherQueueController : IInspectable
{
    IDispatcherQueue GetDispatcherQueue();
    void ShutdownQueueAsync(/*Windows.Foundation.IAsyncAction*/ IntPtr operation);
};
