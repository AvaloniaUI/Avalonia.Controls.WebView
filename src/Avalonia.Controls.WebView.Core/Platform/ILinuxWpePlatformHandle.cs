using System;

// ReSharper disable once CheckNamespace
namespace Avalonia.Platform;

public interface ILinuxWpePlatformHandle : IPlatformHandle
{
    /// <summary>
    /// Returns a pointer to the WebKitWebView GObject instance.
    /// </summary>
    IntPtr WebKitWebView { get; }

    /// <summary>
    /// Returns a pointer to the wpe_view_backend native struct.
    /// Returns <see cref="IntPtr.Zero"/> when the web view uses <see cref="WpeApi.Platform"/>.
    /// </summary>
    IntPtr WpeViewBackend { get; }

    /// <summary>
    /// Returns a pointer to the WPEView GObject instance.
    /// Returns <see cref="IntPtr.Zero"/> when the web view uses <see cref="WpeApi.Legacy"/>.
    /// </summary>
    IntPtr WpeView { get; }
}
