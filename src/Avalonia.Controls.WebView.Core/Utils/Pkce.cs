using System;
using System.Security.Cryptography;
using System.Text;

namespace Avalonia.Controls.Utils;

/// <summary>
/// Proof Key for Code Exchange (PKCE) helpers per <see href="https://www.rfc-editor.org/rfc/rfc7636">RFC 7636</see>.
/// </summary>
internal static class Pkce
{
    /// <summary>
    /// Creates a random code verifier.
    /// </summary>
    /// <remarks>
    /// 32 random bytes encode to 43 characters, which is the length RFC 7636 requires as a minimum.
    /// </remarks>
    public static string CreateCodeVerifier()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// Computes the S256 code challenge (BASE64URL(SHA256(code_verifier))) for a verifier.
    /// </summary>
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
