using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Avalonia.Controls;

internal sealed class OperatingSystemEx
{
#if NET6_0_OR_GREATER
    [SupportedOSPlatformGuard("windows6.1")] // win7
    public static bool IsWindows() => OperatingSystem.IsWindowsVersionAtLeast(6, 1);
    [SupportedOSPlatformGuard("macos")]
    public static bool IsMacOS() => OperatingSystem.IsMacOS();
    [SupportedOSPlatformGuard("linux")]
    public static bool IsLinux() => OperatingSystem.IsLinux();
    [SupportedOSPlatformGuard("android")]
    public static bool IsAndroid() => OperatingSystem.IsAndroid();
    [SupportedOSPlatformGuard("ios")]
    public static bool IsIOS() => OperatingSystem.IsIOS();
    [SupportedOSPlatformGuard("browser")]
    public static bool IsBrowser() => OperatingSystem.IsBrowser();
    [SupportedOSPlatformGuard("ios")]
    public static bool IsIOSVersionAtLeast(int i, int i1) => OperatingSystem.IsIOSVersionAtLeast(i, i1);
    [SupportedOSPlatformGuard("macos")]
    public static bool IsMacOSVersionAtLeast(int i, int i1) => OperatingSystem.IsMacOSVersionAtLeast(i, i1);
    public static bool IsOSPlatform(string platform) => OperatingSystem.IsOSPlatform(platform);
#else
    [SupportedOSPlatformGuard("windows6.1")]
    public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    [SupportedOSPlatformGuard("macos")]
    public static bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    [SupportedOSPlatformGuard("linux")]
    public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    [SupportedOSPlatformGuard("android")]
    public static bool IsAndroid() => IsOSPlatform("ANDROID");
    [SupportedOSPlatformGuard("ios")]
    public static bool IsIOS() => IsOSPlatform("IOS");
    [SupportedOSPlatformGuard("browser")]
    public static bool IsBrowser() => IsOSPlatform("BROWSER");
    public static bool IsIOSVersionAtLeast(int i, int i1) => false;
    public static bool IsMacOSVersionAtLeast(int i, int i1) => IsMacOS() && i < 14;
    public static bool IsOSPlatform(string platform) => RuntimeInformation.IsOSPlatform(OSPlatform.Create(platform));
#endif
}
