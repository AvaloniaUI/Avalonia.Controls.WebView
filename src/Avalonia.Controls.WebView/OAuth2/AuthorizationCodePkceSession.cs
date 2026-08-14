using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Controls.OAuth2;

/// <summary>
/// One OAuth 2.0 authorization code flow with PKCE: builds the authorization request,
/// checks the callback, and exchanges the code for tokens.
/// </summary>
/// <remarks>
/// Pass <see cref="AuthorizationUri"/> and <see cref="RedirectUri"/> to
/// <see cref="WebAuthenticationBroker"/>, then hand the result to <see cref="ExchangeCodeAsync"/>.
/// </remarks>
public sealed class AuthorizationCodePkceSession
{
    private readonly Uri _tokenEndpoint;
    private readonly string _clientId;
    private readonly string _redirectUriString;
    private readonly string _codeVerifier;

    private AuthorizationCodePkceSession(
        Uri authorizationUri,
        Uri tokenEndpoint,
        string clientId,
        Uri redirectUri,
        string redirectUriString,
        string state,
        string codeVerifier)
    {
        AuthorizationUri = authorizationUri;
        RedirectUri = redirectUri;
        State = state;
        _tokenEndpoint = tokenEndpoint;
        _clientId = clientId;
        _redirectUriString = redirectUriString;
        _codeVerifier = codeVerifier;
    }

    /// <summary>Gets the full authorization request URL, including its query.</summary>
    public Uri AuthorizationUri { get; }

    /// <summary>Gets the redirect URI registered for the client.</summary>
    public Uri RedirectUri { get; }

    /// <summary>Gets the value sent as <c>state</c>, which ties the callback to this session.</summary>
    public string State { get; }

    /// <summary>
    /// Discovers the endpoints of <paramref name="issuer"/> and creates a session for them.
    /// </summary>
    /// <param name="issuer">Issuer identifier of the authorization server, as an https URL.</param>
    /// <param name="clientId">OAuth client identifier.</param>
    /// <param name="redirectUri">Registered redirect URI, used exactly as written.</param>
    /// <param name="scope">Space separated OAuth scopes.</param>
    /// <param name="nonce">Optional OpenID Connect nonce.</param>
    /// <param name="resource">Optional resource indicator (RFC 8707).</param>
    /// <param name="httpClient">Optional <see cref="HttpClient"/> used for discovery.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentException">An argument is empty or not an absolute URL.</exception>
    /// <exception cref="InvalidOperationException">The server published no usable metadata.</exception>
    public static async Task<AuthorizationCodePkceSession> CreateAsync(
        string issuer,
        string clientId,
        string redirectUri,
        string scope,
        string? nonce = null,
        string? resource = null,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var issuerUri = AuthorizationServerMetadataClient.ParseIssuer(issuer, nameof(issuer));
        var metadata = await AuthorizationServerMetadataClient
            .GetAsync(issuerUri, httpClient, cancellationToken)
            .ConfigureAwait(false);

        EnsurePkceS256Supported(metadata);

        var authorizationEndpoint = OAuth2Endpoint.ParseMetadataUrl(
            metadata.AuthorizationEndpoint, "authorization_endpoint");
        var tokenEndpoint = OAuth2Endpoint.ParseMetadataUrl(
            metadata.TokenEndpoint, "token_endpoint");

        return Create(authorizationEndpoint, tokenEndpoint, clientId, redirectUri, scope, nonce, resource);
    }

    /// <summary>
    /// Creates a session for endpoints that are already known, without discovery.
    /// </summary>
    /// <param name="authorizationEndpoint">The authorization endpoint of the server.</param>
    /// <param name="tokenEndpoint">The token endpoint of the server.</param>
    /// <param name="clientId">OAuth client identifier.</param>
    /// <param name="redirectUri">Registered redirect URI, used exactly as written.</param>
    /// <param name="scope">Space separated OAuth scopes.</param>
    /// <param name="nonce">Optional OpenID Connect nonce.</param>
    /// <param name="resource">Optional resource indicator (RFC 8707).</param>
    /// <exception cref="ArgumentException">An argument is empty, not absolute, or not an https URL.</exception>
    public static AuthorizationCodePkceSession Create(
        Uri authorizationEndpoint,
        Uri tokenEndpoint,
        string clientId,
        string redirectUri,
        string scope,
        string? nonce = null,
        string? resource = null)
    {
        EnsureSecureEndpoint(authorizationEndpoint, nameof(authorizationEndpoint));
        EnsureSecureEndpoint(tokenEndpoint, nameof(tokenEndpoint));

        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("Client ID is required.", nameof(clientId));

        // The redirect URI has to reach the token endpoint byte for byte as it was registered,
        // so the original string is kept instead of a round-tripped Uri.
        var redirectUriString = redirectUri?.Trim() ?? "";
        if (redirectUriString.Length == 0)
            throw new ArgumentException("Redirect URI is required.", nameof(redirectUri));

        if (!Uri.TryCreate(redirectUriString, UriKind.Absolute, out var redirectUriParsed))
            throw new ArgumentException("Redirect URI must be an absolute URL.", nameof(redirectUri));

        var codeVerifier = Pkce.CreateCodeVerifier();
        var state = CreateState();

        var query = new List<string>
        {
            "response_type=code",
            $"client_id={Uri.EscapeDataString(clientId)}",
            $"redirect_uri={Uri.EscapeDataString(redirectUriString)}",
            $"scope={Uri.EscapeDataString(scope ?? "")}",
            $"state={Uri.EscapeDataString(state)}",
            $"code_challenge={Uri.EscapeDataString(Pkce.CreateCodeChallengeS256(codeVerifier))}",
            "code_challenge_method=S256",
        };

        if (!string.IsNullOrEmpty(nonce))
            query.Add($"nonce={Uri.EscapeDataString(nonce)}");

        if (!string.IsNullOrEmpty(resource))
            query.Add($"resource={Uri.EscapeDataString(resource)}");

        return new AuthorizationCodePkceSession(
            AppendQuery(authorizationEndpoint, string.Join("&", query)),
            tokenEndpoint,
            clientId,
            redirectUriParsed,
            redirectUriString,
            state,
            codeVerifier);
    }

    /// <summary>
    /// Returns true when <paramref name="result"/> carries the <c>state</c> of this session.
    /// </summary>
    public bool IsCallbackFor(WebAuthenticationResult result) =>
        result is not null && string.Equals(result.State, State, StringComparison.Ordinal);

    /// <summary>
    /// Validates the callback and exchanges its authorization code for tokens.
    /// </summary>
    /// <param name="result">The result returned by <see cref="WebAuthenticationBroker"/>.</param>
    /// <param name="clientSecret">Optional client secret for confidential clients.</param>
    /// <param name="httpClient">Optional <see cref="HttpClient"/> used for the token request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public async Task<OAuth2TokenResponse> ExchangeCodeAsync(
        WebAuthenticationResult result,
        string? clientSecret = null,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        if (result is null)
            throw new ArgumentNullException(nameof(result));

        // State is checked first: anything else in the callback, including an error response,
        // may have been sent by a process that is not part of this flow.
        if (!IsCallbackFor(result))
            throw new InvalidOperationException("The callback state does not match the authorization request.");

        if (result.Error is { } error)
        {
            throw new InvalidOperationException(
                result.ErrorDescription is { } description ? $"{error}: {description}" : error);
        }

        if (result.Code is not { } code)
            throw new InvalidOperationException("The callback has no authorization code.");

        return await AuthorizationServerTokenClient.ExchangeAuthorizationCodeAsync(
            _tokenEndpoint,
            _clientId,
            code,
            _redirectUriString,
            _codeVerifier,
            httpClient,
            clientSecret,
            cancellationToken).ConfigureAwait(false);
    }

    private static Uri AppendQuery(Uri endpoint, string query)
    {
        var builder = new UriBuilder(endpoint);
        builder.Query = builder.Query is { Length: > 1 } existing
            ? $"{existing[1..]}&{query}"
            : query;
        return builder.Uri;
    }

    private static void EnsureSecureEndpoint(Uri endpoint, string parameterName)
    {
        if (endpoint is null)
            throw new ArgumentNullException(parameterName);

        if (!endpoint.IsAbsoluteUri)
            throw new ArgumentException("Endpoint must be an absolute URL.", parameterName);

        if (!OAuth2Endpoint.IsSecure(endpoint))
            throw new ArgumentException($"Endpoint must be an https URL, but was '{endpoint}'.", parameterName);
    }

    private static void EnsurePkceS256Supported(AuthorizationServerMetadata metadata)
    {
        var methods = metadata.CodeChallengeMethodsSupported;

        // An absent list says nothing about support, so it is not treated as a failure.
        if (methods is null || methods.Length == 0)
            return;

        foreach (var method in methods)
        {
            if (string.Equals(method, "S256", StringComparison.OrdinalIgnoreCase))
                return;
        }

        throw new InvalidOperationException(
            "Authorization server metadata lists code_challenge_methods_supported but does not include S256.");
    }

    private static string CreateState()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }
}
