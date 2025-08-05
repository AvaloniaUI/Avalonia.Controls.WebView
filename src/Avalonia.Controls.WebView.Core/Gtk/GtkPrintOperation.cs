using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Avalonia.Controls.Gtk.GtkInterop;

namespace Avalonia.Controls.Gtk;

internal class GtkPrintOperation : IDisposable
{
    private readonly TaskCompletionSource<bool> _tcs;
    private readonly GtkSignal _failedCallback;
    private readonly GtkSignal _finishedCallback;
    private readonly IntPtr _operation;
    private readonly IntPtr _settings;

    public unsafe GtkPrintOperation(IntPtr webView, string outputFile)
    {
        _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _operation = webkit_print_operation_new(webView);
        _settings = gtk_print_settings_new();
        gtk_print_settings_set(_settings, "output-file-format", "pdf");
        gtk_print_settings_set(_settings, "printer", "Print to File");
        gtk_print_settings_set(_settings, "output-uri", new Uri(outputFile).AbsoluteUri);

        webkit_print_operation_set_print_settings(_operation, _settings);

        _failedCallback = new GtkSignal(_operation, "failed",
            new((delegate* unmanaged[Cdecl]<IntPtr, GError*, IntPtr, void>)&PrintOperationFailedCallback), this);
        _finishedCallback = new GtkSignal(_operation, "finished",
            new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)&PrintOperationFinishedCallback), this);
    }

    public Task Task => _tcs.Task;

    public void Print() => webkit_print_operation_print(_operation);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void PrintOperationFailedCallback(IntPtr operation, GError* error, IntPtr data)
    {
        if (!GtkSignal.TryGetState<GtkPrintOperation>(data, out var state))
        {
            return;
        }

        var exception = Marshal.PtrToStringAuto(error->Message);
        state._tcs.SetException(new Exception(exception));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void PrintOperationFinishedCallback(IntPtr operation, IntPtr data)
    {
        if (!GtkSignal.TryGetState<GtkPrintOperation>(data, out var state))
        {
            return;
        }

        state._tcs.SetResult(true);
    }

    public void Dispose()
    {
        _failedCallback.Dispose();
        _finishedCallback.Dispose();
        g_object_unref(_operation);
        g_object_unref(_settings);
    }
}
