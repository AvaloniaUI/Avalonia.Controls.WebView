#if ANDROID

using System;
using System.IO;
using Avalonia.Controls.Utils;

namespace Avalonia.Controls.Android;

/// <summary>
/// A response body for Android WebView, which fails the request on an IOException but crashes on any other.
/// </summary>
/// <param name="stream">The body.</param>
/// <param name="size">
/// The body's length, or 0 if unknown. WebView sends it as Content-Length of a 200 response.
/// </param>
internal sealed class ResponseInputStream(Stream stream, int size) : Java.IO.InputStream
{
    private int _read;

    public override int Available() => Math.Max(0, size - _read);

    public override int Read()
    {
        try
        {
            var value = stream.ReadByte();
            if (value >= 0)
                _read++;
            return value;
        }
        catch (Exception ex)
        {
            throw ToIOException(ex);
        }
    }

    public override int Read(byte[]? buffer, int offset, int length)
    {
        if (length == 0)
            return 0;
        try
        {
            var read = stream.Read(buffer!, offset, length);
            _read += read;
            return read == 0 ? -1 : read;
        }
        catch (Exception ex)
        {
            throw ToIOException(ex);
        }
    }

    public override void Close()
    {
        try
        {
            stream.Dispose();
        }
        catch (Exception ex)
        {
            WebResourceResponseHelper.LogError("Failed to dispose a web resource response: {Exception}", ex);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Close();
        base.Dispose(disposing);
    }

    private static Java.IO.IOException ToIOException(Exception ex)
    {
        WebResourceResponseHelper.LogError("Failed to read a web resource response: {Exception}", ex);
        return ex as Java.IO.IOException ?? new Java.IO.IOException(ex.Message);
    }
}

#endif
