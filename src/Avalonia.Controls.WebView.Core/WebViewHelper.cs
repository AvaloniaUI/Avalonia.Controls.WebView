using System;

namespace Avalonia.Controls;

internal class WebViewHelper
{
    public static Uri EmptyPage { get; } = new("about:blank");

    private static bool? s_isMsWebView2Available;
    public static bool IsMsWebView2Available => s_isMsWebView2Available ??= IsMsWebView2AvailableInternal();

    public static bool IsMsWebView1Available =>
        false; // OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17134);

    private static bool IsMsWebView2AvailableInternal()
    {
        if (!OperatingSystemEx.IsWindows())
        {
            return false;
        }

#if !NETSTANDARD2_0
        try
        {
            var versionString = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
            return !string.IsNullOrWhiteSpace(versionString);
        }
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            return false;
        }
#else
        return false;
#endif
    }
}
