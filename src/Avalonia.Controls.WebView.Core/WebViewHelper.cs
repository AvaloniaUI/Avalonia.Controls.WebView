using System;

namespace Avalonia.Controls;

internal static class WebViewHelper
{
    public static Uri EmptyPage { get; } = new("about:blank");

    internal static bool IsAnchorNavigation(Uri? currentUrl, Uri? newUrl)
    {
        if (currentUrl is null || newUrl is null)
        {
            return false;
        }

        // Remove fragment for base comparison
        var currentBase = new Uri(currentUrl.GetLeftPart(UriPartial.Path));
        var newBase = new Uri(newUrl.GetLeftPart(UriPartial.Path));

        // Get fragment (anchor) parts, trimming leading '#'
        var currentAnchor = currentUrl.Fragment.Length > 1 ? currentUrl.Fragment.Substring(1) : string.Empty;
        var newAnchor = newUrl.Fragment.Length > 1 ? newUrl.Fragment.Substring(1) : string.Empty;

        // Check if this is anchor navigation:
        // 1. Both URLs should have the same base (before #)
        // 2. New URL should have an anchor part (after #)
        // 3. The anchor parts should be different (or one missing)
        return Uri.Compare(currentBase, newBase, UriComponents.AbsoluteUri, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0
            && !string.IsNullOrEmpty(newAnchor)
            && !string.Equals(currentAnchor, newAnchor, StringComparison.OrdinalIgnoreCase);
    }
}
