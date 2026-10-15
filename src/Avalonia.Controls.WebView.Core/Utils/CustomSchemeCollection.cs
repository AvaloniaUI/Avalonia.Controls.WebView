using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Avalonia.Controls.Utils;

/// <summary>
/// Validates <see cref="WebViewEnvironmentRequestedEventArgs.CustomSchemes"/> as they're added,
/// so a bad scheme fails in the handler on every platform, including those that ignore the list.
/// </summary>
internal sealed class CustomSchemeCollection : Collection<string>
{
    // Schemes every engine handles itself.
    private static readonly HashSet<string> s_builtInSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeFile, Uri.UriSchemeFtp, Uri.UriSchemeWs, Uri.UriSchemeWss,
        "about", "data", "blob", "javascript"
    };

    protected override void InsertItem(int index, string item) => base.InsertItem(index, Validate(item));

    protected override void SetItem(int index, string item) => base.SetItem(index, Validate(item));

    private static string Validate(string scheme)
    {
        if (scheme is null)
            throw new ArgumentNullException(nameof(scheme));
        if (!Uri.CheckSchemeName(scheme))
            throw new ArgumentException($"'{scheme}' is not a valid URI scheme.", nameof(scheme));
        if (s_builtInSchemes.Contains(scheme))
            throw new ArgumentException($"'{scheme}' is a built-in scheme and can't be registered.", nameof(scheme));
        return scheme;
    }
}
