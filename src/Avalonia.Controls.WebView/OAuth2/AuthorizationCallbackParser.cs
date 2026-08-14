using System;
using System.Collections.Generic;

namespace Avalonia.Controls.OAuth2;

/// <summary>Parses the authorization response redirect (query string).</summary>
public static class AuthorizationCallbackParser
{
    /// <summary>
    /// Parses <paramref name="callbackUri"/> query for <c>code</c>, <c>state</c>, and OAuth <c>error</c> parameters.
    /// </summary>
    /// <param name="callbackUri">The redirect URI including query from the authorization server.</param>
    /// <param name="expectedState">The <c>state</c> value from the authorization request.</param>
    /// <returns>The parsed authorization code.</returns>
    /// <exception cref="InvalidOperationException">Missing code, state mismatch, or error response.</exception>
    public static AuthorizationCallbackResult Parse(Uri callbackUri, string expectedState)
    {
        var query = callbackUri.Query;
        if (string.IsNullOrEmpty(query))
            throw new InvalidOperationException("Callback URI has no query string.");

        var coll = ParseQueryString(query);
        if (coll.TryGetValue("error", out var error) && !string.IsNullOrEmpty(error))
        {
            coll.TryGetValue("error_description", out var desc);
            throw new InvalidOperationException(
                string.IsNullOrEmpty(desc) ? error : $"{error}: {desc}");
        }

        if (!coll.TryGetValue("code", out var code) || string.IsNullOrEmpty(code))
            throw new InvalidOperationException("Callback URI is missing code.");

        if (!coll.TryGetValue("state", out var state) || string.IsNullOrEmpty(state))
            throw new InvalidOperationException("Callback URI is missing state.");

        if (!string.Equals(state, expectedState, StringComparison.Ordinal))
            throw new InvalidOperationException("State does not match the authorization request.");

        return new AuthorizationCallbackResult(code);
    }

    static Dictionary<string, string> ParseQueryString(string query)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var trimmed = query.StartsWith("?", StringComparison.Ordinal) ? query[1..] : query;
        if (trimmed.Length == 0)
            return d;

        foreach (var part in trimmed.Split('&'))
        {
            if (part.Length == 0)
                continue;
            var i = part.IndexOf('=');
            string key;
            string value;
            if (i < 0)
            {
                key = Uri.UnescapeDataString(part);
                value = "";
            }
            else
            {
                key = Uri.UnescapeDataString(part[..i]);
                value = Uri.UnescapeDataString(part[(i + 1)..]);
            }

            d[key] = value;
        }

        return d;
    }
}

/// <summary>OAuth authorization redirect response values.</summary>
/// <param name="AuthorizationCode">The authorization code for the token endpoint.</param>
public readonly record struct AuthorizationCallbackResult(string AuthorizationCode);
