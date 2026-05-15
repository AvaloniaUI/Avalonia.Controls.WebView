using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.Controls;

public sealed class NativeWebViewCookieManager
{
    private readonly IWebViewAdapterWithCookieManager _webView;

    internal NativeWebViewCookieManager(IWebViewAdapterWithCookieManager webView)
    {
        _webView = webView;
    }

    public void AddOrUpdateCookie(System.Net.Cookie cookie) => _webView.AddOrUpdateCookie(cookie);
    [System.Obsolete("Use DeleteCookie(System.Net.Cookie cookie) instead")]
    public void DeleteCookie(string name, string domain, string path) => _webView.DeleteCookie(name, domain, path);
    public void DeleteCookie(System.Net.Cookie cookie) => _webView.DeleteCookie(cookie);
    public Task<IReadOnlyList<System.Net.Cookie>> GetCookiesAsync() => _webView.GetCookiesAsync();
}
