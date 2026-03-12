using Avalonia.Controls;

// ReSharper disable once CheckNamespace
namespace Avalonia.Platform;

internal class BrowserWebViewEnvironmentRequestedEventArgs : WebViewEnvironmentRequestedEventArgs
{
    internal BrowserWebViewEnvironmentRequestedEventArgs(DeferralManager deferralManager) : base(deferralManager)
    {
    }
}
