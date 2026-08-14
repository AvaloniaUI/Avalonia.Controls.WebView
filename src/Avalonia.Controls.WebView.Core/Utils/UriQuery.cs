using System;
using System.Collections.Generic;

namespace Avalonia.Controls.Utils;

/// <summary>
/// Parses URI query strings for authentication callbacks and requests.
/// </summary>
/// <remarks>
/// <see cref="System.Web.HttpUtility"/> is not used here
/// It matches names case-insensitively, joins repeated names into one comma separated value, and decodes <c>+</c> as a space.
/// None of which suit OAuth 2.0 parameters.
/// </remarks>
internal static class UriQuery
{
    /// <summary>
    /// Parses the query of <paramref name="uri"/> into its parameters.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Parse(Uri uri) =>
        Parse(uri.IsAbsoluteUri ? uri.Query : "");

    /// <inheritdoc cref="Parse(Uri)"/>
    public static IReadOnlyDictionary<string, string> Parse(string query)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        HashSet<string>? repeated = null;

        foreach (var (name, pair) in Split(query))
        {
            var separator = pair.IndexOf('=');
            var value = separator < 0 ? "" : Uri.UnescapeDataString(pair[(separator + 1)..]);

            if (!parameters.TryAdd(name, value))
                (repeated ??= new HashSet<string>(StringComparer.Ordinal)).Add(name);
        }

        if (repeated is not null)
        {
            foreach (var name in repeated)
                parameters.Remove(name);
        }

        return parameters;
    }

    /// <summary>
    /// Splits <paramref name="query"/> into its pairs, decoding the name and keeping the pair as written.
    /// </summary>
    public static IReadOnlyList<(string Name, string Pair)> Split(string query)
    {
        if (query.Length == 0)
            return [];

        if (query[0] == '?')
            query = query[1..];

        var pairs = new List<(string, string)>();

        foreach (var pair in query.Split('&'))
        {
            if (pair.Length == 0)
                continue;

            var separator = pair.IndexOf('=');
            pairs.Add((Uri.UnescapeDataString(separator < 0 ? pair : pair[..separator]), pair));
        }

        return pairs;
    }
}
