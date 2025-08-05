using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

internal enum COREWEBVIEW2_PRINT_ORIENTATION
{
    COREWEBVIEW2_PRINT_ORIENTATION_PORTRAIT,
    COREWEBVIEW2_PRINT_ORIENTATION_LANDSCAPE
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("377F3721-C74E-48CA-8DB1-DF68E51D60E2")]
internal partial interface ICoreWebView2PrintSettings
{
    COREWEBVIEW2_PRINT_ORIENTATION get_Orientation();
    void put_Orientation(COREWEBVIEW2_PRINT_ORIENTATION value);

    double get_ScaleFactor();
    void put_ScaleFactor(double value);

    double get_PageWidth();
    void put_PageWidth(double value);

    double get_PageHeight();
    void put_PageHeight(double value);

    double get_MarginTop();
    void put_MarginTop(double value);

    double get_MarginBottom();
    void put_MarginBottom(double value);

    double get_MarginLeft();
    void put_MarginLeft(double value);

    double get_MarginRight();
    void put_MarginRight(double value);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool get_ShouldPrintBackgrounds();

    void put_ShouldPrintBackgrounds([MarshalAs(UnmanagedType.Bool)] bool value);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool get_ShouldPrintSelectionOnly();

    void put_ShouldPrintSelectionOnly([MarshalAs(UnmanagedType.Bool)] bool value);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool get_ShouldPrintHeaderAndFooter();

    void put_ShouldPrintHeaderAndFooter([MarshalAs(UnmanagedType.Bool)] bool value);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string get_HeaderTitle();

    void put_HeaderTitle([MarshalAs(UnmanagedType.LPWStr)] string value);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string get_FooterUri();

    void put_FooterUri([MarshalAs(UnmanagedType.LPWStr)] string value);
}
