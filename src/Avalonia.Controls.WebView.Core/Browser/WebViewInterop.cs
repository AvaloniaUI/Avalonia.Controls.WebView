#if !ANDROID && NET8_0_OR_GREATER 
using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace Avalonia.Controls.Browser;

[SupportedOSPlatform("browser")]
internal static partial class WebViewInterop
{
    private const string ModuleName = "av-webview";
    private static readonly Lazy<Task> s_import = new(() =>
    {
        // TODO: make AvaloniaModule.ImportModule public
        return JSHost.ImportAsync(ModuleName, "./av-webview.mjs");
    });
    public static Task EnsureLoaded() => s_import.Value;

    [JSImport("openAuthWindow", "av-webview")]
    [return: JSMarshalAs<JSType.Promise<JSType.String>>]
    public static partial Task<string?> OpenAuthWindow(string windowId, string url, string redirectUri);

    [JSImport("closeAuthWindow", "av-webview")]
    public static partial Task CloseAuthWindow(string windowId);

    [JSImport("createIframeElement", "av-webview")]
    public static partial Task<JSObject> CreateElement(string tagName);

    [JSImport("getActualLocation", "av-webview")]
    public static partial string? GetActualLocation(JSObject iframe);

    [JSImport("goBack", "av-webview")]
    public static partial bool GoBack(JSObject iframe);

    [JSImport("goForward", "av-webview")]
    public static partial bool GoForward(JSObject iframe);

    [JSImport("canGoBack", "av-webview")]
    public static partial bool CanGoBack(JSObject iframe);

    [JSImport("refresh", "av-webview")]
    public static partial bool Refresh(JSObject iframe);

    [JSImport("stop", "av-webview")]
    public static partial bool Stop(JSObject iframe);

    [JSImport("subscribe", "av-webview")]
    [return: JSMarshalAs<JSType.Function>]
    public static partial Action Subscribe(
        JSObject iframe,
        [JSMarshalAs<JSType.Function<JSType.String>>]
        Action<string> onload);

    [JSImport("evalScript", "av-webview")]
    public static partial Task<string?> Eval(JSObject iframe, string script);
}
#endif
