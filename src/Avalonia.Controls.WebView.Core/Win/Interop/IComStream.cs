using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.Interop;

#if COM_SOURCE_GEN
[GeneratedComInterface]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("0000000c-0000-0000-C000-000000000046")]
internal partial interface IComStream
{
    [PreserveSig]
    unsafe int Read(
        byte* pv, uint cb, uint* pcbRead);

    [PreserveSig]
    unsafe int Write(
        byte* pv, uint cb, uint* pcbWritten);

    [PreserveSig]
    int Seek(long dlibMove, int dwOrigin, out ulong plibNewPosition);

    [PreserveSig]
    int SetSize(ulong libNewSize);

    [PreserveSig]
    int CopyTo(IComStream pstm, ulong cb, out ulong pcbRead, out ulong pcbWritten);

    [PreserveSig]
    int Commit(int grfCommitFlags);

    [PreserveSig]
    int Revert();

    [PreserveSig]
    int LockRegion(ulong libOffset, ulong cb, int dwLockType);

    [PreserveSig]
    int UnlockRegion(ulong libOffset, ulong cb, int dwLockType);

    [PreserveSig]
    int Stat(IntPtr pstatstg, int grfStatFlag);

    [PreserveSig]
    int Clone(out IComStream ppstm);
}

// Based of https://github.com/dotnet/winforms/blob/f3e0d22d6c6020804f782f1c4c5810b3fb4f1993/src/System.Windows.Forms/System/Windows/Forms/ActiveX/DataStreamFromComStream.cs#L8
internal unsafe class DataStreamFromComStream(IComStream stream) : Stream
{
    private IComStream? _stream = stream;

    public override long Position
    {
        get => Seek(0, SeekOrigin.Current);
        set => Seek(value, SeekOrigin.Begin);
    }

    public override bool CanWrite => true;

    public override bool CanSeek => true;

    public override bool CanRead => true;

    public override long Length
    {
        get
        {
            long curPos = Position;
            long endPos = Seek(0, SeekOrigin.End);
            Position = curPos;
            return endPos - curPos;
        }
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesRead = 0;
        if (count > 0 && offset >= 0 && (count + offset) <= buffer.Length)
        {
            Span<byte> span = new(buffer, offset, count);
            bytesRead = Read(span);
        }

        return bytesRead;
    }

    public
#if NET6_0_OR_GREATER
        override
#endif
        int Read(Span<byte> buffer)
    {
        if (_stream is null)
            throw new ObjectDisposedException(nameof(DataStreamFromComStream));

        uint bytesRead = 0;
        if (!buffer.IsEmpty)
        {
            fixed (byte* ch = &buffer[0])
            {
                _stream.Read(ch, (uint)buffer.Length, &bytesRead);
            }
        }

        return (int)bytesRead;
    }

    public override int ReadByte()
    {
        Span<byte> buffer = stackalloc byte[1];
        int r = Read(buffer);
        return r == 0 ? -1 : buffer[0];
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if (_stream is null)
            throw new ObjectDisposedException(nameof(DataStreamFromComStream));

        _stream.Seek(offset, (int)origin, out var newPosition);
        return (long)newPosition;
    }

    public override void SetLength(long value)
    {
        if (_stream is null)
            throw new ObjectDisposedException(nameof(DataStreamFromComStream));

        _stream.SetSize((ulong)value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (count <= 0)
        {
            return;
        }

        if (offset >= 0 && (count + offset) <= buffer.Length)
        {
            ReadOnlySpan<byte> span = new(buffer, offset, count);
            Write(span);
            return;
        }

        throw new IOException();
    }

    public
#if NET6_0_OR_GREATER
        override
#endif
        void Write(ReadOnlySpan<byte> buffer)
    {
        if (_stream is null)
            throw new ObjectDisposedException(nameof(DataStreamFromComStream));

        if (buffer.IsEmpty)
        {
            return;
        }

        uint bytesWritten = 0;
        fixed (byte* b = &buffer[0])
        {
            _stream.Write(b, (uint)buffer.Length, &bytesWritten);
        }

        if (bytesWritten < buffer.Length)
        {
            throw new IOException();
        }
    }

    public override void WriteByte(byte value) => Write([value]);

    protected override void Dispose(bool disposing)
    {
        if (disposing && _stream is not null)
        {
            _stream.Commit(0);
        }

        _stream = null;
        base.Dispose(disposing);
    }
}
