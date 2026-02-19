using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
// ReSharper disable InconsistentNaming

namespace Avalonia.Controls.Win.WebView2.Interop;

internal enum COREWEBVIEW2_BOUNDS_MODE
{
    COREWEBVIEW2_BOUNDS_MODE_USE_RAW_PIXELS,
    COREWEBVIEW2_BOUNDS_MODE_USE_RASTERIZATION_SCALE
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("F9614724-5D2B-41DC-AEF7-73D62B51543B")]
internal partial interface ICoreWebView2Controller3 : ICoreWebView2Controller2
{
    double GetRasterizationScale();
    void SetRasterizationScale(double value);

    int GetShouldDetectMonitorScaleChanges();
    void SetShouldDetectMonitorScaleChanges(int value);

    void add_RasterizationScaleChanged([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);
    void remove_RasterizationScaleChanged(EventRegistrationToken token);

    COREWEBVIEW2_BOUNDS_MODE GetBoundsMode();
    void SetBoundsMode(COREWEBVIEW2_BOUNDS_MODE value);
}
