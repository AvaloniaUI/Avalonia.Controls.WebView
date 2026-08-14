using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Controls.OAuth2.MetadataClient;

/// <summary>
/// Fetches <see cref="AuthorizationServerMetadata"/> from the issuer's well-known URL.
/// </summary>
internal static class AuthorizationServerMetadataClient
{
    private static readonly HttpClient s_sharedClient = new();

    /// <summary>
    /// Downloads and validates the metadata document of <paramref name="issuer"/>.
    /// </summary>
    /// <param name="issuer">Authorization server issuer identifier.</param>
    /// <param name="httpClient">Optional <see cref="HttpClient"/>; a shared instance is used when null.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="InvalidOperationException">No well-known URL responded, or the document is not valid for this issuer.</exception>
    public static async Task<AuthorizationServerMetadata> GetAsync(
        Uri issuer,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var client = httpClient ?? s_sharedClient;
        var candidates = GetWellKnownMetadataUrls(issuer);
        AuthorizationServerMetadata? metadata = null;

        foreach (var candidate in candidates)
        {
            using var response = await client.GetAsync(candidate, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                continue;

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                metadata = JsonSerializer.Deserialize(json, OAuth2JsonContext.Default.AuthorizationServerMetadata);
            }
            catch (JsonException)
            {
                // A server can answer an unknown path with an HTML error page. Try the next candidate.
                continue;
            }

            // A document without an issuer is not usable, and can be an unrelated JSON response.
            if (metadata is { Issuer.Length: > 0 })
                break;

            metadata = null;
        }

        if (metadata?.Issuer is not { } documentIssuer)
            throw new InvalidOperationException(
                $"No authorization server metadata was found for '{issuer}'. Tried: {string.Join(", ", candidates)}.");

        // RFC 8414, section 3.3: the issuer in the document must be the one the document was requested for.
        // Without this check, a redirect or a mistyped issuer can point the token request at another server.
        if (!string.Equals(documentIssuer.TrimEnd('/'), issuer.AbsoluteUri.TrimEnd('/'), StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Authorization server metadata declares issuer '{documentIssuer}', but was requested for '{issuer}'.");

        return metadata;
    }

    /// <remarks>
    /// RFC 8414 section 3.1 inserts the well-known segment between the host and the issuer path.
    /// The OpenID Connect variants follow, because many servers only publish those.
    /// </remarks>
    public static IReadOnlyList<string> GetWellKnownMetadataUrls(Uri issuer)
    {
        var authority = issuer.GetLeftPart(UriPartial.Authority);
        var path = issuer.AbsolutePath.TrimEnd('/');

        var candidates = new List<string>(3)
        {
            $"{authority}/.well-known/oauth-authorization-server{path}",
            $"{authority}/.well-known/openid-configuration{path}",
        };

        // Identical to the previous candidate when the issuer has no path.
        var appended = $"{authority}{path}/.well-known/openid-configuration";
        if (!string.Equals(candidates[1], appended, StringComparison.Ordinal))
            candidates.Add(appended);

        return candidates;
    }

    public static Uri ParseIssuer(string issuer, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(issuer))
            throw new ArgumentException("Issuer is required.", parameterName);

        if (!Uri.TryCreate(issuer.Trim(), UriKind.Absolute, out var uri))
            throw new ArgumentException($"Issuer must be an absolute URL, but was '{issuer}'.", parameterName);

        if (!OAuth2Endpoint.IsSecure(uri))
            throw new ArgumentException($"Issuer must be an https URL, but was '{issuer}'.", parameterName);

        return uri;
    }
}
