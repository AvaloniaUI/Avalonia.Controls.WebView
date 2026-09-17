using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Platform;
using static Avalonia.Controls.Gtk.GtkInterop;

namespace Avalonia.Controls.Gtk;

internal class GtkPrintOperation : IDisposable
{
    // GTK_UNIT_POINTS = 1 (1 point = 1/72 inch)
    private const int GtkUnitPoints = 1;

    private readonly TaskCompletionSource<bool> _tcs;
    private readonly GtkSignal _failedCallback;
    private readonly GtkSignal _finishedCallback;
    private readonly IntPtr _operation;
    private readonly IntPtr _settings;
    private IntPtr _pageSetup;

    public unsafe GtkPrintOperation(IntPtr webView)
    {
        _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _operation = webkit_print_operation_new(webView);
        _settings = gtk_print_settings_new();
        webkit_print_operation_set_print_settings(_operation, _settings);

        _failedCallback = new GtkSignal(_operation, "failed",
            new((delegate* unmanaged[Cdecl]<IntPtr, GError*, IntPtr, void>)&PrintOperationFailedCallback), this);
        _finishedCallback = new GtkSignal(_operation, "finished",
            new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)&PrintOperationFinishedCallback), this);
    }

    public Task Task => _tcs.Task;

    public void ApplySettings(WebViewPrintSettings settings)
    {
        // Set scale factor via print settings (scale is in percentage, 100 = 100%)
        gtk_print_settings_set(_settings, "scale", (settings.ScaleFactor * 100).ToString("F0", System.Globalization.CultureInfo.InvariantCulture));

        // Create page setup for orientation and margins
        _pageSetup = gtk_page_setup_new();

        // Set orientation (GTK_PAGE_ORIENTATION_PORTRAIT = 0, GTK_PAGE_ORIENTATION_LANDSCAPE = 1)
        var gtkOrientation = settings.Orientation == WebViewPrintOrientation.Landscape ? 1 : 0;
        gtk_page_setup_set_orientation(_pageSetup, gtkOrientation);

        // Set margins (converting pixels to points: 1 pixel ≈ 0.75 points at 96 DPI)
        // Using points as unit since it's the standard for printing
        const double pixelsToPoints = 72.0 / 96.0; // 96 DPI standard screen, 72 points per inch
        gtk_page_setup_set_top_margin(_pageSetup, settings.MarginTop * pixelsToPoints, GtkUnitPoints);
        gtk_page_setup_set_bottom_margin(_pageSetup, settings.MarginBottom * pixelsToPoints, GtkUnitPoints);
        gtk_page_setup_set_left_margin(_pageSetup, settings.MarginLeft * pixelsToPoints, GtkUnitPoints);
        gtk_page_setup_set_right_margin(_pageSetup, settings.MarginRight * pixelsToPoints, GtkUnitPoints);

        webkit_print_operation_set_page_setup(_operation, _pageSetup);
    }

    public void PrintToFile(string outputFile)
    {
        gtk_print_settings_set(_settings, "output-file-format", "pdf");
        gtk_print_settings_set(_settings, "printer", "Print to File");
        gtk_print_settings_set(_settings, "output-uri", new Uri(outputFile).AbsoluteUri);
        webkit_print_operation_print(_operation);
    }

    public void RunDialog(IntPtr window)
    {
        webkit_print_operation_run_dialog(_operation, window);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void PrintOperationFailedCallback(IntPtr operation, GError* error, IntPtr data)
    {
        if (!GtkSignal.TryGetState<GtkPrintOperation>(data, out var state))
        {
            return;
        }

        var exception = Marshal.PtrToStringAuto(error->Message);
        state._tcs.TrySetException(new Exception(exception));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void PrintOperationFinishedCallback(IntPtr operation, IntPtr data)
    {
        if (!GtkSignal.TryGetState<GtkPrintOperation>(data, out var state))
        {
            return;
        }

        state._tcs.TrySetResult(true);
    }

    public void Dispose()
    {
        _failedCallback.Dispose();
        _finishedCallback.Dispose();
        g_object_unref(_operation);
        g_object_unref(_settings);
        if (_pageSetup != IntPtr.Zero)
        {
            g_object_unref(_pageSetup);
        }
    }
}
