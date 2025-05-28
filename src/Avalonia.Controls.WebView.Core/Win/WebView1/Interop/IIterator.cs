using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView1.Interop;

#if COM_SOURCE_GEN
[GeneratedComInterface]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("6a79e863-4300-459a-9966-cbb660963ee1")]
internal partial interface IIterator : IInspectable
{
#if !COM_SOURCE_GEN
    void _VtblGap1_3();
#endif

    IntPtr get_Current();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool get_HasCurrent();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool MoveNext();

    uint GetMany(uint count, uint itemsSize, IntPtr items);
}

#if COM_SOURCE_GEN
[GeneratedComInterface]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("faa585ea-6214-4217-afda-7f46de5869b3")]
internal partial interface IIterable : IInspectable
{
#if !COM_SOURCE_GEN
    void _VtblGap1_3();
#endif

    IIterator First();
}
