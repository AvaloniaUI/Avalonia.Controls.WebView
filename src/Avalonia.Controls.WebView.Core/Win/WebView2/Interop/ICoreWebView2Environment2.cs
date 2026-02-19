using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("41F3632B-5EF4-404F-AD82-2D606C5A9A21")]
internal partial interface ICoreWebView2Environment2 : ICoreWebView2Environment
{
    IntPtr CreateWebResourceRequest([MarshalAs(UnmanagedType.LPWStr)] string uri, [MarshalAs(UnmanagedType.LPWStr)] string Method, [MarshalAs(UnmanagedType.Interface)] IStream postData, [MarshalAs(UnmanagedType.LPWStr)] string Headers);
}
