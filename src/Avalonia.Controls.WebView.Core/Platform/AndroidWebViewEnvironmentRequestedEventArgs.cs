using Avalonia.Controls;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Avalonia.Platform;

public sealed class AndroidWebViewEnvironmentRequestedEventArgs : WebViewEnvironmentRequestedEventArgs
{
    public bool BuiltInZoomControls { get; set; } = true;
}
