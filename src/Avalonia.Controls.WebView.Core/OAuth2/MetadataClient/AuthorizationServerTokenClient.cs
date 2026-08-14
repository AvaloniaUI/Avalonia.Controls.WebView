using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Controls.OAuth2.MetadataClient;

/// <summary>Exchanges an authorization code for tokens at the <c>token_endpoint</c>.</summary>
internal static class AuthorizationServerTokenClient
{
    private static readonly HttpClient s_sharedClient = new();

    /// <summary>
    /// POST <c>grant_type=authorization_code</c> with PKCE <paramref name="codeVerifier"/>.
    /// </summary>
    /// <param name="tokenEndpoint">The token endpoint of the authorization server.</param>
    /// <param name="clientId">OAuth client identifier.</param>
    /// <param name="authorizationCode">The code from the authorization redirect.</param>
    /// <param name="redirectUri">Exact <c>redirect_uri</c> used in the authorization request.</param>
    /// <param name="codeVerifier">The PKCE code verifier for this flow.</param>
    /// <param name="httpClient">Optional HTTP client; a shared instance is used when null.</param>
    /// <param name="clientSecret">Optional client secret for confidential clients.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public static async Task<OAuth2TokenResponse> ExchangeAuthorizationCodeAsync(
        Uri tokenEndpoint,
        string clientId,
        string authorizationCode,
        string redirectUri,
        string codeVerifier,
        HttpClient? httpClient = null,
        string? clientSecret = null,
        CancellationToken cancellationToken = default)
    {
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

        var client = httpClient ?? s_sharedClient;
        using var content = new FormUrlEncodedContent(form);
        using var response = await client.PostAsync(tokenEndpoint, content, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Token endpoint returned {(int)response.StatusCode}: {body}");

        OAuth2TokenResponse? token;
        try
        {
            token = JsonSerializer.Deserialize(body, OAuth2JsonContext.Default.OAuth2TokenResponse);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Token response could not be read.", ex);
        }

        if (token is null || string.IsNullOrEmpty(token.AccessToken))
            throw new InvalidOperationException("Token response could not be read.");

        return token;
    }
}
