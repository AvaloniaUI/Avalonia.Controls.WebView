using Avalonia.Controls;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Avalonia.Platform;

public sealed class GtkWebViewEnvironmentRequestedEventArgs : WebViewEnvironmentRequestedEventArgs
{
    internal GtkWebViewEnvironmentRequestedEventArgs(DeferralManager deferralManager) : base(deferralManager)
    {
    }

    /// <summary>
    /// Gets or sets the application name to append to the default user agent string.
    /// </summary>
    public string? ApplicationNameForUserAgent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable experimental offscreen support for GTK WebView, allowing it to be hosted in the same Avalonia window without overlapping other controls.
    /// </summary>
    public bool ExperimentalOffscreen { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether GDK_BACKEND should be forced to "x11" while GTK is initialized.
    /// The GTK adapters require the x11 GDK backend, and a Wayland desktop usually pre-sets GDK_BACKEND=wayland, which makes gtk_init fail.
    /// </summary>
    public bool ForceX11GdkBackend { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the webview should use an ephemeral data manager, handling all website data as non-persistent and not writing anything to client storage.
    /// </summary>
    /// <remarks>
    /// If enabled, all other parameters to configure data directories will be ignored.
    /// </remarks>
    public bool EphemeralDataManager { get; set; }

    /// <summary>
    /// Gets or sets the base directory for website data, used as a base directory when no specific data directory is provided.
    /// </summary>
    public string? BaseDataDirectory { get; set; }

    /// <summary>
    /// Gets or sets the base directory for website cache, used as a base directory when no specific cache directory is provided.
    /// </summary>
    public string? BaseCacheDirectory { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use a single process for content rendering, shared among all WebKitWebView instances created by the application.
    /// </summary>
    public bool SharedProcessModel { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to disable the cache completely, substantially reducing memory usage.
    /// </summary>
    /// <remarks>
    /// Useful for applications that only access a single local file, with no navigation to other pages. No remote resources will be cached.
    /// Equivalent to WEBKIT_CACHE_MODEL_DOCUMENT_VIEWER. When disabled, WEBKIT_CACHE_MODEL_DOCUMENT_BROWSER is used.
    /// </remarks>
    public bool DisableCache { get; set; } = false;
}
