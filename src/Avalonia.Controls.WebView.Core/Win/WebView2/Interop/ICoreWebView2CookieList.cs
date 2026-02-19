using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("F7F6F714-5D2A-43C6-9503-346ECE02D186")]
internal partial interface ICoreWebView2CookieList
{
    uint GetCount();

    ICoreWebView2Cookie GetValueAtIndex(uint index);
}
