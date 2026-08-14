using System;
using System.Collections.Generic;
using Avalonia.Controls.Utils;

// ReSharper disable once CheckNamespace
namespace Avalonia.Controls;

/// <param name="CallbackUri">The response URI containing authentication data.</param>
public record WebAuthenticationResult(Uri CallbackUri)
{
    private Uri? _parsedFor;

    /// <summary>
    /// Gets the parameters of the <see cref="CallbackUri"/> query string.
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters
    {
        get
        {
            if (!ReferenceEquals(_parsedFor, CallbackUri))
            {
                field = UriQuery.Parse(CallbackUri);
                _parsedFor = CallbackUri;
            }

            return field!;
        }
    }

    /// <summary>
    /// Gets the OAuth 2.0 <c>code</c> parameter, or null when it is absent or repeated.
    /// </summary>
    public string? Code => GetParameter("code");

    /// <summary>
    /// Gets the OAuth 2.0 <c>state</c> parameter, or null when it is absent or repeated.
    /// </summary>
    public string? State => GetParameter("state");

    /// <summary>
    /// Gets the OAuth 2.0 <c>error</c> parameter, or null when the callback is not an error response.
    /// </summary>
    public string? Error => GetParameter("error");

    /// <summary>
    /// Gets the OAuth 2.0 <c>error_description</c> parameter, if the server sent one.
    /// </summary>
    public string? ErrorDescription => GetParameter("error_description");

    private string? GetParameter(string name) =>
        Parameters.TryGetValue(name, out var value) && value.Length > 0 ? value : null;
}
