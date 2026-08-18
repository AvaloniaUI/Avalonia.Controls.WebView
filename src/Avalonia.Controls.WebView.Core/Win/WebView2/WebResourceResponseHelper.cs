using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Avalonia.Controls.Win.WebView2.Interop;

namespace Avalonia.Controls.Win.WebView2;

[SupportedOSPlatform("windows6.1")]
internal static unsafe class WebResourceResponseHelper
{
    [DllImport("shlwapi.dll", ExactSpelling = true)]
    private static extern IntPtr SHCreateMemStream(byte* pInit, uint cbInit);

    public static IntPtr CreateNativeResponse(
        ICoreWebView2Environment environment,
        WebViewWebResourceResponse response)
    {
        var headers = BuildHeaderString(response);
        using var ms = response.Content as MemoryStream ?? CopyToMemoryStream(response.Content);
        var bytes = ms.ToArray();
        IntPtr nativeStream;
        if (bytes.Length == 0)
        {
            nativeStream = SHCreateMemStream(null, 0);
        }
        else
        {
            fixed (byte* ptr = bytes)
            {
                nativeStream = SHCreateMemStream(ptr, (uint)bytes.Length);
            }
        }

        if (nativeStream == IntPtr.Zero)
            throw new InvalidOperationException("SHCreateMemStream failed.");

        try
        {
            return environment.CreateWebResourceResponse(
                nativeStream,
                response.StatusCode,
                response.ReasonPhrase,
                headers);
        }
        finally
        {
            Marshal.Release(nativeStream);
        }
    }

    private static string BuildHeaderString(WebViewWebResourceResponse response)
    {
        var sb = new StringBuilder();
        var hasContentType = false;
        var hasContentLength = false;
        foreach (var pair in response.Headers)
        {
            if (pair.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                hasContentType = true;
            if (pair.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                hasContentLength = true;
            sb.Append(pair.Key).Append(": ").Append(pair.Value).Append("\r\n");
        }

        if (!hasContentType)
            sb.Append("Content-Type: ").Append(response.ContentType).Append("\r\n");

        if (!hasContentLength && response.Content.CanSeek)
            sb.Append("Content-Length: ").Append(response.Content.Length).Append("\r\n");

        return sb.ToString();
    }

    private static MemoryStream CopyToMemoryStream(Stream source)
    {
        if (source.CanSeek)
            source.Position = 0;
        var ms = new MemoryStream();
        source.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }
}
