using System;

// ReSharper disable once CheckNamespace
namespace Avalonia.Platform;

public interface IWindowsWebView2PlatformHandle : IPlatformHandle
{
    /// <summary>
    /// Returns COM handle to the ICoreWebView2 [76ECEACB-0462-4D94-AC83-423A6793775E] COM interface.
    /// </summary>
    IntPtr CoreWebView2 { get; }

    /// <summary>
    /// Returns COM handle to the ICoreWebView2 [4D00C0D1-9434-4EB6-8078-8697A560334F] COM interface.
    /// </summary>
    IntPtr CoreWebView2Controller { get; }
}
