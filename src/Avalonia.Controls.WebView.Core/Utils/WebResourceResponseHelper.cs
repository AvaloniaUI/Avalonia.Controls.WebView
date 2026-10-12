using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Avalonia.Logging;

namespace Avalonia.Controls.Utils;

internal static class WebResourceResponseHelper
{
    // RFC 9110 token characters.
    private static readonly SearchValues<char> s_tokenChars =
        SearchValues.Create("!#$%&'*+-.^_`|~0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");

    /// <summary>
    /// Returns <see cref="WebViewEnvironmentRequestedEventArgs.CustomSchemes"/> lowercased and without duplicates,
    /// sorted so the order doesn't change the WebView2 environment.
    /// </summary>
    public static string[] GetCustomSchemes(WebViewEnvironmentRequestedEventArgs args) =>
        args.CustomSchemes.Select(scheme => scheme.ToLowerInvariant()).Distinct().Order(StringComparer.Ordinal)
            .ToArray();

    public static bool IsCustomScheme(Uri? uri, string[] schemes) =>
        uri is { IsAbsoluteUri: true } && Array.IndexOf(schemes, uri.Scheme) >= 0;

    public static WebViewWebResourceResponse NotFound() => new() { StatusCode = (int)HttpStatusCode.NotFound };

    /// <summary>
    /// The handler's response, or a 404 for an unanswered custom scheme request.
    /// </summary>
    public static WebViewWebResourceResponse? GetResponseOrNotFound(
        WebViewWebResourceResponse? response, bool isCustomScheme) =>
        response ?? (isCustomScheme ? NotFound() : null);

    /// <summary>
    /// Raises the event, treating a handler that throws as one that didn't answer.
    /// </summary>
    public static void Raise(
        object sender, EventHandler<WebResourceRequestedEventArgs> handler, WebResourceRequestedEventArgs args)
    {
        try
        {
            handler.Invoke(sender, args);
        }
        catch (Exception ex)
        {
            LogError("A WebResourceRequested handler failed: {Exception}", ex);
            DisposeContent(args.Response);
            args.Response = null;
        }
    }

    /// <summary>
    /// Raises the event on the current thread and waits for its deferrals.
    /// </summary>
    public static async Task<WebViewWebResourceResponse?> RaiseAsync(
        object sender, EventHandler<WebResourceRequestedEventArgs>? handler, WebViewWebResourceRequest request)
    {
        if (handler is null)
            return null;

        var args = new WebResourceRequestedEventArgs { Request = request };
        Raise(sender, handler, args);
        await args.WaitForDeferralsAsync();
        return args.Response;
    }

    /// <summary>
    /// Reads the rest of the body, without a copy for a MemoryStream with a visible buffer,
    /// so the result is used before the stream is disposed.
    /// </summary>
    public static async Task<ReadOnlyMemory<byte>> ReadContentAsync(Stream? content)
    {
        if (content is null)
            return ReadOnlyMemory<byte>.Empty;
        if (content is MemoryStream memory && memory.TryGetBuffer(out var buffer))
            return buffer.AsMemory((int)memory.Position);

        var copy = new MemoryStream();
        await content.CopyToAsync(copy).ConfigureAwait(false);
        return copy.GetBuffer().AsMemory(0, (int)copy.Length);
    }

    /// <summary>
    /// Disposes the body the web view owns, logging instead of throwing.
    /// </summary>
    public static void DisposeContent(WebViewWebResourceResponse? response)
    {
        try
        {
            response?.Content?.Dispose();
        }
        catch (Exception ex)
        {
            LogError("Failed to dispose a web resource response: {Exception}", ex);
        }
    }

    /// <summary>
    /// Disposes the body once the handler's deferrals complete, for a response the web view doesn't use.
    /// </summary>
    public static async void DisposeContentWhenDone(WebResourceRequestedEventArgs? args)
    {
        if (args is null)
            return;
        await args.WaitForDeferralsAsync().ConfigureAwait(false);
        DisposeContent(args.Response);
    }

    public static void LogError(string messageTemplate, Exception ex) =>
        Logger.TryGet(LogEventLevel.Error, "WebView")?.Log(null, messageTemplate, ex);

    /// <summary>
    /// The headers that can be sent, without names that aren't tokens and values with control characters,
    /// which could add headers on WebView2.
    /// </summary>
    public static Dictionary<string, string> GetValidHeaders(WebViewWebResourceResponse response)
    {
        var headers = new Dictionary<string, string>(response.Headers.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var header in response.Headers)
        {
            if (header.Key is { Length: > 0 } name && !name.AsSpan().ContainsAnyExcept(s_tokenChars)
                && header.Value is { } value && !HasControlCharacters(value))
                headers[name] = value;
            else
                Logger.TryGet(LogEventLevel.Warning, "WebView")?.Log(null,
                    "Skipped the invalid response header {Name}", header.Key);
        }
        return headers;
    }

    // Tab is allowed.
    private static bool HasControlCharacters(string value) =>
        value.AsSpan().ContainsAnyInRange('\0', '\b') || value.AsSpan().ContainsAnyInRange('\n', '\x1f')
        || value.Contains('\x7f');

    public static string GetReasonPhrase(WebViewWebResourceResponse response)
    {
        // Printable ASCII and not blank, as Android requires.
        if (response.ReasonPhrase is { } reasonPhrase && !string.IsNullOrWhiteSpace(reasonPhrase)
            && !reasonPhrase.AsSpan().ContainsAnyExceptInRange(' ', '~'))
            return reasonPhrase;
        // HttpResponseMessage fills in the standard phrase.
        using var message = new HttpResponseMessage((HttpStatusCode)response.StatusCode);
        if (message.ReasonPhrase is { Length: > 0 } standard)
            return standard;
        return response.StatusCode is >= 200 and < 300 ? "OK" : "Unknown";
    }

    /// <summary>
    /// Splits the Content-Type header, "text/html; charset=utf-8" gives ("text/html", "utf-8").
    /// </summary>
    public static (string? MediaType, string? CharSet) GetContentType(WebViewWebResourceResponse response)
    {
        if (!response.Headers.TryGetValue(HttpHeaderNames.ContentType, out var value)
            || string.IsNullOrWhiteSpace(value))
            return (null, null);
        if (MediaTypeHeaderValue.TryParse(value, out var parsed))
            return (parsed.MediaType, parsed.CharSet?.Trim('"'));
        return (value.Split(';')[0].Trim(), null);
    }
}
