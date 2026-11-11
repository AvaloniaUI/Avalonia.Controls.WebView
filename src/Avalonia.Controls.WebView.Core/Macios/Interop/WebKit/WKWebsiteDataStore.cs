using System;
using System.Linq;
using System.Net;

namespace Avalonia.Controls.Macios.Interop.WebKit;

internal class WKWebsiteDataStore(IntPtr handle, bool owns) : NSObject(handle, owns)
{
    private static readonly IntPtr s_class = Libobjc.objc_getClass("WKWebsiteDataStore");
    private static readonly IntPtr s_httpCookieStore = Libobjc.sel_getUid("httpCookieStore");
    private static readonly IntPtr s_defaultDataStore = Libobjc.sel_getUid("defaultDataStore");
    private static readonly IntPtr s_nonPersistentDataStore = Libobjc.sel_getUid("nonPersistentDataStore");
    private static readonly IntPtr s_dataStoreForIdentifier = Libobjc.sel_getUid("dataStoreForIdentifier:");
    private static readonly IntPtr s_setProxyConfigurations = Libobjc.sel_getUid("setProxyConfigurations:");

    public WKHTTPCookieStore HttpCookieStore => new(Libobjc.intptr_objc_msgSend(Handle, s_httpCookieStore), false);

    public static WKWebsiteDataStore Default =>
        new(Libobjc.intptr_objc_msgSend(s_class, s_defaultDataStore), false);
    public static WKWebsiteDataStore NonPersistent =>
        new(Libobjc.intptr_objc_msgSend(s_class, s_nonPersistentDataStore), false);
    public static WKWebsiteDataStore ForIdentifier(Guid identifier)
    {
        using var nsIdentifier = NSUUID.Create(identifier);
        return new WKWebsiteDataStore(
            Libobjc.intptr_objc_msgSend(s_class, s_dataStoreForIdentifier, nsIdentifier.Handle), true);
    }

    public void SetProxyConfiguration(Uri proxyAddress, string[] excludedDomains, NetworkCredential? credentials = null)
    {
        if (proxyAddress.Port <= 0)
            throw new ArgumentException(
                $"Proxy address '{proxyAddress}' must specify an explicit port.", nameof(proxyAddress));

        var endpoint = NetworkFramework.nw_endpoint_create_host(proxyAddress.Host, proxyAddress.Port.ToString());

        try
        {
            IntPtr proxyConfig;
            IntPtr? tlsOptions = null;

            switch (proxyAddress.Scheme)
            {
                case "http":
                    proxyConfig = NetworkFramework.nw_proxy_config_create_http_connect(endpoint, IntPtr.Zero);
                    break;

                case "https":
                    tlsOptions = NetworkFramework.nw_tls_create_options();
                    proxyConfig = NetworkFramework.nw_proxy_config_create_http_connect(endpoint, tlsOptions.Value);
                    break;

                case "socks5":
                    proxyConfig = NetworkFramework.nw_proxy_config_create_socksv5(endpoint);
                    break;

                default:
                    throw new NotSupportedException($"Proxy scheme '{proxyAddress.Scheme}' is not supported.");
            }

            try
            {
                var (username, password) = credentials is not null ?
                    (credentials.UserName, credentials.Password) :
                    ParseUserInfo(proxyAddress.UserInfo);

                if (!string.IsNullOrEmpty(username))
                    NetworkFramework.nw_proxy_config_set_username(proxyConfig, username);

                if (!string.IsNullOrEmpty(password))
                    NetworkFramework.nw_proxy_config_set_password(proxyConfig, password);

                foreach (var excludedDomain in excludedDomains.Where(d => !string.IsNullOrWhiteSpace(d)))
                {
                    NetworkFramework.nw_proxy_config_add_excluded_domain(proxyConfig, excludedDomain);
                }

                using var array = NSArray.FromObject(proxyConfig);
                _ = Libobjc.intptr_objc_msgSend(Handle, s_setProxyConfigurations, array.Handle);
            }
            finally
            {
                NetworkFramework.nw_release(proxyConfig);

                if (tlsOptions.HasValue)
                    NetworkFramework.nw_release(tlsOptions.Value);
            }
        }
        finally
        {
            NetworkFramework.nw_release(endpoint);
        }

        static (string? Username, string? Password) ParseUserInfo(string userInfo)
        {
            if (string.IsNullOrEmpty(userInfo))
                return (null, null);

            var colonIndex = userInfo.IndexOf(':');
            if (colonIndex < 0)
                return (Uri.UnescapeDataString(userInfo), null);

            var username = Uri.UnescapeDataString(userInfo.AsSpan(0, colonIndex));
            var password = Uri.UnescapeDataString(userInfo.AsSpan(colonIndex + 1));
            return (username, password);
        }
    }
}
