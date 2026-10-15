using System;
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
    /// With <see cref="WpeApi.Legacy"/>, <see cref="WpeRenderingMode.Auto"/> uses SHM.
    /// With <see cref="WpeApi.Platform"/>, <see cref="WpeRenderingMode.Auto"/> lets WPE WebKit use SHM or DMA-BUF buffers,
    /// and <see cref="WpeRenderingMode.Shm"/> uses SHM buffers only.
    /// <see cref="WpeRenderingMode.Egl"/> and <see cref="WpeRenderingMode.DmaBuf"/> are not supported
    /// and will result in an error if selected.
    /// Note: this choice is process-global and affects all WebView instances.
    /// </summary>
    public WpeRenderingMode RenderingMode { get; set; } = WpeRenderingMode.Auto;

    /// <summary>
    /// Gets or sets the WPE WebKit embedding API.
    /// The first WPE web view created in the process fixes the API for the rest of the process.
    /// Requesting a different API, or an API that is not available, throws <see cref="InvalidOperationException"/>
    /// when the web view is created.
    /// </summary>
    public WpeApi Api { get; set; } = WpeApi.Auto;

    /// <summary>
    /// Gets or sets a value indicating whether to prefer WebKitGTK instead of WPE WebKit.
    /// When set to true, the GTK-based WebView adapter will be used even if WPE is available.
    /// </summary>
    [Obsolete("Please use NativeWebView.AdapterPreference or NativeWebDialog.AdapterPreference instead.")]
    public bool PreferWebKitGtkInstead { get; set; }
}
