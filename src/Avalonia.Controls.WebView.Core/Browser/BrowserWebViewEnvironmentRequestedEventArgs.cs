using Avalonia.Controls;

// ReSharper disable once CheckNamespace
namespace Avalonia.Platform;

internal class BrowserWebViewEnvironmentRequestedEventArgs : WebViewEnvironmentRequestedEventArgs
{
    internal BrowserWebViewEnvironmentRequestedEventArgs(DeferralManager deferralManager) : base(deferralManager)
    {
    }

    /// <summary>
    /// When enabled, injects a postMessage bridge script into same-origin pages after each navigation.
    /// This allows hosted pages to call <c>chrome.webview.postMessage(message)</c> to communicate with the adapter,
    /// matching the API used by other platform WebView adapters.
    /// Requires same-origin pages or permissive CORS policies; will silently fail on cross-origin pages.
    /// </summary>
    public bool EnablePostMessageBridge { get; set; } = true;

    /// <summary>
    /// Sets the <c>sandbox</c> attribute on the iframe element for security restrictions.
    /// When null (default), no sandbox attribute is set.
    /// Example: <c>"allow-scripts allow-same-origin allow-forms"</c>.
    /// See https://developer.mozilla.org/en-US/docs/Web/HTML/Element/iframe#sandbox
    /// </summary>
    public string? Sandbox { get; set; }
}
