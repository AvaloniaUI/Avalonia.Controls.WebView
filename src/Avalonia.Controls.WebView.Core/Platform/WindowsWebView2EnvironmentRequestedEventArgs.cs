using System;
using Avalonia.Controls;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Avalonia.Platform;

public sealed class WindowsWebView2EnvironmentRequestedEventArgs : WebViewEnvironmentRequestedEventArgs
{
    internal WindowsWebView2EnvironmentRequestedEventArgs(DeferralManager deferralManager) : base(deferralManager)
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether to prefer WebView1 instead of WebView2.
    /// </summary>
    public bool PreferWebView1Instead { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable offscreen composition mode.
    /// WebView will render into an offscreen buffer before presenting to the screen.
    /// It allows to avoid airspace issues.
    /// </summary>
    /// <remarks>
    /// This mode is powered by ICoreWebView2CompositionController.
    /// </remarks>
    public bool ExperimentalOffscreen { get; set; }

    /// <summary>
    /// Gets or sets an existing ICoreWebView2Environment COM reference handle that the webview adapter will use instead of managing its own.
    /// </summary>
    public IntPtr ExplicitEnvironment { get; set; }

    /// <summary>
    /// Determines whether to enable single sign on with Azure Active Directory (AAD) resources inside WebView using the logged in Windows account and single sign on (SSO) with web sites using Microsoft account associated with the login in Windows account.
    /// </summary>
    public bool AllowSingleSignOnUsingOSPrimaryAccount { get; set; }

    /// <summary>
    /// Gets or sets the profile name, which must contain only allowed ASCII characters.
    /// </summary>
    public string? ProfileName { get; set; }

    /// <summary>
    /// Gets or sets the value to pass as the browserExecutableFolder parameter of CreateAsync(String, String, CoreWebView2EnvironmentOptions) when creating an environment with this instance.
    /// </summary>
    public string? BrowserExecutableFolder { get; set; }

    /// <summary>
    /// Gets or sets the value to pass as the userDataFolder parameter of CreateAsync(String, String, CoreWebView2EnvironmentOptions) when creating an environment with this instance.
    /// </summary>
    public string? UserDataFolder { get; set; }

    /// <summary>
    /// Gets or sets the additional browser arguments to use for the CoreWebView2EnvironmentOptions parameter passed to CreateAsync(String, String, CoreWebView2EnvironmentOptions) when creating an environment with this instance.
    /// </summary>
    /// <remarks>
    /// The arguments are passed to the browser process as part of the command. For more information about using command-line switches with Chromium browser processes, see https://www.chromium.org/developers/how-tos/run-chromium-with-flags/.
    /// </remarks>
    public string? AdditionalBrowserArguments { get; set; }

    /// <summary>
    /// Gets or sets the default display language for WebView.
    /// </summary>
    /// <remarks>
    /// Applies to browser UI such as context menus and dialogs, and to the accept-languages HTTP header sent to websites. The locale value should be in the format of BCP 47 Language Tags.
    /// </remarks>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether InPrivate mode is enabled.
    /// </summary>
    public bool IsInPrivateModeEnabled { get; set; }
}
