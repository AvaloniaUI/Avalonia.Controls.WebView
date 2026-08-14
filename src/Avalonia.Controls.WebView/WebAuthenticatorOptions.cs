using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Controls.Authentication;

#if AVALONIA
namespace Avalonia.Controls;
#elif WPF
namespace Avalonia.Xpf.Controls;
#endif

/// <summary>
/// Authentication options that control the broker's behavior.
/// </summary>
/// <param name="RequestUri">The initial URI that starts the authentication flow.</param>
/// <param name="RedirectUri">URI that indicates the completion of the authentication flow.</param>
public record WebAuthenticatorOptions(Uri RequestUri, Uri RedirectUri)
{
    /// <summary>
    /// Implementation used to run the flow.
    /// </summary>
    public WebAuthenticatorMode Mode { get; init; }

    /// <summary>
    /// If true, WebAuthenticationBroker will avoid platform specific implementation option, and will use webview dialog window.
    /// </summary>
    [Obsolete("Use Mode = WebAuthenticatorMode.NativeWebDialog instead.")]
    public bool PreferNativeWebDialog { get; init; }

    /// <summary>
    /// Hint for the platform implementation to not store any session data persistently.
    /// </summary>
    /// <remarks>
    /// Ignored by <see cref="WebAuthenticatorMode.Browser"/>, which uses the user's own browser session.
    /// </remarks>
    public bool NonPersistent { get; init; }

    /// <summary>
    /// Callback that can be used to override NativeWebDialog creation when WebAuthenticationBroker uses dialog implementation instead of system auth APIs.
    /// </summary>
    public Func<NativeWebDialog?>? NativeWebDialogFactory { get; init; }

    /// <summary>
    /// Options used when <see cref="Mode"/> is <see cref="WebAuthenticatorMode.Browser"/>.
    /// </summary>
    public BrowserOptions? BrowserOptions { get; init; }
}

/// <summary>
/// Selects which implementation <see cref="WebAuthenticationBroker"/> uses to run the flow.
/// </summary>
public enum WebAuthenticatorMode
{
    /// <summary>
    /// Automatically selects the most appropriate authentication mode for the current platform.
    /// </summary>
    Auto,

    /// <summary>
    /// Uses the platform's native web authentication APIs when available.
    /// </summary>
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [SupportedOSPlatform("browser")]
    System,

    /// <summary>
    /// Displays the authentication flow in a <see cref="NativeWebDialog"/> containing an embedded web view.
    /// </summary>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("android")]
    [SupportedOSPlatform("linux")]
    NativeWebDialog,

    /// <summary>
    /// Opens the authentication flow in the user's default web browser and uses a local HTTP listener to receive the redirect.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Requires <see cref="WebAuthenticatorOptions.RedirectUri"/> to be an <c>http</c> loopback address.
    /// </para>
    /// <para>
    /// The redirect is received on a local socket.
    /// Any process on the machine can connect to it, so <see cref="WebAuthenticationResult.CallbackUri"/> is untrusted input.
    /// The caller must validate its <c>code</c>, <c>state</c> and <c>error</c> parameters.
    /// </para>
    /// <para>
    /// PKCE is strongly recommended (RFC 8252, section 8.1).
    /// It is what makes an injected authorization code unusable at the token endpoint.
    /// Use <see cref="BrowserOptions.CallbackFilter"/> to keep the listener waiting when a request does not belong to the flow.
    /// </para>
    /// </remarks>
    [UnsupportedOSPlatform("browser")]
    Browser
}

/// <summary>
/// Options for <see cref="WebAuthenticatorMode.Browser"/>.
/// </summary>
public record BrowserOptions
{
    /// <summary>
    /// How long to wait for the callback before the flow is canceled.
    /// Defaults to 5 minutes.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets a callback used to customize the HTTP response sent to the browser after the authentication callback is received.
    /// </summary>
    /// <remarks>
    /// If not specified, a default response is sent to the browser.
    /// </remarks>
    public BrowserResponseHandler? ResponseHandler { get; init; }

    /// <summary>
    /// Gets or sets a callback that decides whether a request to the redirect path belongs to this authentication flow.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The local listener accepts connections from any process on the machine.
    /// A request that reaches the redirect path is therefore not necessarily the browser's.
    /// </para>
    /// <para>
    /// The usual implementation compares the <c>state</c> query parameter against the value sent in the authorization request.
    /// The caller still has to check <c>code</c>, <c>state</c> and <c>error</c> on the returned <see cref="WebAuthenticationResult.CallbackUri"/>.
    /// </para>
    /// </remarks>
    public BrowserCallbackFilter? CallbackFilter { get; init; }

    /// <summary>
    /// Handles the HTTP response sent to the browser after the authentication callback
    /// is received.
    /// </summary>
    /// <param name="result">
    /// The result of the authentication flow.
    /// </param>
    /// <param name="response">
    /// The response used to configure the HTTP response sent to the browser.
    /// </param>
    public delegate Task BrowserResponseHandler(
        WebAuthenticationResult result,
        BrowserResponse response);

    /// <summary>
    /// Decides whether a request received on the redirect path belongs to the authentication flow.
    /// </summary>
    /// <param name="callbackUri">
    /// The candidate callback uri.
    /// </param>
    /// <returns>
    /// <see langword="true"/> to complete the flow with <paramref name="callbackUri"/>;
    /// <see langword="false"/> to reject it and keep waiting.
    /// </returns>
    public delegate bool BrowserCallbackFilter(Uri callbackUri);
}
