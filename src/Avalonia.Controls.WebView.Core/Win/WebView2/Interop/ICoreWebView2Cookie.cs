using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

internal enum COREWEBVIEW2_COOKIE_SAME_SITE_KIND
{
    COREWEBVIEW2_COOKIE_SAME_SITE_KIND_NONE,
    COREWEBVIEW2_COOKIE_SAME_SITE_KIND_LAX,
    COREWEBVIEW2_COOKIE_SAME_SITE_KIND_STRICT
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("AD26D6BE-1486-43E6-BF87-A2034006CA21")]
internal partial interface ICoreWebView2Cookie
{
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetName();

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetValue();
    void SetValue([MarshalAs(UnmanagedType.LPWStr)] string value);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetDomain();

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetPath();

    double GetExpires();
    void SetExpires(double value);

    int GetIsHttpOnly();
    void SetIsHttpOnly(int value);

    COREWEBVIEW2_COOKIE_SAME_SITE_KIND GetSameSite();
    void SetSameSite(COREWEBVIEW2_COOKIE_SAME_SITE_KIND value);

    int GetIsSecure();
    void SetIsSecure(int value);

    int GetIsSession();
}
