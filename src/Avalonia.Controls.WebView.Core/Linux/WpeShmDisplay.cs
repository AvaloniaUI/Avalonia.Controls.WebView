using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls.Linux.Interop;

namespace Avalonia.Controls.Linux;

internal static unsafe class WpeShmDisplay
{
    private const int GObjectClassSlots = 17;
    private const int ConnectSlot = GObjectClassSlots;
    private const int CreateViewSlot = GObjectClassSlots + 1;
    private const int GetDrmDeviceSlot = GObjectClassSlots + 8;
    private const int CreateToplevelSlot = GObjectClassSlots + 11;

    private static IntPtr s_type;

    public static IntPtr Create()
    {
        if (s_type == IntPtr.Zero)
            s_type = RegisterType();

        return WpeInterop.g_object_new_with_properties(s_type, 0, [], []);
    }

    private static IntPtr RegisterType()
    {
        var parentType = WpePlatformInterop.wpe_display_get_type();
        GTypeQuery query;
        WpePlatformInterop.g_type_query(parentType, &query);
        if (query.Type == IntPtr.Zero)
            throw new InvalidOperationException("Failed to query the WPEDisplay type.");

        var type = WpePlatformInterop.g_type_register_static_simple(
            parentType, "AvaloniaWpeShmDisplay", query.ClassSize, &ClassInit, query.InstanceSize, IntPtr.Zero, 0);
        if (type == IntPtr.Zero)
            throw new InvalidOperationException("Failed to register the WPE SHM display type.");
        return type;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ClassInit(IntPtr klass, IntPtr classData)
    {
        var slots = (IntPtr*)klass;
        slots[ConnectSlot] = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr*, int>)&Connect;
        slots[CreateViewSlot] = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr>)&CreateView;
        slots[GetDrmDeviceSlot] = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr>)&GetDrmDevice;
        slots[CreateToplevelSlot] = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, uint, IntPtr>)&CreateToplevel;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int Connect(IntPtr display, IntPtr* error) => 1;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr CreateView(IntPtr display) =>
        WpePlatformViewBackend.NewWithDisplay(WpePlatformInterop.wpe_view_headless_get_type(), display);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr GetDrmDevice(IntPtr display) => IntPtr.Zero;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static IntPtr CreateToplevel(IntPtr display, uint maxViews) =>
        WpePlatformViewBackend.NewWithDisplay(WpePlatformInterop.wpe_toplevel_headless_get_type(), display);
}
