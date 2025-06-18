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
[Guid("0F99A40C-E962-4207-9E92-E3D542EFF849")]
internal partial interface ICoreWebView2WebMessageReceivedEventArgs
{
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetSource();
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string WebMessageAsJson();
    [PreserveSig]
    int TryGetWebMessageAsString([MarshalAs(UnmanagedType.LPWStr)] out string result);
}
