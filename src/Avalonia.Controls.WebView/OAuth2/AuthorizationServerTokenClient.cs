using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Controls.OAuth2;

/// <summary>Exchanges an authorization code for tokens at the RFC 8414 <c>token_endpoint</c>.</summary>
public static class AuthorizationServerTokenClient
{
    private static readonly HttpClient SharedClient = new();

    /// <summary>
    /// POST <c>grant_type=authorization_code</c> with PKCE <paramref name="codeVerifier"/>.
    /// </summary>
    /// <param name="metadata">Authorization server metadata (must include <c>token_endpoint</c>).</param>
    /// <param name="clientId">OAuth client identifier.</param>
    /// <param name="authorizationCode">The code from the authorization redirect.</param>
    /// <param name="redirectUri">Exact <c>redirect_uri</c> used in the authorization request.</param>
    /// <param name="codeVerifier">The PKCE code verifier for this flow.</param>
    /// <param name="httpClient">Optional HTTP client; a shared instance is used when null.</param>
    /// <param name="clientSecret">Optional client secret for confidential clients.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The token response from the authorization server.</returns>
    public static async Task<OAuth2TokenResponse> ExchangeAuthorizationCodeAsync(
        AuthorizationServerMetadata metadata,
        string clientId,
        string authorizationCode,
        string redirectUri,
        string codeVerifier,
        HttpClient? httpClient = null,
        string? clientSecret = null,
        CancellationToken cancellationToken = default)
    {
        if (metadata.TokenEndpoint is not { Length: > 0 } tokenEndpoint)
            throw new InvalidOperationException("Authorization server metadata is missing token_endpoint.");

        var form = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "authorization_code"),
            new("client_id", clientId),
            new("code", authorizationCode),
            new("redirect_uri", redirectUri),
            new("code_verifier", codeVerifier),
        };

        if (!string.IsNullOrEmpty(clientSecret))
            form.Add(new KeyValuePair<string, string>("client_secret", clientSecret));

        var client = httpClient ?? SharedClient;
        using var content = new FormUrlEncodedContent(form);
        using var response = await client.PostAsync(tokenEndpoint, content, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Token endpoint returned {(int)response.StatusCode}: {body}");

        var token = JsonSerializer.Deserialize(body, OAuth2JsonContext.Default.OAuth2TokenResponse);
        if (token is null || string.IsNullOrEmpty(token.AccessToken))
            throw new InvalidOperationException("Token response could not be read.");

        return token;
    }
}
