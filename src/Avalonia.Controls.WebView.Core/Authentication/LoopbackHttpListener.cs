using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Controls.Authentication;

/// <summary>
/// Local HTTP listener that captures an OAuth redirect callback from the browser.
/// </summary>
/// <remarks>
/// Uses <see cref="TcpListener"/> bound to the loopback address rather than <see cref="HttpListener"/>.
/// <see cref="HttpListener"/> on macOS does not reliably accept connections when its prefix contains a
/// literal IP address such as <c>http://127.0.0.1:port/</c>, causing the browser redirect to fail with a
/// "Can't Connect to the Server" error. <see cref="TcpListener"/> binds directly to the desired
/// <see cref="IPEndPoint"/> and avoids this platform specific limitation.
/// </remarks>
[UnsupportedOSPlatform("browser")]
internal sealed class LoopbackHttpListener : IDisposable
{
    private const int HttpRequestBufferSize = 4096;

    private readonly TcpListener _listener;
    private readonly string _host;
    private readonly string _redirectPath;

    /// <summary>
    /// Starts a listener on the loopback interface.
    /// </summary>
    /// <param name="redirectUri">
    /// Redirect uri to serve.
    /// Its host selects the loopback address to bind and is reported back in the callback uri.
    /// Its port is used when specified, otherwise the OS allocates a free one.
    /// </param>
    public LoopbackHttpListener(Uri redirectUri)
    {
        _host = redirectUri.Host;
        _redirectPath = redirectUri.AbsolutePath;

        var address = ResolveBindAddress(redirectUri);
        var port = redirectUri.IsDefaultPort ? 0 : redirectUri.Port;

        _listener = new TcpListener(address, port) { ExclusiveAddressUse = true };

        try
        {
            _listener.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to start the local callback listener on {address}:{port}.", ex);
        }
    }

    private static IPAddress ResolveBindAddress(Uri redirectUri) =>
        IPAddress.TryParse(redirectUri.DnsSafeHost, out var address) && IPAddress.IsLoopback(address)
            ? address
            : IPAddress.Loopback;

    /// <summary>
    /// Port the listener is actually bound to.
    /// </summary>
    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    /// <summary>
    /// Number of requests to the redirect path that <c>callbackFilter</c> rejected.
    /// </summary>
    public int RejectedCallbackCount { get; private set; }

    public async Task<Uri> WaitForCallbackAsync(
        Func<Uri, bool>? callbackFilter,
        Func<Uri, BrowserResponse, Task>? responseFactory,
        CancellationToken cancellationToken)
    {
        // Keep looping until the HTTP client sends a matching request.
        // Browsers can send OPTIONS or favicon.ico first, before the expected callback.
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var requestUri = await ReadRequestUriAsync(client, cancellationToken).ConfigureAwait(false);
                if (requestUri is null)
                    continue;

                var found = requestUri.AbsolutePath == _redirectPath;

                if (found && callbackFilter is not null && !callbackFilter(requestUri))
                {
                    found = false;
                    RejectedCallbackCount++;
                }

                BrowserResponse? response;
                if (found && responseFactory is not null)
                {
                    var stream = new MemoryStream();
                    response = new BrowserResponse(stream);
                    await responseFactory(requestUri, response).ConfigureAwait(false);
                }
                else
                {
                    response = found ? BuildDefaultResponse(HttpStatusCode.OK) : BuildDefaultResponse(HttpStatusCode.NotFound);
                }

                await SendResponseAsync(client, response, cancellationToken).ConfigureAwait(false);
                if (found)
                {
                    return requestUri;
                }
            }
            catch (IOException)
            {
                // Client disconnected or sent a malformed request; wait for the next one.
            }
            catch (SocketException)
            {
                // Client disconnected; wait for the next one.
            }
        }
    }

    public void Dispose()
    {
        try
        {
            _listener.Dispose();
        }
        catch
        {
            // Ignore errors during cleanup.
        }
    }

    private async Task<Uri?> ReadRequestUriAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII,
            detectEncodingFromByteOrderMarks: false, bufferSize: HttpRequestBufferSize, leaveOpen: true);

        // Parse the HTTP request line: "GET /callback?code=...&state=... HTTP/1.1"
        var requestLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        if (requestLine is null)
            return null;

        // Drain all remaining headers so the browser can receive our response.
        // An empty line signals the end of the HTTP header section.
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { Length: > 0 })
        {
        }

        var parts = requestLine.Split(' ');
        if (parts.Length < 2)
            return null;

        var rawTarget = parts[1];
        // Only origin-form targets (starting with '/') are valid for callback GETs.
        // Absolute-form, authority-form and asterisk-form targets are rejected so they can't be
        // concatenated into a spoofed URI.
        if (rawTarget.Length == 0 || rawTarget[0] != '/')
            return null;

        // The authority is the host the application configured plus the port actually bound.
        // It never comes from the request's Host header.
        return Uri.TryCreate($"http://{_host}:{Port}{rawTarget}", UriKind.Absolute, out var uri) ? uri : null;
    }

    private static async Task SendResponseAsync(
        TcpClient client, BrowserResponse response, CancellationToken cancellationToken)
    {
        var redirect = response.ReadRedirect();

        if (response.OutputStream.CanSeek)
        {
            response.OutputStream.Position = 0;
        }

        // A redirect carries the target in the Location header and no body.
        var contentHeader = redirect is not null
            ? $"Location: {redirect.AbsoluteUri}\r\n"
            : "Content-Type: text/html; charset=utf-8\r\n";

        var header =
            $"HTTP/1.1 {(int)response.StatusCode} {GetReasonPhrase(response.StatusCode)}\r\n" +
            contentHeader +
            $"Content-Length: {response.OutputStream.Length}\r\n" +
            "Connection: close\r\n\r\n";

        var headerBytes = Encoding.ASCII.GetBytes(header);

        var stream = client.GetStream();
        await stream.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
        if (response.OutputStream.Length > 0)
        {
            await response.OutputStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        }
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

        // Send FIN so the browser sees a clean end-of-stream rather than a potential RST
        // when the TcpClient is disposed right after this method returns.
        try
        {
            client.Client.Shutdown(SocketShutdown.Send);
        }
        catch (SocketException)
        {
            // Peer already closed the connection.
        }
        catch (ObjectDisposedException)
        {
            // Client disposed during a shutdown race.
        }
    }

    private static string GetReasonPhrase(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.OK => "OK",
            HttpStatusCode.Found => "Found",
            HttpStatusCode.BadRequest => "Bad Request",
            HttpStatusCode.NotFound => "Not Found",
            _ => statusCode.ToString() // enum name is only a best-effort reason phrase
        };
    }

    private static BrowserResponse BuildDefaultResponse(HttpStatusCode code)
    {
        var stream = new MemoryStream();
        if (code == HttpStatusCode.OK)
        {
            stream.Write(
                """
                <!doctype html>
                <html lang="en">
                <head>
                    <meta charset="utf-8">
                    <meta name="viewport" content="width=device-width,initial-scale=1">
                    <title>Authentication complete</title>
                </head>
                <body style="font-family:system-ui,sans-serif;text-align:center;padding:3rem">
                    <h1>Authentication complete</h1>
                    <p>You can close this window and return to the application.</p>
                </body>
                </html>
                """u8);
        }
        else
        {
            stream.Write(Encoding.UTF8.GetBytes($"<!doctype html><title>{GetReasonPhrase(code)}</title>"));
        }

        return new BrowserResponse(stream) { StatusCode = code };
    }
}
