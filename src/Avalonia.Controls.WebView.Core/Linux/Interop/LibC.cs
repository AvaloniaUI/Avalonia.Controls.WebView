using System.Runtime.InteropServices;

namespace Avalonia.Controls.Linux.Interop;

/// <summary>
/// Interop for standard C library functions.
/// </summary>
internal static unsafe partial class LibC
{
    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int setenv(string name, string value, int overwrite);

    [LibraryImport("libc")]
    public static partial int poll(GPollFD* fds, nint nfds, int timeout);

    [LibraryImport("libc")]
    public static partial int eventfd(uint initval, int flags);

    [LibraryImport("libc")]
    public static partial nint write(int fd, void* buf, nint count);

    [LibraryImport("libc")]
    public static partial int close(int fd);
}

/// <summary>
/// Minimal P/Invoke for wpe_loader_init from libwpe-1.0.
/// Kept in a separate class so the JIT does not trigger loading of libWPEWebKit
/// or libWPEBackend-fdo when we only need to initialize the loader.
/// </summary>
internal static partial class WpeLoader
{
    [LibraryImport("libwpe-1.0.so.1", StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool wpe_loader_init(string implLibraryName);

    [LibraryImport("libwpe-1.0.so.1")]
    public static partial nint wpe_loader_get_loaded_implementation_library_name();
}
