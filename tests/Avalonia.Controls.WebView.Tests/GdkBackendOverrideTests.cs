using System;
using System.Runtime.InteropServices;
using Avalonia.Controls.Gtk;
using Avalonia.Controls.Linux.Interop;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

public class GdkBackendOverrideTests
{
    private const string EnvVar = "GDK_BACKEND";

    [Fact]
    public void Wayland_Is_Overridden_And_Restored()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "Linux-only: exercises libc setenv/getenv");

        using var _ = SaveEnv();
        SetEnv("wayland");

        using (var scope = AvaloniaGtk.EnsureX11GdkBackendForGtkInit())
        {
            AssertEnv("x11");
        }

        AssertEnv("wayland");
    }

    [Fact]
    public void Unset_Is_Overridden_And_Restored_To_Unset()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "Linux-only: exercises libc setenv/getenv");

        using var _ = SaveEnv();
        SetEnv(null);

        using (var scope = AvaloniaGtk.EnsureX11GdkBackendForGtkInit())
        {
            AssertEnv("x11");
        }

        AssertEnv(null);
    }

    [Fact]
    public void Already_X11_Is_NoOp()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "Linux-only: exercises libc setenv/getenv");

        using var _ = SaveEnv();
        SetEnv("x11");

        using (var scope = AvaloniaGtk.EnsureX11GdkBackendForGtkInit())
        {
            AssertEnv("x11");
        }

        AssertEnv("x11");
    }

    [Fact]
    public void Explicit_Other_Backend_Is_Not_Overridden()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "Linux-only: exercises libc setenv/getenv");

        using var _ = SaveEnv();
        SetEnv("broadway");

        using (var scope = AvaloniaGtk.EnsureX11GdkBackendForGtkInit())
        {
            AssertEnv("broadway");
        }

        AssertEnv("broadway");
    }

    [Fact]
    public void Nested_Scopes_Restore_Only_On_Last_Dispose()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "Linux-only: exercises libc setenv/getenv");

        using var _ = SaveEnv();
        SetEnv("wayland");

        using (var outer = AvaloniaGtk.EnsureX11GdkBackendForGtkInit())
        {
            AssertEnv("x11");

            using (var inner = AvaloniaGtk.EnsureX11GdkBackendForGtkInit())
            {
                AssertEnv("x11");
            }

            // outer still active — must not have restored yet
            AssertEnv("x11");
        }

        AssertEnv("wayland");
    }

    private static void SetEnv(string? value)
    {
        if (value is null)
            LibC.unsetenv(EnvVar);
        else
            LibC.setenv(EnvVar, value, 1);
        Environment.SetEnvironmentVariable(EnvVar, value);
    }

    private static string? GetLibCEnv(string name)
    {
        var ptr = LibC.getenv(name);
        return ptr == 0 ? null : Marshal.PtrToStringUTF8(ptr);
    }

    // Asserts both the managed env cache AND libc's environ — the latter is
    // what gtk_init reads via getenv, so it's the layer that actually decides
    // whether the override is effective.
    private static void AssertEnv(string? expected)
    {
        Assert.Equal(expected, Environment.GetEnvironmentVariable(EnvVar));
        Assert.Equal(expected, GetLibCEnv(EnvVar));
    }

    private static IDisposable SaveEnv()
    {
        var original = Environment.GetEnvironmentVariable(EnvVar);
        return new RestoreOnDispose(() => SetEnv(original));
    }

    private sealed class RestoreOnDispose(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}
