using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls.Linux.Interop;
using Avalonia.Threading;

namespace Avalonia.Controls.Linux;

/// <summary>
/// Integrates the GLib main context with the Avalonia UI thread using the proper
/// prepare → query → poll → check → dispatch cycle.
///
/// A background thread performs the blocking poll() on GLib's file descriptors,
/// then posts check + dispatch back to the Avalonia UI thread. This avoids the
/// fixed-interval DispatcherTimer and gives event-driven, low-latency processing.
///
/// Reference-counted — multiple WPE views share one integration instance.
/// </summary>
internal static unsafe class WpeGLibIntegration
{
    private const ushort PollIn = 1; // POLLIN

    private static int s_refCount;
    private static IntPtr s_mainContext;
    private static Thread? s_pollThread;
    private static volatile bool s_running;

    // Shutdown signaling via eventfd — added to the poll set so the poll thread
    // wakes immediately when Stop() is called, regardless of GLib timeout.
    private static int s_shutdownFd = -1;

    // Unmanaged buffer for GPollFD entries returned by g_main_context_query.
    // Only accessed sequentially: UI thread writes (prepare/query), then poll thread
    // reads (poll), then UI thread reads (check/dispatch), then repeat.
    private static GPollFD* s_fds;
    private static int s_fdsCapacity;
    private static int s_nFds;
    private static int s_timeout;
    private static int s_maxPriority;

    // Separate unmanaged buffer used by the poll thread. Sized to s_fdsCapacity + 1
    // (extra slot for the shutdown eventfd). Avoids stackalloc in a loop.
    private static GPollFD* s_pollBuf;

    // Handshake: UI thread sets this after prepare+query; poll thread waits on it.
    private static readonly ManualResetEventSlim s_pollReady = new(false);

#pragma warning disable CA1416 // Platform compatibility — this class is only used on Linux
    public static void Start()
    {
        Dispatcher.UIThread.VerifyAccess();

        s_refCount++;
        if (s_refCount > 1)
            return;

        s_mainContext = WpeInterop.g_main_context_ref_thread_default();
        s_fdsCapacity = 64;
        s_fds = (GPollFD*)NativeMemory.Alloc((nuint)(s_fdsCapacity * sizeof(GPollFD)));
        s_pollBuf = (GPollFD*)NativeMemory.Alloc((nuint)((s_fdsCapacity + 1) * sizeof(GPollFD)));
        s_shutdownFd = LibC.eventfd(0, 0);
        s_running = true;

        s_pollThread = new Thread(PollThreadProc)
        {
            IsBackground = true,
            Name = "GLib-Poll"
        };
        s_pollThread.Start();

        // Kick off the first iteration cycle.
        PrepareAndQuery();
    }

    public static void Stop()
    {
        Dispatcher.UIThread.VerifyAccess();

        s_refCount--;
        if (s_refCount > 0)
            return;

        s_running = false;

        // Wake the poll thread if it is blocked in poll() or waiting on s_pollReady.
        if (s_shutdownFd >= 0)
        {
            ulong val = 1;
            LibC.write(s_shutdownFd, &val, sizeof(ulong));
        }

        s_pollReady.Set();

        if (s_pollThread != null)
        {
            s_pollThread.Join();
            s_pollThread = null;
        }

        if (s_shutdownFd >= 0)
        {
            LibC.close(s_shutdownFd);
            s_shutdownFd = -1;
        }

        if (s_mainContext != IntPtr.Zero)
        {
            WpeInterop.g_main_context_unref(s_mainContext);
            s_mainContext = IntPtr.Zero;
        }

        if (s_fds != null)
        {
            NativeMemory.Free(s_fds);
            s_fds = null;
        }

        if (s_pollBuf != null)
        {
            NativeMemory.Free(s_pollBuf);
            s_pollBuf = null;
        }
    }
#pragma warning restore CA1416

    /// <summary>
    /// Runs on the UI thread. Acquires the GLib context, calls prepare + query to
    /// collect the file descriptors and timeout, then signals the poll thread.
    /// </summary>
    private static void PrepareAndQuery()
    {
        if (!s_running)
            return;

        if (!WpeInterop.g_main_context_acquire(s_mainContext))
        {
            // Another thread owns the context — retry on next dispatcher cycle.
            Dispatcher.UIThread.Post(PrepareAndQuery, DispatcherPriority.Background);
            return;
        }

        int priority;
        WpeInterop.g_main_context_prepare(s_mainContext, &priority);

        int timeout;
        int nFds = WpeInterop.g_main_context_query(s_mainContext, priority, &timeout, s_fds, s_fdsCapacity);

        // Grow both buffers if needed, then re-query.
        if (nFds > s_fdsCapacity)
        {
            s_fdsCapacity = nFds + 16;
            NativeMemory.Free(s_fds);
            NativeMemory.Free(s_pollBuf);
            s_fds = (GPollFD*)NativeMemory.Alloc((nuint)(s_fdsCapacity * sizeof(GPollFD)));
            s_pollBuf = (GPollFD*)NativeMemory.Alloc((nuint)((s_fdsCapacity + 1) * sizeof(GPollFD)));
            nFds = WpeInterop.g_main_context_query(s_mainContext, priority, &timeout, s_fds, s_fdsCapacity);
        }

        s_nFds = nFds;
        s_timeout = timeout;
        s_maxPriority = priority;

        // Hand off to the poll thread.
        s_pollReady.Set();
    }

    /// <summary>
    /// Background thread that blocks in poll() on GLib's file descriptors.
    /// When poll returns (event or timeout), posts check + dispatch to the UI thread.
    /// </summary>
    private static void PollThreadProc()
    {
        while (s_running)
        {
#pragma warning disable CA1416 // Platform compatibility — this class is only used on Linux
            s_pollReady.Wait();
#pragma warning restore CA1416
            s_pollReady.Reset();

            if (!s_running)
                break;

            int nFds = s_nFds;
            int timeout = s_timeout;

            // Build the poll set in s_pollBuf: GLib FDs + our shutdown eventfd.
            int totalFds = nFds + 1;
            Unsafe.CopyBlock(s_pollBuf, s_fds, (uint)(nFds * sizeof(GPollFD)));
            s_pollBuf[nFds] = new GPollFD { Fd = s_shutdownFd, Events = PollIn, Revents = 0 };

            LibC.poll(s_pollBuf, totalFds, timeout);

            // Copy the revents back into the shared buffer (only GLib FDs).
            Unsafe.CopyBlock(s_fds, s_pollBuf, (uint)(nFds * sizeof(GPollFD)));

            if (!s_running)
                break;

            // Post check + dispatch to the UI thread.
            Dispatcher.UIThread.Post(CheckAndDispatch, DispatcherPriority.Render);
        }
    }

    /// <summary>
    /// Runs on the UI thread. Passes poll results to GLib via check, dispatches
    /// ready sources, releases the context, and starts the next cycle.
    /// </summary>
    private static void CheckAndDispatch()
    {
        if (!s_running)
            return;

        WpeInterop.g_main_context_check(s_mainContext, s_maxPriority, s_fds, s_nFds);
        WpeInterop.g_main_context_dispatch(s_mainContext);
        WpeInterop.g_main_context_release(s_mainContext);

        // Immediately begin the next iteration.
        PrepareAndQuery();
    }
}
