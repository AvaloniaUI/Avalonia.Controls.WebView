using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("603E88E4-A338-4FFE-A457-A5CFB9CEB899")]
internal partial interface IDispatcherQueueHandler
{
    void Invoke();
};
