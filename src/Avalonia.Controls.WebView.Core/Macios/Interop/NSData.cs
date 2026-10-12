using System;

namespace Avalonia.Controls.Macios.Interop;

internal sealed class NSData : NSObject
{
    private static readonly IntPtr s_class = Libobjc.objc_getClass("NSData");
    private static readonly IntPtr s_alloc = Libobjc.sel_getUid("alloc");
    private static readonly IntPtr s_initWithBytesLength = Libobjc.sel_getUid("initWithBytes:length:");

    private NSData(IntPtr handle) : base(handle, true)
    {
        // Balances alloc, Dispose releases the last reference.
        Release();
    }

    /// <summary>
    /// Copies the bytes, without autoreleasing.
    /// </summary>
    public static unsafe NSData Create(ReadOnlySpan<byte> bytes)
    {
        fixed (byte* ptr = bytes)
        {
            var allocated = Libobjc.intptr_objc_msgSend(s_class, s_alloc);
            return new NSData(Libobjc.intptr_objc_msgSend(
                allocated, s_initWithBytesLength, (IntPtr)ptr, (IntPtr)bytes.Length));
        }
    }
}
