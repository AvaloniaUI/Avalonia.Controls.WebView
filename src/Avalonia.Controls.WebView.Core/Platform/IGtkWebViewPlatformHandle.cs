using System;

// ReSharper disable once CheckNamespace
namespace Avalonia.Platform;

public interface IGtkWebViewPlatformHandle : IPlatformHandle
{
    /// <summary>
    /// Returns a handle of the WebKitWebView GTK object.
    /// See https://webkitgtk.org/reference/webkit2gtk/2.5.1/WebKitWebView.html.
    /// </summary>
    IntPtr WebKitWebView { get; }
}
