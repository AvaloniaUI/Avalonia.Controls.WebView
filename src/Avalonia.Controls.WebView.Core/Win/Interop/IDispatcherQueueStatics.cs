using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("A96D83D7-9371-4517-9245-D0824AC12C74")]
internal partial interface IDispatcherQueueStatics : IInspectable
{
    IDispatcherQueue? GetForCurrentThread();
}
