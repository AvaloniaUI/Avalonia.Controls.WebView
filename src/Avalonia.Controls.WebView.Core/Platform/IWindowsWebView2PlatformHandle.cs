using System;

// ReSharper disable once CheckNamespace
namespace Avalonia.Platform;

public interface IWindowsWebView2PlatformHandle : IPlatformHandle
{
    /// <summary>
    /// Returns COM handle to the ICoreWebView2 [76ECEACB-0462-4D94-AC83-423A6793775E] COM interface.
    /// </summary>
    /// <remarks>
    /// The reference count is not incremented, and the handle stays valid while the web view is alive.
    /// Call AddRef to keep it longer.
    /// </remarks>
    IntPtr CoreWebView2 { get; }

    /// <summary>
    /// Returns COM handle to the ICoreWebView2Controller [4D00C0D1-9434-4EB6-8078-8697A560334F] COM interface.
    /// </summary>
    /// <remarks>
    /// The reference count is not incremented, and the handle stays valid while the web view is alive.
    /// Call AddRef to keep it longer.
    /// </remarks>
    IntPtr CoreWebView2Controller { get; }
}
