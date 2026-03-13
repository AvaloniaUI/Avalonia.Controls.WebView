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
    /// The default (<see cref="WpeRenderingMode.Auto"/>) tries SHM first (no GPU required),
    /// then falls back to EGL/DMABuf.
    /// Note: this choice is process-global and affects all WebView instances.
    /// </summary>
    public WpeRenderingMode RenderingMode { get; set; } = WpeRenderingMode.Auto;
}
