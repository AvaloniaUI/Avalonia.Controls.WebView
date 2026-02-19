using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("0702FC30-F43B-47BB-AB52-A42CB552AD9F")]
internal partial interface ICoreWebView2HttpHeadersCollectionIterator
{
    void GetCurrentHeader([MarshalAs(UnmanagedType.LPWStr)] out string name, [MarshalAs(UnmanagedType.LPWStr)] out string value);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetHasCurrentHeader();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool MoveNext();
}
