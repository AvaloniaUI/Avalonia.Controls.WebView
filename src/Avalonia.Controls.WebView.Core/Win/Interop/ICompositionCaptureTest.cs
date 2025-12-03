using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.Interop;

/// --------------------------------------------------------------------- 
/// UNDOCUMENTED API ALERT
/// ---------------------------------------------------------------------
#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("2056F1E3-7DC8-4D28-AD74-B817F3481BB9")]
internal partial interface ICompositionCaptureTest
{
    [PreserveSig]
    int RenderVisual(
        ICompositionVisual visual,
        uint offsetX,
        uint offsetY,
        uint width,
        uint height,
        CompositionCaptureTestBitmapPixelFormat format,
        ref IntPtr hMap,
        ref IntPtr hEvent,
        out uint cbMap); // count in bytes of the map
}

internal enum CompositionCaptureTestBitmapPixelFormat : uint
{
    Rgba16 = 12,
    Rgba8 = 30,
    Gray16 = 57,
    Gray8 = 62,
    Bgra8 = 87
}
