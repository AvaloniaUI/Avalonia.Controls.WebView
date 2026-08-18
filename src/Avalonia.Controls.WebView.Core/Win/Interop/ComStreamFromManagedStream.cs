using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;

namespace Avalonia.Controls.Win.Interop;

/// <summary>
/// Exposes a managed <see cref="Stream"/> as a COM <see cref="IComStream"/> for WebView2 APIs.
/// </summary>
[GeneratedComClass]
[SupportedOSPlatform("windows6.1")]
internal partial class ComStreamFromManagedStream(Stream stream) : IComStream
{
    private Stream? _stream = stream;

    public unsafe int Read(byte* pv, uint cb, uint* pcbRead)
    {
        if (_stream is null)
            return unchecked((int)0x80004005); // E_FAIL

        var buffer = new byte[cb];
        var read = _stream.Read(buffer, 0, (int)cb);
        if (read > 0)
            Marshal.Copy(buffer, 0, (IntPtr)pv, read);
        if (pcbRead != null)
            *pcbRead = (uint)read;
        return 0;
    }

    public unsafe int Write(byte* pv, uint cb, uint* pcbWritten)
    {
        if (_stream is null)
            return unchecked((int)0x80004005);

        var buffer = new byte[cb];
        Marshal.Copy((IntPtr)pv, buffer, 0, (int)cb);
        _stream.Write(buffer, 0, (int)cb);
        if (pcbWritten != null)
            *pcbWritten = cb;
        return 0;
    }

    public int Seek(long dlibMove, int dwOrigin, out ulong plibNewPosition)
    {
        plibNewPosition = 0;
        if (_stream is null)
            return unchecked((int)0x80004005);

        plibNewPosition = (ulong)_stream.Seek(dlibMove, (SeekOrigin)dwOrigin);
        return 0;
    }

    public int SetSize(ulong libNewSize)
    {
        if (_stream is null)
            return unchecked((int)0x80004005);
        _stream.SetLength((long)libNewSize);
        return 0;
    }

    public int CopyTo(IComStream pstm, ulong cb, out ulong pcbRead, out ulong pcbWritten)
    {
        pcbRead = 0;
        pcbWritten = 0;
        return unchecked((int)0x80004001); // E_NOTIMPL
    }

    public int Commit(int grfCommitFlags)
    {
        _stream?.Flush();
        return 0;
    }

    public int Revert() => 0;

    public int LockRegion(ulong libOffset, ulong cb, int dwLockType) =>
        unchecked((int)0x80004001);

    public int UnlockRegion(ulong libOffset, ulong cb, int dwLockType) =>
        unchecked((int)0x80004001);

    public int Stat(IntPtr pstatstg, int grfStatFlag) =>
        unchecked((int)0x80004001);

    public int Clone(out IComStream ppstm)
    {
        ppstm = null!;
        return unchecked((int)0x80004001);
    }
}
