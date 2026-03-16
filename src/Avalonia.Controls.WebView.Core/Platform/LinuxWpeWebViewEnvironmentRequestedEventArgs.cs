using Avalonia.Controls;

// ReSharper disable once CheckNamespace
namespace Avalonia.Platform;

public sealed class LinuxWpeWebViewEnvironmentRequestedEventArgs : WebViewEnvironmentRequestedEventArgs
{
    internal LinuxWpeWebViewEnvironmentRequestedEventArgs(DeferralManager deferralManager) : base(deferralManager)
    {
    }

    /// <summary>
    /// Gets or sets the data directory for persistent website data.
    /// When null, the default WebKit data directory is used.
    /// </summary>
    public string? DataDirectory { get; set; }

    /// <summary>
    /// Gets or sets the cache directory for website cache data.
    /// When null, the default WebKit cache directory is used.
    /// </summary>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Gets or sets the rendering mode for WPE WebKit.
    /// The default (<see cref="WpeRenderingMode.Auto"/>) currently uses SHM only (no GPU required)
    /// with the WPE adapter.
    /// Modes that rely on EGL/DMABuf are not supported by the current WPE adapter implementation
    /// and will result in an error if selected.
    /// Note: this choice is process-global and affects all WebView instances.
    /// </summary>
    public WpeRenderingMode RenderingMode { get; set; } = WpeRenderingMode.Auto;

    /// <summary>
    /// Gets or sets a value indicating whether to prefer WebKitGTK instead of WPE WebKit.
    /// When set to true, the GTK-based WebView adapter will be used even if WPE is available.
    /// </summary>
    public bool PreferWebKitGtkInstead { get; set; }
}
