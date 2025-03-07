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
    public void DeleteCookie(string name, string domain, string path) => _webView.DeleteCookie(name, domain, path);
    public Task<IReadOnlyList<System.Net.Cookie>> GetCookiesAsync() => _webView.GetCookiesAsync();
}
