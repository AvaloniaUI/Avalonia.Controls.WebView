using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Gtk;

internal class GtkSignal
    : IDisposable
{
    private static readonly unsafe IntPtr s_onDestroy = new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)&OnDestroy);

    private readonly nint _instance;
    private readonly ulong _signal;
    private readonly GCHandle _state;

    public GtkSignal(IntPtr instance, string signal, IntPtr callback, object state)
    {
        _state = GCHandle.Alloc(state);
        _signal = GtkInterop.g_signal_connect_data(
            instance,
            signal,
            callback,
            GCHandle.ToIntPtr(_state),
            s_onDestroy,
            0);
        _instance = instance;
    }

    public void Dispose()
    {
        GtkInterop.g_signal_handler_disconnect(_instance, _signal);
        _state.Free();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnDestroy(IntPtr data, IntPtr closure)
    {
        GCHandle.FromIntPtr(data).Free();
    }
}
