using System;
using System.Collections.Generic;
using System.Net;
using Avalonia.Logging;

namespace Avalonia.Controls.Macios.Interop;

internal class NSHTTPCookie : NSObject
{
    private static readonly IntPtr s_class = Libobjc.objc_getClass("NSHTTPCookie");
    private static readonly IntPtr s_cookieWithProperties = Libobjc.sel_getUid("cookieWithProperties:");
    private static readonly IntPtr s_properties = Libobjc.sel_getUid("properties");

    public NSHTTPCookie(IntPtr handle, bool owns) : base(handle, owns)
    {
    }

    public static NSHTTPCookie Create(Cookie cookie)
    {
        var names = new List<NSObject>();
        var values = new List<NSObject>();

        void Add(NSObject value, NSObject key)
        {
            names.Add(value);
            values.Add(key);
        }

        if (string.IsNullOrEmpty(cookie.Value)
            || string.IsNullOrEmpty(cookie.Domain)
            || string.IsNullOrEmpty(cookie.Name))
        {
            throw new InvalidOperationException(
                "To successfully create a cookie, you must provide values for (at least) the Cookie.Name and Cookie.Value keys, and Cookie.Domain key.");
        }

        Add(NSString.Create("Value"), NSString.Create(cookie.Value));
        Add(NSString.Create("Domain"), NSString.Create(cookie.Domain));
        Add(NSString.Create("Name"), NSString.Create(cookie.Name));
        Add(NSString.Create("Path"), NSString.Create(string.IsNullOrEmpty(cookie.Path) ? "/" : cookie.Path));
        if (cookie.Secure)
            Add(NSString.Create("Secure"), NSString.Create("TRUE"));
        if (cookie.HttpOnly)
            Add(NSString.Create("HttpOnly"), NSString.Create("TRUE"));
        if (cookie.Expires != DateTime.MinValue)
            Add(NSString.Create("Expires"), NSDate.FromDateTimeOffset(cookie.Expires));

        try
        {
            using var dict = NSDictionary.WithObjects(values, names, (uint)names.Count);
            var handle = Libobjc.intptr_objc_msgSend(s_class, s_cookieWithProperties, dict.Handle);
            if (handle == default)
                throw new InvalidOperationException("NSHTTPCookie creation failed.");

            return new NSHTTPCookie(handle, true);
        }
        finally
        {
            foreach (var name in names)
            {
                name.Dispose();
            }

            foreach (var value in values)
            {
                value.Dispose();
            }
        }
    }

    public Cookie? ToSystemCookie()
    {
        var props = NSDictionary.AsStringDictionary(Libobjc.intptr_objc_msgSend(Handle, s_properties));
        object? GetValueOrDefault(string key)
        {
            return props.TryGetValue(key, out var prop) ? prop : null;
        }

        try
        {
            return new Cookie(
                (string)props["Name"]!,
                (string?)GetValueOrDefault("Value"),
                (string?)GetValueOrDefault("Path"),
                (string?)GetValueOrDefault("Domain"))
            {
                Expires =
                    (GetValueOrDefault("Expires") as DateTimeOffset? ?? DateTimeOffset.MinValue).UtcDateTime,
                Secure = (string?)GetValueOrDefault("Secure") == "TRUE",
                HttpOnly = (string?)GetValueOrDefault("HttpOnly") == "TRUE",
                Version = int.TryParse((string?)GetValueOrDefault("Version"), out var v) ? v : 0
            };
        }
        catch (Exception ex)
        {
            Logger.TryGet(LogEventLevel.Warning, "WebView")
                ?.Log(this, "Unable to read NSHTTPCookie:\r\n{Exception}", ex);
            return null;
        }
    }
}
