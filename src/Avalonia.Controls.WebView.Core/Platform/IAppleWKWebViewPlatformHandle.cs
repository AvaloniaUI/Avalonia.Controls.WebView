using System;

// ReSharper disable once CheckNamespace
namespace Avalonia.Platform;

public interface IAppleWKWebViewPlatformHandle : IPlatformHandle
{
    /// <summary>
    /// Returns handle to the WKWebView obj-c object.
    /// </summary>
    IntPtr WKWebView { get; }

    /// <summary>
    /// Returns handle to the WKWebView obj-c object, incrementing reference count.
    /// </summary>
    IntPtr GetWKWebViewRetained();
}
