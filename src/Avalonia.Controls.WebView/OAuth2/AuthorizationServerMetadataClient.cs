using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Controls.OAuth2;

/// <summary>
/// Fetches <see cref="AuthorizationServerMetadata"/> from the RFC 8414 well-known URL
/// <c>/.well-known/oauth-authorization-server</c>.
/// </summary>
public static class AuthorizationServerMetadataClient
{
    private static readonly HttpClient SharedClient = new();

    /// <summary>
    /// GET <c>{issuer}/.well-known/oauth-authorization-server</c> and deserialize metadata.
    /// </summary>
    /// <param name="issuer">Authorization server issuer identifier (URL, no fragment or query).</param>
    /// <param name="httpClient">Optional <see cref="HttpClient"/>; a shared instance is used when null.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The deserialized authorization server metadata.</returns>
    public static async Task<AuthorizationServerMetadata> GetAsync(
        string issuer,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(issuer))
            throw new ArgumentException("Issuer is required.", nameof(issuer));

        var metadataUrl = GetWellKnownMetadataUrl(issuer);
        var client = httpClient ?? SharedClient;
        using var response = await client.GetAsync(metadataUrl, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var metadata = JsonSerializer.Deserialize(json, OAuth2JsonContext.Default.AuthorizationServerMetadata);
        if (metadata is null)
            throw new InvalidOperationException("Authorization server metadata response was empty.");

        return metadata;
    }

    /// <summary>
    /// Builds the RFC 8414 metadata URL: issuer path + <c>/.well-known/oauth-authorization-server</c>.
    /// </summary>
    /// <param name="issuer">The authorization server issuer identifier.</param>
    /// <returns>The absolute metadata document URL.</returns>
    public static string GetWellKnownMetadataUrl(string issuer)
    {
        var trimmed = issuer.TrimEnd('/');
        return $"{trimmed}/.well-known/oauth-authorization-server";
    }
}
