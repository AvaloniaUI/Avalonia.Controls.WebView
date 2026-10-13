using System;
using System.Runtime.InteropServices;
using Avalonia.Controls.Linux.Interop;
using Avalonia.Platform;

namespace Avalonia.Controls.Linux;

internal static unsafe class WpeRuntime
{
    private const string LibWpeWebKit = "libWPEWebKit-2.0.so.1";
    private const string LibWpe = "libwpe-1.0.so.1";
    private const string LibWpeBackendFdo = "libWPEBackend-fdo-1.0.so.1";

    private static readonly Lazy<(bool Legacy, bool Platform)> s_apis = new(ProbeApis);

    private static WpeApi? s_lockedApi;
    private static IntPtr s_display;
    private static bool s_displayShmOnly;

    public static bool IsAvailable => IsLegacyAvailable || IsPlatformAvailable;
    public static bool IsLegacyAvailable => s_apis.Value.Legacy;
    public static bool IsPlatformAvailable => s_apis.Value.Platform;

    private static (bool Legacy, bool Platform) ProbeApis()
    {
        try
        {
            WpeLegacyViewBackend.EnsureBackendLibrary();
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }

        if (!NativeLibrary.TryLoad(LibWpeWebKit, out var webKit))
            return (false, false);

        var platform = NativeLibrary.TryGetExport(webKit, "webkit_web_view_get_wpe_view", out _)
                       && NativeLibrary.TryGetExport(webKit, "wpe_display_headless_new", out _);

        var legacy = NativeLibrary.TryGetExport(webKit, "webkit_web_view_backend_new", out _)
                     && NativeLibrary.TryLoad(LibWpe, out _)
                     && NativeLibrary.TryLoad(LibWpeBackendFdo, out _);

        return (legacy, platform);
    }

    public static WpeApi ResolveApi(WpeApi requested)
    {
        var resolved = requested == WpeApi.Auto ? s_lockedApi ?? ResolveAuto() : requested;

        if (s_lockedApi is { } locked && locked != resolved)
            throw new InvalidOperationException(
                $"WPE WebKit already uses the {locked} API in this process; the {resolved} API cannot be used.");

        if (resolved == WpeApi.Legacy && !IsLegacyAvailable)
            throw new InvalidOperationException(
                "The WPE WebKit legacy API (libwpe and WPEBackend-fdo) is not available.");

        if (resolved == WpeApi.Platform && !IsPlatformAvailable)
            throw new InvalidOperationException("The WPE WebKit WPEPlatform API is not available.");

        s_lockedApi = resolved;
        return resolved;
    }

    private static WpeApi ResolveAuto()
    {
        if (IsPlatformAvailable && IsVersionAtLeast(2, 54))
            return WpeApi.Platform;
        if (IsLegacyAvailable)
            return WpeApi.Legacy;
        if (IsPlatformAvailable)
            return WpeApi.Platform;
        throw new InvalidOperationException("WPE WebKit libraries are not installed.");
    }

    private static bool IsVersionAtLeast(uint major, uint minor)
    {
        var currentMajor = WpeInterop.webkit_get_major_version();
        var currentMinor = WpeInterop.webkit_get_minor_version();
        return currentMajor > major || (currentMajor == major && currentMinor >= minor);
    }

    public static IntPtr GetPlatformDisplay(bool shmOnly)
    {
        if (s_display != IntPtr.Zero)
        {
            if (s_displayShmOnly != shmOnly)
                throw new InvalidOperationException(
                    $"WPE WebKit already uses the {(s_displayShmOnly ? WpeRenderingMode.Shm : WpeRenderingMode.Auto)} rendering mode in this process.");
            return s_display;
        }

        var display = shmOnly ? WpeShmDisplay.Create() : WpePlatformInterop.wpe_display_headless_new();
        if (display == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create the WPE headless display.");

        IntPtr error = IntPtr.Zero;
        if (!WpePlatformInterop.wpe_display_connect(display, &error))
        {
            var message = TakeErrorMessage(error);
            WpeInterop.g_object_unref(display);
            throw new InvalidOperationException($"Failed to connect the WPE headless display: {message}");
        }

        s_display = display;
        s_displayShmOnly = shmOnly;
        return display;
    }

    public static string? TakeErrorMessage(IntPtr error)
    {
        if (error == IntPtr.Zero)
            return null;
        var message = Marshal.PtrToStringUTF8(((GError*)error)->Message);
        WpePlatformInterop.g_error_free(error);
        return message;
    }
}
