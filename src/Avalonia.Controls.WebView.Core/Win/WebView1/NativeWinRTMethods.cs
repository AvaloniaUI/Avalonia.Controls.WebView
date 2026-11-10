using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Threading;
using Avalonia.Controls.Win.WebView1;

namespace Avalonia.Controls.Win;

[SupportedOSPlatform("windows")]
internal static unsafe partial class NativeWinRTMethods
{
    
    [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern int WindowsCreateString(ushort* sourceString, int length, IntPtr* hstring);

    [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern int WindowsDeleteString(IntPtr hstring);

    [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern char* WindowsGetStringRawBuffer(IntPtr hstring, uint* length);

    [DllImport("combase.dll")]
    private static extern void RoInitialize(RO_INIT_TYPE initType);
    [DllImport("combase.dll")]
    private static extern int RoActivateInstance(IntPtr activatableClassId, IntPtr* instance);
    [DllImport("api-ms-win-core-winrt-l1-1-0.dll")]
    private static extern int RoGetActivationFactory(IntPtr runtimeClassId, Guid* iid, IntPtr* factory);

    internal static T? CreateInstance<T>(string fullName)
    {
        using var s = new HStringInterop(fullName);
        EnsureRoInitialized();
        IntPtr pUnk;
        Marshal.ThrowExceptionForHR(RoActivateInstance(s.Handle, &pUnk));
        try
        {
            return ComInterfaceMarshaller<T>.ConvertToManaged(pUnk.ToPointer());
        }
        finally
        {
            ComInterfaceMarshaller<T>.Free(pUnk.ToPointer());
        }
    }

    internal static TFactory? CreateActivationFactory<TFactory>(string fullName)
    {
        using var s = new HStringInterop(fullName);
        EnsureRoInitialized();
        var guid = typeof(TFactory).GUID;
        IntPtr pUnk = default;
        Marshal.ThrowExceptionForHR(RoGetActivationFactory(s.Handle, &guid, &pUnk));
        try
        {
            return ComInterfaceMarshaller<TFactory>.ConvertToManaged(pUnk.ToPointer());
        }
        finally
        {
            ComInterfaceMarshaller<TFactory>.Free(pUnk.ToPointer());
        }
    }

    internal enum RO_INIT_TYPE
    {
        RO_INIT_SINGLETHREADED = 0, // Single-threaded application
        RO_INIT_MULTITHREADED = 1, // COM calls objects on any thread.
    }

    private static bool _initialized;

    private static void EnsureRoInitialized()
    {
        if (_initialized)
            return;
        RoInitialize(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA ?
            RO_INIT_TYPE.RO_INIT_SINGLETHREADED :
            RO_INIT_TYPE.RO_INIT_MULTITHREADED);
        _initialized = true;
    }
}
