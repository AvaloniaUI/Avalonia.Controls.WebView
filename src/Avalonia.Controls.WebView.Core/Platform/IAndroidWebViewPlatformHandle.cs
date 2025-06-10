using System;

// ReSharper disable once CheckNamespace
namespace Avalonia.Platform;

public interface IAndroidWebViewPlatformHandle : IPlatformHandle
{
    /// <summary>
    /// Returns handle of the Android.Webkit.WebView Android object.
    /// </summary>
    IntPtr WebKitWebView { get; }
}
