using System;

// ReSharper disable once CheckNamespace
namespace Avalonia.Platform;

public interface IWindowsWebView1PlatformHandle : IPlatformHandle
{
    /// <summary>
    /// Returns COM handle to the IWebViewControl [3F921316-BC70-4BDA-9136-C94370899FAB] COM interface.
    /// </summary>
    IntPtr WebViewControl { get; }
}
