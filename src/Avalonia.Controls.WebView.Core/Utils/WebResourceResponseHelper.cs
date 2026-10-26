using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
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
    /// <summary>
    /// The most an open-ended range gets on WebView2, which reads the whole body into memory.
    /// </summary>
    public const long BufferedRangeLength = 4 * 1024 * 1024;

    private const string BytesUnit = "bytes";

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
    /// Answers a single byte range of a seekable body with 206, as media elements request ranges to play and seek.
    /// </summary>
    /// <remarks>
    /// The body is what's left from the stream's position. Other responses and requests are returned as they are,
    /// except that a seekable 200 response advertises Accept-Ranges.
    /// Android WebView skips to the first byte of an explicit range itself,
    /// so with <paramref name="engineSkipsToFirstByte"/> the body keeps those bytes.
    /// An open-ended range, as media elements send, gets at most <paramref name="maxOpenEndedLength"/> bytes,
    /// for engines that read the whole body into memory; the client asks for the rest.
    /// </remarks>
    public static WebViewWebResourceResponse ApplyRange(
        WebViewWebResourceRequest request, WebViewWebResourceResponse response, bool engineSkipsToFirstByte = false,
        long maxOpenEndedLength = long.MaxValue)
    {
        if (response is not { StatusCode: (int)HttpStatusCode.OK, Content: { CanSeek: true } content }
            || request.Method != HttpMethod.Get
            || response.Headers.ContainsKey(HttpHeaderNames.ContentRange)
            || response.Headers.ContainsKey(HttpHeaderNames.ContentEncoding))
            return response;

        long start, length;
        try
        {
            start = content.Position;
            length = content.Length - start;
        }
        catch (NotSupportedException)
        {
            return response;
        }

        response.Headers.TryAdd(HttpHeaderNames.AcceptRanges, BytesUnit);
        if (GetRequestHeader(request, HttpHeaderNames.Range) is not { } value
            || !IsIfRangeSatisfied(request, response)
            || ParseRange(value) is not { } range)
            return response;

        var (first, last) = GetBounds(range, length, maxOpenEndedLength);
        var satisfiable = first <= last;
        // Suffix ranges, "bytes=-500", aren't skipped.
        var skipped = engineSkipsToFirstByte && range.From is not null ? first : 0;
        var partial = new WebViewWebResourceResponse
        {
            StatusCode = (int)(satisfiable ? HttpStatusCode.PartialContent : HttpStatusCode.RequestedRangeNotSatisfiable),
            Content = satisfiable ? new RangeStream(content, start + first - skipped, last - first + 1 + skipped) : null
        };
        foreach (var header in response.Headers)
        {
            if (!string.Equals(header.Key, HttpHeaderNames.ContentLength, StringComparison.OrdinalIgnoreCase))
                partial.Headers[header.Key] = header.Value;
        }
        if (!satisfiable)
        {
            partial.Headers[HttpHeaderNames.ContentRange] = new ContentRangeHeaderValue(length).ToString();
            DisposeContent(response);
            return partial;
        }
        partial.Headers[HttpHeaderNames.ContentRange] = new ContentRangeHeaderValue(first, last, length).ToString();
        partial.Headers[HttpHeaderNames.ContentLength] = (last - first + 1).ToString(CultureInfo.InvariantCulture);
        return partial;
    }

    // Native lookups aren't case-insensitive everywhere.
    private static string? GetRequestHeader(WebViewWebResourceRequest request, string name)
    {
        if (request.Headers.TryGetValue(name, out var value))
            return value;
        foreach (var header in request.Headers)
        {
            if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
                return header.Value;
        }
        return null;
    }

    // RFC 9110 13.1.5: the range only applies to the representation the client already has.
    private static bool IsIfRangeSatisfied(WebViewWebResourceRequest request, WebViewWebResourceResponse response)
    {
        if (GetRequestHeader(request, HttpHeaderNames.IfRange)?.Trim() is not { } ifRange)
            return true;
        if (!RangeConditionHeaderValue.TryParse(ifRange, out var condition) || condition.EntityTag is { IsWeak: true })
            return false;
        var validatorName = condition.EntityTag is null ? HttpHeaderNames.LastModified : HttpHeaderNames.ETag;
        return response.Headers.TryGetValue(validatorName, out var validator) && validator.Trim() == ifRange;
    }

    /// <summary>
    /// Parses a Range header with a single byte range, or returns null to ignore it and send the whole body.
    /// Several ranges would need a multipart body.
    /// </summary>
    internal static RangeItemHeaderValue? ParseRange(string value) =>
        RangeHeaderValue.TryParse(value, out var header)
        && string.Equals(header.Unit, BytesUnit, StringComparison.OrdinalIgnoreCase)
        && header.Ranges.Count == 1 ?
            header.Ranges.First() :
            null;

    /// <summary>
    /// The first and last byte of the range in a body of <paramref name="length"/> bytes.
    /// An unsatisfiable range gives first &gt; last.
    /// </summary>
    internal static (long First, long Last) GetBounds(
        RangeItemHeaderValue range, long length, long maxOpenEndedLength = long.MaxValue)
    {
        if (range.From is not { } first)
        {
            var suffix = range.To ?? 0;
            return suffix > 0 && length > 0 ? (Math.Max(0, length - suffix), length - 1) : (0, -1);
        }
        if (first >= length)
            return (0, -1);
        var last = range.To ?? (length - first > maxOpenEndedLength ? first + maxOpenEndedLength - 1 : length - 1);
        return (first, Math.Min(last, length - 1));
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
