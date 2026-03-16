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
    /// </summary>
    IntPtr WpeViewBackend { get; }
}
