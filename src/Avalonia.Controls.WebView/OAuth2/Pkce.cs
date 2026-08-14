using System;
using System.Security.Cryptography;
using System.Text;

namespace Avalonia.Controls.OAuth2;

/// <summary>
/// Proof Key for Code Exchange (PKCE) helpers per <see href="https://www.rfc-editor.org/rfc/rfc7636">RFC 7636</see>.
/// </summary>
public static class Pkce
{
    /// <summary>
    /// Creates a new cryptographically random code verifier string (RFC 7636).
    /// </summary>
    /// <param name="size">Number of random bytes to encode (default 64). Must be 43-128.</param>
    /// <returns>The code verifier string.</returns>
    public static string CreateCodeVerifier(int size = 64)
    {
        if (size is < 43 or > 128)
            throw new ArgumentOutOfRangeException(nameof(size), "Verifier length must be between 43 and 128.");

        // Unreserved characters [A-Z] / [a-z] / [0-9] / "-" / "." / "_" / "~"
        var bytes = new byte[size];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// Computes the S256 code challenge (BASE64URL(SHA256(code_verifier))) for a verifier.
    /// </summary>
    /// <param name="codeVerifier">The PKCE code verifier.</param>
    /// <returns>The code challenge string.</returns>
    public static string CreateCodeChallengeS256(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
