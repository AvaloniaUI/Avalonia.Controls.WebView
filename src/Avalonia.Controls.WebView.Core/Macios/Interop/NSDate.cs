using System;

namespace Avalonia.Controls.Macios.Interop;

internal class NSDate : NSObject
{
    private static readonly IntPtr s_class = Libobjc.objc_getClass("NSDate");
    private static readonly IntPtr s_dateWithTimeIntervalSince1970 = Libobjc.sel_getUid("dateWithTimeIntervalSince1970:");
    private static readonly IntPtr s_timeIntervalSince1970 = Libobjc.sel_getUid("timeIntervalSince1970");

    public NSDate(IntPtr handle, bool owns) : base(handle, owns)
    {
    }

    public static NSDate FromDateTimeOffset(DateTimeOffset dateTimeOffset)
    {
        var handle = Libobjc.intptr_objc_msgSend(s_class, s_dateWithTimeIntervalSince1970,
            dateTimeOffset.ToUnixTimeMilliseconds() / 1000d);
        return new NSDate(handle, true);
    }

    public DateTimeOffset ToDateTimeOffset() =>
        DateTimeOffset.FromUnixTimeMilliseconds((long)(Libobjc.double_objc_msgSend(Handle, s_timeIntervalSince1970) * 1000));

    public static DateTimeOffset? TryAsDateTimeOffset(IntPtr handle)
    {
        if (RespondsToSelector(handle, s_timeIntervalSince1970))
        {
            using var date = new NSDate(handle, false);
            return date.ToDateTimeOffset();
        }
        return null;
    }
}
