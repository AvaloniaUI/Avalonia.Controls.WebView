using System;
using System.Linq;
using AvaloniaUI.Licensing;

namespace Avalonia.Controls;

internal static class Licensing
{
    private const string WebViewProductName = "Avalonia.Controls.WebView";
    private static AvaloniaLicenseInformation? s_cachedLicense;

    public static void ValidateWebView()
    {
        // TODO: RuntimeConfig is broken there
        if (OperatingSystemEx.IsBrowser())
            return;

        var license = s_cachedLicense ??= AvaloniaLicenseInformation.LoadProduct("Avalonia.Controls.WebView").FirstOrDefault()
            ?? throw new AvaloniaLicensingException($"Missing AvaloniaUILicenseKey with {WebViewProductName} product included.");
        license.ValidateLibrary(
            WebViewProductName,
            buildTime: DateTimeOffset.FromUnixTimeSeconds(AvnLicensingConstants.BuildTimeUnixTimestamp));
    }
}
