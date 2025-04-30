namespace Avalonia.Controls;

public class WebViewOptions
{
    /// <remark>
    /// Currently only supported on macOS.
    /// Might block application from being uploaded to the AppStore.
    /// </remark>
    public bool EnableDevTools { get; set; }

    /// <summary>
    /// Experimental support for GTK WebView that can be hosted in the same Avalonia window, without overlapping other controls.
    /// </summary>
    public bool ExperimentalGtkOffscreen { get; set; }
}
