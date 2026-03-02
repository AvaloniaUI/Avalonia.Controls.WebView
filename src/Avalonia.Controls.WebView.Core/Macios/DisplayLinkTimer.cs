using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using Avalonia.Controls.Macios.Interop;

namespace Avalonia.Controls.Macios;

/// <summary>
/// Uses CVDisplayLink to drive frame updates at the display's refresh rate.
/// Only fires a callback when a new frame has been explicitly requested.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class DisplayLinkTimer : IDisposable
{
    private IntPtr _displayLink;
    private readonly Action _callback;
    private int _frameRequested;
    private GCHandle _selfHandle;
    private bool _disposed;

    private static readonly unsafe IntPtr s_outputCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr*, IntPtr*, IntPtr, IntPtr, IntPtr, int>)&DisplayLinkOutputCallback);

    public DisplayLinkTimer(Action callback)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        _selfHandle = GCHandle.Alloc(this);

        unsafe
        {
            IntPtr link;
            var result = CVDisplayLinkCreateWithActiveCGDisplays(&link);
            if (result != 0)
                throw new InvalidOperationException($"CVDisplayLinkCreateWithActiveCGDisplays failed: {result}");

            _displayLink = link;

            result = CVDisplayLinkSetOutputCallback(_displayLink, s_outputCallback, GCHandle.ToIntPtr(_selfHandle));
            if (result != 0)
                throw new InvalidOperationException($"CVDisplayLinkSetOutputCallback failed: {result}");

            result = CVDisplayLinkStart(_displayLink);
            if (result != 0)
                throw new InvalidOperationException($"CVDisplayLinkStart failed: {result}");
        }

        // Request the initial frame
        RequestNextFrame();
    }

    /// <summary>
    /// Request that the next display link tick fires the callback.
    /// </summary>
    public void RequestNextFrame()
    {
        Interlocked.Exchange(ref _frameRequested, 1);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        var displayLink = Interlocked.Exchange(ref _displayLink, IntPtr.Zero);
        if (displayLink != IntPtr.Zero)
        {
            CVDisplayLinkStop(displayLink);
            CVDisplayLinkRelease(displayLink);
        }

        if (_selfHandle.IsAllocated)
            _selfHandle.Free();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static unsafe int DisplayLinkOutputCallback(
        IntPtr displayLink,
        IntPtr* inNow,
        IntPtr* inOutputTime,
        IntPtr flagsIn,
        IntPtr flagsOut,
        IntPtr displayLinkContext)
    {
        if (displayLinkContext == IntPtr.Zero)
            return 0;

        var handle = GCHandle.FromIntPtr(displayLinkContext);
        if (handle.Target is not DisplayLinkTimer timer)
            return 0;

        if (Interlocked.CompareExchange(ref timer._frameRequested, 0, 1) == 1)
        {
            timer._callback();
        }

        return 0;
    }

    private const string CoreVideo = "/System/Library/Frameworks/CoreVideo.framework/CoreVideo";

    [DllImport(CoreVideo)]
    private static extern unsafe int CVDisplayLinkCreateWithActiveCGDisplays(IntPtr* displayLinkOut);

    [DllImport(CoreVideo)]
    private static extern int CVDisplayLinkSetOutputCallback(IntPtr displayLink, IntPtr callback, IntPtr userInfo);

    [DllImport(CoreVideo)]
    private static extern int CVDisplayLinkStart(IntPtr displayLink);

    [DllImport(CoreVideo)]
    private static extern int CVDisplayLinkStop(IntPtr displayLink);

    [DllImport(CoreVideo)]
    private static extern void CVDisplayLinkRelease(IntPtr displayLink);
}
