using System.Text.Json.Serialization;

namespace Avalonia.Controls.OAuth2.MetadataClient;

/// <summary>System.Text.Json source generation context for OAuth 2.0 metadata and token JSON.</summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(AuthorizationServerMetadata))]
[JsonSerializable(typeof(OAuth2TokenResponse))]
internal partial class OAuth2JsonContext : JsonSerializerContext;
