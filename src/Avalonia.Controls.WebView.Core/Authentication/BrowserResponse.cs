using System;
using System.IO;
using System.Net;

namespace Avalonia.Controls.Authentication;

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
    /// </summary>
    public void Redirect(Uri uri)
    {
        _redirect = uri;
        StatusCode = HttpStatusCode.Found;
    }

    internal Uri? ReadRedirect() => _redirect;
}
