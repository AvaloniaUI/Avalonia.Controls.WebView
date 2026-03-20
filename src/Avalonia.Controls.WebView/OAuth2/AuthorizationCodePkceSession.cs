using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Avalonia.Controls.OAuth2;

/// <summary>
/// Holds PKCE and CSRF values for one authorization code flow built from
/// <see cref="AuthorizationServerMetadata"/> (RFC 8414).
/// </summary>
public sealed class AuthorizationCodePkceSession
{
    AuthorizationCodePkceSession(
        Uri authorizationUri,
        Uri redirectUri,
        string redirectUriString,
        string state,
        string codeVerifier,
        string? nonce)
    {
        AuthorizationUri = authorizationUri;
        RedirectUri = redirectUri;
        RedirectUriString = redirectUriString;
        State = state;
        CodeVerifier = codeVerifier;
        Nonce = nonce;
    }

    /// <summary>Gets the full authorization request URL (includes query).</summary>
    public Uri AuthorizationUri { get; }

    /// <summary>
    /// Gets the redirect URI registered for the client (used for broker callback matching).
    /// </summary>
    public Uri RedirectUri { get; }

    /// <summary>
    /// Gets the exact <c>redirect_uri</c> string sent to the authorize and token endpoints
    /// (must match client registration byte-for-byte).
    /// Use this instead of <see cref="Uri.ToString"/> on <see cref="RedirectUri"/>, which can add a trailing slash.
    /// </summary>
    public string RedirectUriString { get; }

    /// <summary>Gets the opaque CSRF value sent as <c>state</c>.</summary>
    public string State { get; }

    /// <summary>Gets the PKCE code verifier; send to the token endpoint as <c>code_verifier</c>.</summary>
    public string CodeVerifier { get; }

    /// <summary>Gets the optional OIDC nonce, if requested.</summary>
    public string? Nonce { get; }

    /// <summary>
    /// Creates a session: validates metadata for authorization code + S256 PKCE, then builds the authorization URL.
    /// </summary>
    /// <param name="metadata">Authorization server metadata from RFC 8414 discovery.</param>
    /// <param name="clientId">OAuth client identifier.</param>
    /// <param name="redirectUri">Registered redirect URI (exact string; must match token request).</param>
    /// <param name="scope">Space-separated OAuth scopes.</param>
    /// <param name="nonce">Optional OpenID Connect nonce.</param>
    /// <param name="resource">Optional resource indicator (RFC 8707).</param>
    /// <returns>A session holding URIs, PKCE verifier, and state for the broker and token exchange.</returns>
    public static AuthorizationCodePkceSession Create(
        AuthorizationServerMetadata metadata,
        string clientId,
        string redirectUri,
        string scope,
        string? nonce = null,
        string? resource = null)
    {
        if (metadata.AuthorizationEndpoint is not { Length: > 0 } authEndpoint)
            throw new InvalidOperationException("Authorization server metadata is missing authorization_endpoint.");

        var redirectForOAuth = redirectUri.Trim();
        if (redirectForOAuth.Length == 0)
            throw new ArgumentException("Redirect URI is required.", nameof(redirectUri));

        if (!Uri.TryCreate(redirectForOAuth, UriKind.Absolute, out var redirectUriParsed))
            throw new ArgumentException("Redirect URI must be an absolute URL.", nameof(redirectUri));

        EnsurePkceS256Supported(metadata);

        var codeVerifier = Pkce.CreateCodeVerifier();
        var codeChallenge = Pkce.CreateCodeChallengeS256(codeVerifier);
        var state = CreateState();

        var query = new List<string>
        {
            "response_type=code",
            $"client_id={Uri.EscapeDataString(clientId)}",
            $"redirect_uri={Uri.EscapeDataString(redirectForOAuth)}",
            $"scope={Uri.EscapeDataString(scope)}",
            $"state={Uri.EscapeDataString(state)}",
            $"code_challenge={Uri.EscapeDataString(codeChallenge)}",
            "code_challenge_method=S256",
        };

        if (!string.IsNullOrEmpty(nonce))
            query.Add($"nonce={Uri.EscapeDataString(nonce)}");

        if (!string.IsNullOrEmpty(resource))
            query.Add($"resource={Uri.EscapeDataString(resource)}");

        var separator = authEndpoint.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        var authorizationUri = new Uri($"{authEndpoint}{separator}{string.Join("&", query)}");

        return new AuthorizationCodePkceSession(
            authorizationUri,
            redirectUriParsed,
            redirectForOAuth,
            state,
            codeVerifier,
            nonce);
    }

    static void EnsurePkceS256Supported(AuthorizationServerMetadata metadata)
    {
        var methods = metadata.CodeChallengeMethodsSupported;
        if (methods is null || methods.Length == 0)
            return;

        foreach (var m in methods)
        {
            if (string.Equals(m, "S256", StringComparison.OrdinalIgnoreCase))
                return;
        }

        throw new InvalidOperationException(
            "Authorization server metadata lists code_challenge_methods_supported but does not include S256.");
    }

    static string CreateState()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }
}
