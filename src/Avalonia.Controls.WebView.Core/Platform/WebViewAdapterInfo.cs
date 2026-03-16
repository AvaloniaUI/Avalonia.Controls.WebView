using System;

// ReSharper disable once CheckNamespace
namespace Avalonia.Platform;

/// <summary>
/// Represents the type of WebView adapter available on the system.
/// </summary>
public enum WebViewAdapterType
{
    /// <summary>
    /// Unknown or unsupported adapter.
    /// </summary>
    Unknown = 0,
    
    /// <summary>
    /// Microsoft Edge WebView2.
    /// </summary>
    /// <remarks>
    /// Comes preinstalled on Windows 11 and is available for Windows 7 and newer via separate installation.
    /// </remarks>
    WebView2 = 1,

    /// <summary>
    /// Legacy WebView control based on EdgeHTML.
    /// </summary>
    /// <remarks>
    /// Available on Windows 10 but deprecated in favor of WebView2.
    /// </remarks>
    WebView1,

    /// <summary>
    /// Apple WebKit WebView.
    /// </summary>
    /// <remarks>
    /// Available on macOS and iOS platforms.
    /// </remarks>
    WkWebView,

    /// <summary>
    /// GTK WebKit WebView.
    /// </summary>
    WebKitGtk,

    /// <summary>
    /// Android WebView.
    /// </summary>
    AndroidWebView,

    /// <summary>
    /// Browser iframe-based WebView for WASM platforms.
    /// </summary>
    BrowserIFrame,

    /// <summary>
    /// WPE WebKit WebView for Linux platforms.
    /// </summary>
    WpeWebKit,

    /// <summary>
    /// Headless WebView for testing scenarios.
    /// </summary>
    Headless = int.MaxValue
}

/// <summary>
/// Represents the embedding scenarios supported by a WebView adapter.
/// </summary>
[Flags]
public enum WebViewEmbeddingScenario
{
    /// <summary>
    /// No embedding scenarios are supported.
    /// </summary>
    None = 0,

    /// <summary>
    /// Embedding via native control hosting (NativeControlHost).
    /// Uses native window parenting (HWND on Windows, GtkWidget on Linux, etc.).
    /// </summary>
    NativeControlHost = 1 << 0,

    /// <summary>
    /// Embedding via offscreen rendering with compositor.
    /// Renders to a bitmap buffer for software composition.
    /// </summary>
    OffscreenRenderer = 1 << 1,

    /// <summary>
    /// Standalone native dialog window containing the WebView.
    /// Does not include scenarios, where WebView is hosted inside Avalonia window.
    /// </summary>
    NativeDialog = 1 << 2
}

/// <summary>
/// Represents the underlying web rendering engine used by a WebView adapter.
/// </summary>
public enum WebViewEngine
{
    /// <summary>
    /// Unknown or unsupported engine.
    /// </summary>
    Unknown,

    /// <summary>
    /// WebKit engine.
    /// </summary>
    WebKit = 1,

    /// <summary>
    /// Chromium Blink engine.
    /// </summary>
    Blink,

    /// <summary>
    /// EdgeHTML engine.
    /// </summary>
    EdgeHtml
}

/// <summary>
/// Detailed information about a WebView adapter.
/// </summary>
/// <param name="Type">The adapter type.</param>
/// <param name="Engine">The underlying web rendering engine.</param>
/// <param name="IsSupported">Whether this adapter type is supported on the current platform.</param>
/// <param name="IsInstalled">Whether the adapter runtime/dependencies are installed and usable.</param>
/// <param name="Version">The version of the adapter runtime, if available.</param>
/// <param name="UnavailableReason">The reason the adapter is unavailable, if applicable.</param>
/// <param name="SupportedScenarios">The embedding scenarios supported by this adapter.</param>
public record DetailedWebViewAdapterInfo(
    WebViewAdapterType Type,
    WebViewEngine Engine,
    string? Version,
    bool IsSupported,
    bool IsInstalled,
    string? UnavailableReason,
    WebViewEmbeddingScenario SupportedScenarios) : WebViewAdapterInfo(Type, Engine, Version);

/// <summary>
/// Information about a WebView adapter.
/// </summary>
/// <param name="Type">The adapter type.</param>
/// <param name="Engine">The underlying web rendering engine.</param>
/// <param name="Version">The version of the adapter runtime, if available.</param>
public record WebViewAdapterInfo(
    WebViewAdapterType Type,
    WebViewEngine Engine,
    string? Version)
{
    /// <summary>
    /// Gets detailed availability information for a specific adapter.
    /// </summary>
    /// <param name="adapterType">The adapter type to check.</param>
    /// <returns>Detailed information about the adapter's availability.</returns>
#pragma warning disable CA1416
    public static DetailedWebViewAdapterInfo GetAdapterInfo(WebViewAdapterType adapterType)
    {
        return adapterType switch
        {
            WebViewAdapterType.WebView2 => Controls.Win.WebView2.WebView2BaseAdapter.GetWebView2Info(null),
            WebViewAdapterType.WebView1 => Controls.Win.WebView1.WebView1Adapter.GetWebView1Info(),
            WebViewAdapterType.WkWebView => Controls.Macios.MaciosWebViewAdapter.GetWkWebViewInfo(),
            WebViewAdapterType.WebKitGtk => Controls.Gtk.GtkWebViewAdapter.GetWebKitGtkInfo(),
            WebViewAdapterType.AndroidWebView =>
#if ANDROID
                Controls.Android.AndroidWebViewAdapter.GetAndroidWebViewInfo(),
#else
                PlatformNotSupported(adapterType),
#endif
            WebViewAdapterType.BrowserIFrame =>
#if BROWSER
                Controls.Browser.BrowserIFrameAdapter.GetBrowserInfo(),
#else
                PlatformNotSupported(adapterType),
#endif
            WebViewAdapterType.WpeWebKit => Controls.Linux.WpeWebViewAdapter.GetWpeInfo(),
            WebViewAdapterType.Headless => Controls.Headless.HeadlessWebViewAdapter.GetHeadlessInfo(),
            _ => UnknownAdapter(adapterType)
        };
    }
#pragma warning restore CA1416

    internal static DetailedWebViewAdapterInfo PlatformNotSupported(WebViewAdapterType? type = null) => new(
        type ?? WebViewAdapterType.Unknown,
        WebViewEngine.Unknown,
        IsSupported: false,
        IsInstalled: false,
        Version: null,
        UnavailableReason: "The adapter is not supported on the current platform.",
        SupportedScenarios: WebViewEmbeddingScenario.None);

    private static DetailedWebViewAdapterInfo UnknownAdapter(WebViewAdapterType type) => new(
        type,
        WebViewEngine.Unknown,
        IsSupported: false,
        IsInstalled: false,
        Version: null,
        UnavailableReason: "Unknown adapter type.",
        SupportedScenarios: WebViewEmbeddingScenario.None);
}
