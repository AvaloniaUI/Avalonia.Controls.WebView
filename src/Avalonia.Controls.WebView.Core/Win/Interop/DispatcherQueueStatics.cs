using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

// ReSharper disable InconsistentNaming

namespace Avalonia.Controls.Win.Interop;

[SupportedOSPlatform("windows")]
internal static partial class DispatcherQueueStatics
{
    private static readonly IDispatcherQueueStatics? s_statics;

    static DispatcherQueueStatics()
    {
        s_statics =
            NativeWinRTMethods.CreateActivationFactory<IDispatcherQueueStatics>("Windows.System.DispatcherQueue");
    }

#if NET6_0 // unnecessary in .NET 7+ or .NET 5
    [UnconditionalSuppressMessage("Trimming", "IL2050")]
#endif
    public static unsafe IDispatcherQueue GetOrCreateOnCurrentThread()
    {
        var statics = s_statics ??
                      throw new InvalidOperationException("Failed to get DispatcherQueueStatics activation factory.");
        if (statics.GetForCurrentThread() is { } dispatcherQueue)
        {
            return dispatcherQueue;
        }

        var options = new DispatcherQueueOptions
        {
            dwSize = sizeof(DispatcherQueueOptions),
            threadType = DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_CURRENT,
            apartmentType = DISPATCHERQUEUE_THREAD_APARTMENTTYPE.DQTAT_COM_STA
        };

        CreateDispatcherQueueController(options, out var controller);
        return controller.GetDispatcherQueue();
    }

    [LibraryImport("coremessaging.dll")]
    private static partial void CreateDispatcherQueueController(DispatcherQueueOptions options,
        out IDispatcherQueueController dispatcherQueueController);

    private enum DISPATCHERQUEUE_THREAD_APARTMENTTYPE
    {
        DQTAT_COM_NONE,
        DQTAT_COM_ASTA,
        DQTAT_COM_STA
    }

    private enum DISPATCHERQUEUE_THREAD_TYPE
    {
        DQTYPE_THREAD_DEDICATED = 1,
        DQTYPE_THREAD_CURRENT
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DispatcherQueueOptions
    {
        public int dwSize;
        [MarshalAs(UnmanagedType.I4)] public DISPATCHERQUEUE_THREAD_TYPE threadType;
        [MarshalAs(UnmanagedType.I4)] public DISPATCHERQUEUE_THREAD_APARTMENTTYPE apartmentType;
    }
}
