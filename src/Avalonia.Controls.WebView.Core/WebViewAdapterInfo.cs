using System;
using Avalonia.Controls.Gtk;

namespace Avalonia.Controls;

/// <summary>
/// Represents the type of WebView adapter available on the system.
/// </summary>
public enum WebViewAdapterType
{
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

#if DEBUG
    /// <summary>
    /// Headless WebView for testing scenarios.
    /// </summary>
    Headless = int.MaxValue
#endif
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
public record WebViewAdapterInfo(
    WebViewAdapterType Type,
    WebViewEngine Engine,
    bool IsSupported,
    bool IsInstalled,
    string? Version,
    string? UnavailableReason,
    WebViewEmbeddingScenario SupportedScenarios)
{
    /// <summary>
    /// Gets detailed availability information for a specific adapter.
    /// </summary>
    /// <param name="adapterType">The adapter type to check.</param>
    /// <returns>Detailed information about the adapter's availability.</returns>
    public static WebViewAdapterInfo GetAdapterInfo(WebViewAdapterType adapterType)
    {
        return adapterType switch
        {
            WebViewAdapterType.WebView2 => GetWebView2Info(null),
            WebViewAdapterType.WebView1 => GetWebView1Info(),
            WebViewAdapterType.WkWebView => GetWkWebViewInfo(),
            WebViewAdapterType.WebKitGtk => GetWebKitGtkInfo(),
            WebViewAdapterType.AndroidWebView => GetAndroidWebViewInfo(),
#if DEBUG
            WebViewAdapterType.Headless => new WebViewAdapterInfo(
                WebViewAdapterType.Headless,
                WebViewEngine.Unknown,
                IsSupported: true,
                IsInstalled: true,
                Version: null,
                UnavailableReason: null,
                SupportedScenarios: WebViewEmbeddingScenario.OffscreenRenderer),
#endif
            _ => UnknownAdapter(adapterType)
        };
    }

    private static WebViewAdapterInfo GetWebView2Info(string? browserExecutableFolder)
    {
        const WebViewEmbeddingScenario scenarios =
            //WebViewEmbeddingScenario.OffscreenRenderer |
            WebViewEmbeddingScenario.NativeControlHost;

        if (!OperatingSystemEx.IsWindows())
        {
            return PlatformNotSupported(WebViewAdapterType.WebView2);
        }

        var error = Win.WebView2.CoreWebView2Environment.TryFindWebView2Runtime(
            browserExecutableFolder, out var runtimeHandle, out var version);
        if (runtimeHandle == IntPtr.Zero && error is not null)
        {
            return new WebViewAdapterInfo(
                WebViewAdapterType.WebView2,
                WebViewEngine.Blink,
                IsSupported: true,
                IsInstalled: false,
                Version: null,
                UnavailableReason: error,
                SupportedScenarios: scenarios);
        }

        return new WebViewAdapterInfo(
            WebViewAdapterType.WebView2,
            WebViewEngine.Blink,
            IsSupported: true,
            IsInstalled: true,
            Version: version,
            UnavailableReason: null,
            SupportedScenarios: scenarios);
    }

    private static WebViewAdapterInfo GetWebView1Info()
    {
        const WebViewEmbeddingScenario scenarios = WebViewEmbeddingScenario.NativeControlHost;

        if (!OperatingSystemEx.IsWindows())
        {
            return PlatformNotSupported(WebViewAdapterType.WebView1);
        }

        // EdgeHtml is available in any Win10 version, but embeddable WebView1 control requires 10.0.17763+
        var isWindows10OrNewer = Environment.OSVersion.Version.Major >= 10 &&
                                 Environment.OSVersion.Version.Build >= 17763;
        if (!isWindows10OrNewer)
        {
            return new WebViewAdapterInfo(
                WebViewAdapterType.WebView1,
                WebViewEngine.EdgeHtml,
                IsSupported: false,
                IsInstalled: false,
                Version: null,
                UnavailableReason: "WebView1 requires Windows 10 or later.",
                SupportedScenarios: scenarios);
        }

        // WebView1 is available on Windows 10+ but deprecated
        return new WebViewAdapterInfo(
            WebViewAdapterType.WebView1,
            WebViewEngine.EdgeHtml,
            IsSupported: true,
            IsInstalled: true,
            Version: null,
            UnavailableReason: null,
            SupportedScenarios: scenarios);
    }

    private static WebViewAdapterInfo GetWkWebViewInfo()
    {
        const WebViewEmbeddingScenario scenarios =
            WebViewEmbeddingScenario.NativeControlHost;

        if (!OperatingSystemEx.IsMacOS() && !OperatingSystemEx.IsIOS())
        {
            return PlatformNotSupported(WebViewAdapterType.WkWebView);
        }

        var isAvailable = OperatingSystemEx.IsMacOSVersionAtLeast(10, 10) ||
                          OperatingSystemEx.IsIOSVersionAtLeast(8, 0);

        return new WebViewAdapterInfo(
            WebViewAdapterType.WkWebView,
            WebViewEngine.WebKit,
            IsSupported: isAvailable,
            IsInstalled: isAvailable,
            Version: null,
            UnavailableReason: isAvailable ? null : "WKWebView requires macOS 10.10+ or iOS 8.0+.",
            SupportedScenarios: isAvailable ? scenarios : WebViewEmbeddingScenario.None);
    }

    private static WebViewAdapterInfo GetWebKitGtkInfo()
    {
        const WebViewEmbeddingScenario scenarios =
            //WebViewEmbeddingScenario.NativeControlHost |
            //WebViewEmbeddingScenario.OffscreenRenderer |
            WebViewEmbeddingScenario.NativeDialog;

        if (!OperatingSystemEx.IsLinux())
        {
            return PlatformNotSupported(WebViewAdapterType.WebKitGtk);
        }

        var version = AvaloniaGtk.TryGetVersion();

        return new WebViewAdapterInfo(
            WebViewAdapterType.WebKitGtk,
            WebViewEngine.WebKit,
            IsSupported: true,
            IsInstalled: version is not null,
            Version: version?.ToString(),
            UnavailableReason: version is not null ? null : "WebKitGtk library is not installed. Install webkit2gtk 4.0+ package.",
            SupportedScenarios: version is not null ? scenarios : WebViewEmbeddingScenario.None);
    }

    private static WebViewAdapterInfo GetAndroidWebViewInfo()
    {
#if !ANDROID
        return PlatformNotSupported(WebViewAdapterType.AndroidWebView);
#else
        if (!OperatingSystem.IsAndroid())
        {
            return PlatformNotSupported(WebViewAdapterType.AndroidWebView);
        }

        const WebViewEmbeddingScenario scenarios =
            WebViewEmbeddingScenario.NativeControlHost |
            WebViewEmbeddingScenario.NativeDialog;

        var (engine, version) = Android.AndroidWebViewAdapter.GetWebViewEngineInfo();
        return new WebViewAdapterInfo(
            WebViewAdapterType.AndroidWebView,
            engine,
            IsSupported: true,
            IsInstalled: true,
            Version: version,
            UnavailableReason: null,
            SupportedScenarios: scenarios);
#endif
    }

    private static WebViewAdapterInfo PlatformNotSupported(WebViewAdapterType type) => new(
        type,
        WebViewEngine.Unknown,
        IsSupported: false,
        IsInstalled: false,
        Version: null,
        UnavailableReason: "The adapter is not supported on the current platform.",
        SupportedScenarios: WebViewEmbeddingScenario.None);

    private static WebViewAdapterInfo UnknownAdapter(WebViewAdapterType type) => new(
        type,
        WebViewEngine.Unknown,
        IsSupported: false,
        IsInstalled: false,
        Version: null,
        UnavailableReason: "Unknown adapter type.",
        SupportedScenarios: WebViewEmbeddingScenario.None);
}
