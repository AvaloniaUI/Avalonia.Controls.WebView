using System.Text.Json.Serialization;

namespace Avalonia.Controls.OAuth2;

/// <summary>
/// OAuth 2.0 Authorization Server Metadata per <see href="https://www.rfc-editor.org/rfc/rfc8414">RFC 8414</see>.
/// </summary>
public sealed class AuthorizationServerMetadata
{
    /// <summary>Gets the required issuer identifier.</summary>
    [JsonPropertyName("issuer")]
    public string? Issuer { get; init; }

    /// <summary>Gets the URL of the authorization endpoint.</summary>
    [JsonPropertyName("authorization_endpoint")]
    public string? AuthorizationEndpoint { get; init; }

    /// <summary>Gets the URL of the token endpoint.</summary>
    [JsonPropertyName("token_endpoint")]
    public string? TokenEndpoint { get; init; }

    /// <summary>Gets the PKCE code challenge methods supported by the authorization server.</summary>
    [JsonPropertyName("code_challenge_methods_supported")]
    public string[]? CodeChallengeMethodsSupported { get; init; }

    /// <summary>Gets the OAuth scopes the authorization server supports.</summary>
    [JsonPropertyName("scopes_supported")]
    public string[]? ScopesSupported { get; init; }

    /// <summary>Gets the OAuth response types the authorization server supports.</summary>
    [JsonPropertyName("response_types_supported")]
    public string[]? ResponseTypesSupported { get; init; }
}
