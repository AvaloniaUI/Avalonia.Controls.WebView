using System.Text.Json.Serialization;

namespace Avalonia.Controls.OAuth2;

/// <summary>
/// Successful token endpoint response (subset of fields commonly used with authorization code + PKCE).
/// </summary>
public sealed class OAuth2TokenResponse
{
    /// <summary>Gets the access token issued by the authorization server.</summary>
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }

    /// <summary>Gets the token type (typically <c>Bearer</c>).</summary>
    [JsonPropertyName("token_type")]
    public string? TokenType { get; init; }

    /// <summary>Gets the lifetime in seconds of the access token.</summary>
    [JsonPropertyName("expires_in")]
    public long? ExpiresIn { get; init; }

    /// <summary>Gets the refresh token, if issued.</summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    /// <summary>Gets the OpenID Connect ID token, if issued.</summary>
    [JsonPropertyName("id_token")]
    public string? IdToken { get; init; }

    /// <summary>Gets the granted scope, if the server returns it.</summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; init; }
}
