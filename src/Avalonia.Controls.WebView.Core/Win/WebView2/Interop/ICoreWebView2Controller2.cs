using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
// ReSharper disable InconsistentNaming

namespace Avalonia.Controls.Win.WebView2.Interop;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct COREWEBVIEW2_COLOR
{
    public byte A;
    public byte R;
    public byte G;
    public byte B;
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("C979903E-D4CA-4228-92EB-47EE3FA96EAB")]
internal partial interface ICoreWebView2Controller2 : ICoreWebView2Controller
{
    COREWEBVIEW2_COLOR GetDefaultBackgroundColor();
    void SetDefaultBackgroundColor(COREWEBVIEW2_COLOR color);
}
