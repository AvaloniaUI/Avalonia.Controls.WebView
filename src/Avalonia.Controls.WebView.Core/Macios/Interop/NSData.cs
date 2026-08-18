using System;

namespace Avalonia.Controls.Macios.Interop;

internal sealed class NSData : NSObject
{
    private static readonly IntPtr s_class = Libobjc.objc_getClass("NSData");
    private static readonly IntPtr s_dataWithBytesLength = Libobjc.sel_getUid("dataWithBytes:length:");

    private NSData(IntPtr handle, bool owns) : base(handle, owns)
    {
    }

    public static unsafe NSData FromBytes(ReadOnlySpan<byte> bytes)
    {
        fixed (byte* ptr = bytes)
        {
            var handle = Libobjc.intptr_objc_msgSend(
                s_class,
                s_dataWithBytesLength,
                (IntPtr)ptr,
                new UIntPtr((uint)bytes.Length));
            return new NSData(handle, true);
        }
    }
}
