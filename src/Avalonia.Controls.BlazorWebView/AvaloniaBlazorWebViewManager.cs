using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.FileProviders;
using AvControls = Avalonia.Controls;

namespace Avalonia.Controls.BlazorWebView;

/// <summary>
/// <see cref="WebViewManager"/> implementation over Avalonia <see cref="AvControls.NativeWebView"/>.
/// </summary>
internal sealed class AvaloniaBlazorWebViewManager : WebViewManager
{
    // On Windows, WebView2 cannot navigate top-level to a custom scheme.
    // On WebKit platforms, http interception is unavailable - use app://.
    internal static readonly string BlazorAppScheme = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "http"
        : "app";

    internal static readonly Uri AppBaseUri = new($"{BlazorAppScheme}://0.0.0.0/");

    private readonly AvControls.NativeWebView _webView;
    private readonly Uri _appBaseUri;

    public AvaloniaBlazorWebViewManager(
        AvControls.NativeWebView webView,
        IServiceProvider provider,
        Dispatcher dispatcher,
        Uri appBaseUri,
        IFileProvider fileProvider,
        JSComponentConfigurationStore jsComponents,
        string hostPageRelativePath)
        : base(provider, dispatcher, appBaseUri, fileProvider, jsComponents, hostPageRelativePath)
    {
        _webView = webView;
        _appBaseUri = appBaseUri;

        _webView.WebMessageReceived += OnWebMessageReceived;
        _webView.WebResourceRequested += OnWebResourceRequested;
    }

    protected override void NavigateCore(Uri absoluteUri) => _webView.Navigate(absoluteUri);

    protected override void SendMessage(string message)
    {
        // Deliver into Blazor's window.external.receiveMessage callback(s).
        var payload = EscapeJsString(message);
        _ = _webView.InvokeScript(
            $"(function(){{var cbs=window.__blazorExternalMessageCallbacks;if(cbs){{for(var i=0;i<cbs.length;i++){{cbs[i]({payload});}}}}}})()");
    }

    private static string EscapeJsString(string value)
    {
        var sb = new System.Text.StringBuilder(value.Length + 2);
        sb.Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\u2028': sb.Append("\\u2028"); break;
                case '\u2029': sb.Append("\\u2029"); break;
                default:
                    if (c < 0x20)
                        sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else
                        sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }

    private void OnWebMessageReceived(object? sender, AvControls.WebMessageReceivedEventArgs e)
    {
        if (e.Body is { } body)
            MessageReceived(_appBaseUri, body);
    }

    private void OnWebResourceRequested(object? sender, AvControls.WebResourceRequestedEventArgs e)
    {
        var url = e.Request.Uri.AbsoluteUri;
        if (!_appBaseUri.IsBaseOf(e.Request.Uri))
            return;

        var hasFileExtension = url.LastIndexOf('.') > url.LastIndexOf('/');
        if (TryGetResponseContent(
                url,
                !hasFileExtension,
                out var statusCode,
                out var statusMessage,
                out var content,
                out var headers))
        {
            var contentType = GetContentType(headers);
            if (contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                content = InjectExternalBridgePolyfill(content);
                headers = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
                headers.Remove("Content-Length");
            }

            e.SetResponse(content, statusCode, statusMessage, contentType,
                (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(headers));
        }
    }

    private static Stream InjectExternalBridgePolyfill(Stream content)
    {
        using var reader = new StreamReader(content);
        var html = reader.ReadToEnd();
        const string polyfill =
            "<script>(function(){" +
            "window.__blazorExternalMessageCallbacks=window.__blazorExternalMessageCallbacks||[];" +
            "window.external=window.external||{};" +
            "window.external.sendMessage=window.external.sendMessage||function(m){if(window.invokeCSharpAction)window.invokeCSharpAction(typeof m==='string'?m:JSON.stringify(m));};" +
            "window.external.receiveMessage=window.external.receiveMessage||function(cb){window.__blazorExternalMessageCallbacks.push(cb);};" +
            "window.chrome=window.chrome||{};window.chrome.webview=window.chrome.webview||{};" +
            "window.chrome.webview.postMessage=window.chrome.webview.postMessage||function(m){window.external.sendMessage(m);};" +
            "window.chrome.webview.addEventListener=window.chrome.webview.addEventListener||function(t,h){if(t==='message'){window.external.receiveMessage(function(d){h({data:d});});}};" +
            "})();</script>";

        if (!html.Contains("window.external.sendMessage", StringComparison.Ordinal))
        {
            var idx = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
            html = idx >= 0
                ? html.Insert(idx + 6, polyfill)
                : polyfill + html;
        }

        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(html));
    }

    private static string GetContentType(IDictionary<string, string> headers)
    {
        foreach (var pair in headers)
        {
            if (pair.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                return pair.Value;
        }
        return "application/octet-stream";
    }

    public async ValueTask DisposeManagedAsync()
    {
        _webView.WebMessageReceived -= OnWebMessageReceived;
        _webView.WebResourceRequested -= OnWebResourceRequested;
        await DisposeAsync().ConfigureAwait(false);
    }
}
