using System;
using System.IO;
using System.Net;

// ReSharper disable once CheckNamespace
namespace Avalonia.Controls;

/// <summary>
/// Represents the HTTP response sent to the browser after the authentication callback is received.
/// </summary>
public sealed class BrowserResponse
{
    private Uri? _redirect;
    internal BrowserResponse(Stream outputStream)
    {
        StatusCode = HttpStatusCode.OK;
        OutputStream = outputStream;
    }

    /// <summary>
    /// Gets or sets the HTTP status code of the response.
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }

    /// <summary>
    /// Gets the stream to which the response content can be written.
    /// </summary>
    public Stream OutputStream { get; }

    /// <summary>
    /// Configures the response to redirect the browser to the specified URI.
    /// Sets <see cref="StatusCode"/> to <see cref="HttpStatusCode.Found"/>.
    /// </summary>
    /// <param name="uri">Absolute uri to redirect the browser to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="uri"/> is not absolute.</exception>
    public void Redirect(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (!uri.IsAbsoluteUri)
        {
            throw new ArgumentException("Redirect uri must be absolute.", nameof(uri));
        }

        _redirect = uri;
        StatusCode = HttpStatusCode.Found;
    }

    internal Uri? ReadRedirect() => _redirect;
}
