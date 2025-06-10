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
[Guid("97055CD4-512C-4264-8B5F-E3F446CEA6A5")]
internal partial interface ICoreWebView2WebResourceRequest
{
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetUri();
    void SetUri([MarshalAs(UnmanagedType.LPWStr)] string uri);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetMethod();
    void SetMethod([MarshalAs(UnmanagedType.LPWStr)] string method);

    /* IStream */ IntPtr GetContent();
    void SetContent(/* IStream */ IntPtr content);

    ICoreWebView2HttpRequestHeaders GetHeaders();
}

