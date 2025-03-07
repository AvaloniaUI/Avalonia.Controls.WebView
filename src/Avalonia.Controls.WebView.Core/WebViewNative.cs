namespace Avalonia.Controls;

public class WebViewOptions
{
    /// <remark>
    /// Currently only supported on macOS.
    /// Might block application from being uploaded to the AppStore.
    /// </remark>
    public bool EnableDevTools { get; set; }
}
