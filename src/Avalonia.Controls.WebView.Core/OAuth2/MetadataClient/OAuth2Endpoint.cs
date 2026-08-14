using System;

namespace Avalonia.Controls.OAuth2.MetadataClient;

/// <summary>
/// Checks that URLs used to carry authorization codes, PKCE verifiers and client secrets are transport secured.
/// </summary>
internal static class OAuth2Endpoint
{
    /// <summary>
    /// Returns true for an absolute <c>https</c> URL, or an <c>http</c> URL on the loopback interface.
    /// </summary>
    /// <remarks>
    /// Loopback is allowed because local development servers and the tests run over plain HTTP.
    /// </remarks>
    public static bool IsSecure(Uri uri) =>
        uri.IsAbsoluteUri &&
        (uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback));

    /// <summary>
    /// Parses a URL from authorization server metadata and requires it to be secure.
    /// </summary>
    /// <exception cref="InvalidOperationException">The value is missing, relative, or not transport secured.</exception>
    public static Uri ParseMetadataUrl(string? value, string parameterName)
    {
        if (value is not { Length: > 0 })
            throw new InvalidOperationException($"Authorization server metadata is missing {parameterName}.");

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            throw new InvalidOperationException(
                $"Authorization server metadata has an invalid {parameterName}: '{value}'.");

        if (!IsSecure(uri))
            throw new InvalidOperationException(
                $"Authorization server metadata {parameterName} must be an https URL, but was '{value}'.");

        return uri;
    }
}
