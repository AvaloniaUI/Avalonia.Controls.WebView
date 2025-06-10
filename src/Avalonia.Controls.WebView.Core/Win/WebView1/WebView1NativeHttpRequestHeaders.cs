using System.Runtime.Versioning;
using Avalonia.Controls.Utils;
using Avalonia.Controls.Win.WebView1.Interop;
using Avalonia.Logging;

namespace Avalonia.Controls.Win.WebView1;

[SupportedOSPlatform("windows")]
internal class WebView1NativeHttpRequestHeaders(
    IHttpRequestHeaderCollection requestHeader) : INativeHttpRequestHeaders
{
    public bool Immutable => false;
    public bool TryClear() => false;
    public bool TryGetCount(out int count)
    {
        count = 0;
        return false;
    }

    public string? GetHeader(string name)
    {
        LogWarning();
        return null;
    }

    public bool Contains(string name)
    {
        LogWarning();
        return false;
    }

    public bool TrySetHeader(string name, string value)
    {
        using var hName = new HStringInterop(name);
        using var hValue = new HStringInterop(value);
        requestHeader.TryAppendWithoutValidation(hName.Handle, hValue.Handle);
        return true;
    }

    public bool TryRemoveHeader(string name)
    {
        LogWarning();
        return false;
    }

    public INativeHttpHeadersCollectionIterator GetIterator()
    {
        LogWarning();
        return new Iterator();
    }

    private void LogWarning()
    {
        Logger.TryGet(LogEventLevel.Warning, "WebView")?
            .Log(null,
                "WebView1 adapter doesn't currently support reading request headers." +
                "Let us know if this feature is important for you.");
    }

    private class Iterator : INativeHttpHeadersCollectionIterator
    {
        public void GetCurrentHeader(out string name, out string value)
        {
            name = "";
            value = "";
        }

        public bool GetHasCurrentHeader() => false;
        public bool MoveNext() => false;
    }
}
