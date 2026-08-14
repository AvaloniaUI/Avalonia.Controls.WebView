using System;

namespace Avalonia.Controls;

internal static class WebViewHelper
{
    internal const string PostAvWebViewMessageName = "postAvWebViewMessage";

    public static Uri EmptyPage { get; } = new("about:blank");

    internal static string BuildWebKitInvokeCSharpActionScript(
        string messageName = PostAvWebViewMessageName, bool stringify = true) =>
        BuildInvokeCSharpActionScript("window.webkit.messageHandlers." + messageName, stringify: stringify);

    /// <param name="postObject">Target object to send message to.</param>
    /// <param name="postMethod">Method on the <paramref name="postObject"/> that should be invoked to pass the message.</param>
    /// <param name="stringify">
    /// Defines if post data should be JSON serialized,
    /// some backends do that automatically when marshall objects to the C# handlers.
    /// </param>
    internal static string BuildInvokeCSharpActionScript(string postObject,
        string postMethod = "postMessage", bool stringify = true)
    {
        return stringify ?
            "function invokeCSharpAction(data){" +
            "var message = typeof data === 'object' ? JSON.stringify(data) : data;" +
            $"{postObject}.{postMethod}(message);" +
            "}" :
            "function invokeCSharpAction(data){" +
            $"{postObject}.{postMethod}(data);" +
            "}";
    }

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
