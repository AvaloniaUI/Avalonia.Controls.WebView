using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Windows.Win32;
using Avalonia.Controls.Utils;
using Avalonia.Controls.Win.WebView2.Interop;

namespace Avalonia.Controls.Win.WebView2;

/// <summary>
/// Turns <see cref="WebResourceRequestedEventArgs.Response"/> into the WebView2 response.
/// </summary>
[SupportedOSPlatform("windows6.1")]
internal static class WebView2ResourceResponse
{
    /// <summary>
    /// Answers right away, or holds a WebView2 deferral until the handler's deferrals complete and the body is read.
    /// </summary>
    public static async void Answer(
        WebView2BaseAdapter adapter,
        ICoreWebView2WebResourceRequestedEventArgs e,
        WebResourceRequestedEventArgs? args,
        bool isCustomScheme,
        NativeHeadersCollection? headers)
    {
        // Only taken before an await yields, while WebView2 still waits for the handler.
        ICoreWebView2Deferral? deferral = null;
        try
        {
            if (args?.WaitForDeferralsAsync() is { IsCompleted: false } wait)
            {
                deferral = e.GetDeferral();
                await wait;
            }

            if (WebResourceResponseHelper.GetResponseOrNotFound(args?.Response, isCustomScheme) is not { } response)
                return;
            // Chromium only reads Content-Range for http and https, a short answer elsewhere would end the file.
            if (args is not null)
                response = WebResourceResponseHelper.ApplyRange(args.Request, response,
                    maxOpenEndedLength: args.Request.Uri.Scheme == Uri.UriSchemeHttp
                        || args.Request.Uri.Scheme == Uri.UriSchemeHttps ?
                        WebResourceResponseHelper.BufferedRangeLength :
                        long.MaxValue);

            var read = WebResourceResponseHelper.ReadContentAsync(response.Content);
            if (!read.IsCompleted)
                deferral ??= e.GetDeferral();
            Apply(adapter, e, response, (await read).Span);
        }
        catch (Exception ex)
        {
            LogError(ex);
        }
        finally
        {
            headers?.Dispose();
            WebResourceResponseHelper.DisposeContent(args?.Response);
            try
            {
                deferral?.Complete();
            }
            catch (Exception ex)
            {
                // The web view may be gone already.
                LogError(ex);
            }
        }
    }

    private static unsafe void Apply(
        WebView2BaseAdapter adapter,
        ICoreWebView2WebResourceRequestedEventArgs e,
        WebViewWebResourceResponse response,
        ReadOnlySpan<byte> content)
    {
        if (adapter.WebViewEnvironment is not { } environment)
            return;

        IntPtr stream;
        fixed (byte* ptr = content)
        {
            stream = (IntPtr)PInvoke.SHCreateMemStream(ptr, (uint)content.Length);
        }
        if (stream == IntPtr.Zero)
            throw new OutOfMemoryException("SHCreateMemStream failed.");

        try
        {
            var nativeResponse = environment.CreateWebResourceResponse(
                stream, response.StatusCode, WebResourceResponseHelper.GetReasonPhrase(response), GetHeaders(response));
            try
            {
                e.SetResponse(nativeResponse);
            }
            finally
            {
                Marshal.Release(nativeResponse);
            }
        }
        finally
        {
            Marshal.Release(stream);
        }
    }

    private static string GetHeaders(WebViewWebResourceResponse response)
    {
        var headers = new StringBuilder();
        foreach (var header in WebResourceResponseHelper.GetValidHeaders(response))
        {
            headers.Append(header.Key).Append(": ").Append(header.Value).Append("\r\n");
        }
        return headers.ToString();
    }

    private static void LogError(Exception ex) =>
        WebResourceResponseHelper.LogError("Failed to answer a web resource request: {Exception}", ex);
}
