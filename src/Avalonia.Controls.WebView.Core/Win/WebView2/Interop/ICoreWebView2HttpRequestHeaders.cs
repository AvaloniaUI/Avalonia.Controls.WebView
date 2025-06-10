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
[Guid("E86CAC0E-5523-465C-B536-8FB9FC8C8C60")]
internal partial interface ICoreWebView2HttpRequestHeaders
{
    [PreserveSig]
    int GetHeader([MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.LPWStr)] out string header);

    ICoreWebView2HttpHeadersCollectionIterator GetHeaders([MarshalAs(UnmanagedType.LPWStr)] string name);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool Contains([MarshalAs(UnmanagedType.LPWStr)] string name);

    [PreserveSig]
    int SetHeader([MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.LPWStr)] string value);

    [PreserveSig]
    int RemoveHeader([MarshalAs(UnmanagedType.LPWStr)] string name);

    ICoreWebView2HttpHeadersCollectionIterator GetIterator();
}
