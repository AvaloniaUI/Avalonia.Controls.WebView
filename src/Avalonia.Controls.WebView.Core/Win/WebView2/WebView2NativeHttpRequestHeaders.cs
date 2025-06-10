using Avalonia.Controls.Utils;
using Avalonia.Controls.Win.WebView2.Interop;

namespace Avalonia.Controls.Win.WebView2;

internal class WebView2NativeHttpRequestHeaders(ICoreWebView2HttpRequestHeaders headers) : INativeHttpRequestHeaders
{
    public bool Immutable => false;
    public bool TryClear() => false;
    public bool TryGetCount(out int count)
    {
        count = 0;
        return false;
    }

    public string? GetHeader(string name) => headers.GetHeader(name, out var value) == 0 ? value : null;

    public bool Contains(string name) => headers.Contains(name);

    public bool TrySetHeader(string name, string value) => headers.SetHeader(name, value) == 0;

    public bool TryRemoveHeader(string name) => headers.RemoveHeader(name) == 0;

    public INativeHttpHeadersCollectionIterator GetIterator() => new Iterator(headers.GetIterator());

    private class Iterator(ICoreWebView2HttpHeadersCollectionIterator iterator) : INativeHttpHeadersCollectionIterator
    {
        public void GetCurrentHeader(out string name, out string value) => iterator.GetCurrentHeader(out name, out value);
        public bool GetHasCurrentHeader() => iterator.GetHasCurrentHeader();
        public bool MoveNext() => iterator.MoveNext();
    }
}
