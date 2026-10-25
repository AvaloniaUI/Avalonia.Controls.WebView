namespace Avalonia.Platform;

/// <summary>
/// Selects the WPE WebKit embedding API used by the WPE WebKit adapter.
/// The first WPE web view created in the process fixes the API for the rest of the process.
/// </summary>
public enum WpeApi
{
    /// <summary>
    /// Uses <see cref="Platform"/> when WPE WebKit is 2.54 or later and exports the WPEPlatform API.
    /// Otherwise uses <see cref="Legacy"/>, then <see cref="Platform"/>, whichever is available.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Uses the legacy libwpe and WPEBackend-fdo API.
    /// </summary>
    Legacy,

    /// <summary>
    /// Uses the WPEPlatform API with a headless display.
    /// </summary>
    Platform,
}
