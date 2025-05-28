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
[Guid("F7F6F714-5D2A-43C6-9503-346ECE02D186")]
internal partial interface ICoreWebView2CookieList
{
    uint GetCount();

    [return: MarshalAs(UnmanagedType.Interface)]
    ICoreWebView2Cookie GetValueAtIndex(uint index);
}
