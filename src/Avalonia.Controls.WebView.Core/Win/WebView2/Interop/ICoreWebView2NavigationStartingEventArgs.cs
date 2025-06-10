using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("5B495469-E119-438A-9B18-7604F25F2E49")]
internal partial interface ICoreWebView2NavigationStartingEventArgs
{
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetUri();

    int GetIsUserInitiated();

    int GetIsRedirected();

    ICoreWebView2NavigationStartingEventArgs GetRequestHeaders();

    int GetCancel();
    void SetCancel(int value);

    ulong GetNavigationId();
}
