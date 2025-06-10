using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("B96D755E-0319-4E92-A296-23436F46A1FC")]
internal partial interface ICoreWebView2Environment
{
    void CreateCoreWebView2Controller(IntPtr ParentWindow, [MarshalAs(UnmanagedType.Interface)] ICoreWebView2CreateCoreWebView2ControllerCompletedHandler handler);

    IntPtr CreateWebResourceResponse(IntPtr Content, int StatusCode, [MarshalAs(UnmanagedType.LPWStr)] string ReasonPhrase, [MarshalAs(UnmanagedType.LPWStr)] string Headers);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetBrowserVersionString();

    void add_NewBrowserVersionAvailable([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);
    
    void remove_NewBrowserVersionAvailable(EventRegistrationToken token);
}
