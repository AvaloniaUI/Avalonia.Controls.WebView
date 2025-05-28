using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("41F3632B-5EF4-404F-AD82-2D606C5A9A21")]
internal partial interface ICoreWebView2Environment2 : ICoreWebView2Environment
{
#if !COM_SOURCE_GEN
    void _VtblGap1_5();
#endif

    [return: MarshalAs(UnmanagedType.Interface)]
    IntPtr CreateWebResourceRequest([MarshalAs(UnmanagedType.LPWStr)] string uri, [MarshalAs(UnmanagedType.LPWStr)] string Method, [MarshalAs(UnmanagedType.Interface)] IStream postData, [MarshalAs(UnmanagedType.LPWStr)] string Headers);
}
