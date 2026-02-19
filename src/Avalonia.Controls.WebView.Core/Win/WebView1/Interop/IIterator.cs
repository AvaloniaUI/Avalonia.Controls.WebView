using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.Interop;

namespace Avalonia.Controls.Win.WebView1.Interop;

[GeneratedComInterface]
[Guid("6a79e863-4300-459a-9966-cbb660963ee1")]
internal partial interface IIterator : IInspectable
{
    IntPtr get_Current();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool get_HasCurrent();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool MoveNext();

    uint GetMany(uint count, uint itemsSize, IntPtr items);
}

[GeneratedComInterface]
[Guid("faa585ea-6214-4217-afda-7f46de5869b3")]
internal partial interface IIterable : IInspectable
{
    IIterator First();
}
