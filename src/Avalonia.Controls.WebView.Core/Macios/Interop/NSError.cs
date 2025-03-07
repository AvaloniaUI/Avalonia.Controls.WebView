using System;

namespace Avalonia.Controls.Macios.Interop;

internal class NSError(IntPtr handle) : NSObject(handle, false)
{
    private static readonly IntPtr s_localizedDescription = Libobjc.sel_getUid("localizedDescription");
    private static readonly IntPtr s_domain = Libobjc.sel_getUid("domain");
    private static readonly IntPtr s_code = Libobjc.sel_getUid("code");
    private static readonly IntPtr s_userInfo = Libobjc.sel_getUid("userInfo");

    public static NSErrorException ToException(IntPtr nsError)
    {
        if (nsError == default)
            throw new ArgumentNullException(nameof(nsError));

        var ex = new NSErrorException(
            NSString.GetString(Libobjc.intptr_objc_msgSend(nsError, s_localizedDescription))!)
        {
            Domain = NSString.GetString(Libobjc.intptr_objc_msgSend(nsError, s_domain)),
            Code = Libobjc.int_objc_msgSend(nsError, s_code)
        };
        var data = NSDictionary.AsStringDictionary(Libobjc.intptr_objc_msgSend(nsError, s_userInfo));
        foreach (var pair in data)
        {
            ex.Data.Add(pair.Key, pair.Value);
        }

        return ex;
    }
}

internal class NSErrorException(string message) : Exception(message)
{
    public string? Domain { get; init; }
    public int Code { get; init; }
}
