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
[Guid("9E8F0CF8-E670-4B5E-B2BC-73E061E3184C")]
internal partial interface ICoreWebView2_2 : ICoreWebView2
{
#if !COM_SOURCE_GEN
    void _VtblGap1_58();
#endif

    void add_WebResourceResponseReceived([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);
    void remove_WebResourceResponseReceived(EventRegistrationToken token);

    void NavigateWithWebResourceRequest([MarshalAs(UnmanagedType.Interface)] IntPtr Request);

    void add_DOMContentLoaded([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);
    void remove_DOMContentLoaded(EventRegistrationToken token);

    [return: MarshalAs(UnmanagedType.Interface)]
    ICoreWebView2CookieManager GetCookieManager();

    [return: MarshalAs(UnmanagedType.Interface)]
    ICoreWebView2Environment Environment();
}

