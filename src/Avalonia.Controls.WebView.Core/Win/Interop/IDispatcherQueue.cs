using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.WebView1.Interop;

namespace Avalonia.Controls.Win.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("603E88E4-A338-4FFE-A457-A5CFB9CEB899")]
internal partial interface IDispatcherQueue : IInspectable
{
    IInspectable CreateTimer();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool TryEnqueue(IDispatcherQueueHandler callback);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool TryEnqueueWithPriority(int priority, IDispatcherQueueHandler callback);

    void add_ShutdownStarting(IntPtr handler, out EventRegistrationToken token);
    void remove_ShutdownStarting(EventRegistrationToken token);
    void add_ShutdownCompleted(IntPtr handler, out EventRegistrationToken token);
    void remove_ShutdownCompleted(EventRegistrationToken token);
}
