using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Controls.Utils;

/// <summary>
/// A read-only window of a seekable stream, which it disposes.
/// </summary>
internal sealed class RangeStream(Stream inner, long start, long length) : Stream
{
    private long _remaining = length;
    private bool _positioned;

    /// <summary>
    /// The bytes left to read.
    /// </summary>
    public long Remaining => _remaining;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        if (_remaining <= 0)
            return 0;
        EnsurePositioned();
        var read = inner.Read(buffer[..(int)Math.Min(buffer.Length, _remaining)]);
        _remaining -= read;
        return read;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_remaining <= 0)
            return 0;
        EnsurePositioned();
        var read = await inner.ReadAsync(buffer[..(int)Math.Min(buffer.Length, _remaining)], cancellationToken)
            .ConfigureAwait(false);
        _remaining -= read;
        return read;
    }

    // On the first read, so creating the window can't fail.
    private void EnsurePositioned()
    {
        if (_positioned)
            return;
        inner.Position = start;
        _positioned = true;
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            inner.Dispose();
        base.Dispose(disposing);
    }
}
