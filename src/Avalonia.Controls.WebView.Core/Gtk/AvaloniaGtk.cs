using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Logging;

namespace Avalonia.Controls.Gtk;

internal static class AvaloniaGtk
{
    static AvaloniaGtk()
    {
#if NET6_0_OR_GREATER
        var map = new Dictionary<string, string[]>
        {
            [GtkInterop.LibGtk] = ["libgtk-3.so.0", "libgtk-3.so"],
            [GtkInterop.LibGdk] = ["libgdk-3.so.0", "libgdk-3.so"],
            [GtkInterop.LibGLib] = ["libglib-2.0.so.0", "libglib-2.0.so"],
            [GtkInterop.LibGObject] = ["libgobject-2.0.so.0", "libgobject-2.0.so"],
            [GtkInterop.LibGio] = ["libgio-2.0.so.0", "libgio-2.0.so"],
            [GtkInterop.LibWebKit] =
            [
                "libwebkit2gtk-4.1.so.0",
                "libwebkit2gtk-4.1.so",
                "libwebkit2gtk-4.0.so.37",
                "libwebkit2gtk-4.0.so"
            ],
            [GtkInterop.LibSoup] =
            [
                "libsoup-3.0.so.0",
                "libsoup-3.0.so",
                "libsoup-2.4.so.1",
                "libsoup-2.4.so"
            ]
        };

        NativeLibrary.SetDllImportResolver(typeof(AvaloniaGtk).Assembly, (name, assembly, searchPath) =>
        {
            if (map.TryGetValue(name, out var candidates))
            {
                foreach (var mapped in candidates)
                {
                    if (NativeLibrary.TryLoad(mapped, assembly, searchPath, out var ptr))
                        return ptr;
                }

                Logger.TryGet(LogEventLevel.Error, "WebView")?.Log(null,
                    "Unable to resolve GTK assembly {Name}. Expected options are: {Candidates}", name,
                    string.Join(',', candidates));
            }

            // Default
            return IntPtr.Zero;
        });

        HasSoup3 = NativeLibrary.TryLoad("libsoup-3.0.so.0", out _) ||
                   NativeLibrary.TryLoad("libsoup-3.0.so", out _);
#endif
    }

    public static bool HasSoup3 { get; }

    public static Version? TryGetVersion()
    {
        try
        {
            var major = (int)GtkInterop.webkit_get_major_version();
            var minor = (int)GtkInterop.webkit_get_minor_version();
            var micro = (int)GtkInterop.webkit_get_micro_version();

            return new Version(major, minor, micro);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static bool CheckAccess() => GtkInterop.g_main_context_default() is var context && context != IntPtr.Zero &&
                                        GtkInterop.g_main_context_is_owner(context);

    public static Task<T> RunOnGlibThreadAsync<T>(Func<T> callback,
        [CallerMemberName] string? callerMethod = null,
        [CallerArgumentExpression(nameof(callback))]
        string? callerExpression = null)
    {
        LogDebug(callerMethod, callerExpression);

        return RunTask(callback);
    }

    public static Task RunOnGlibThreadAsync(Action callback,
        [CallerMemberName] string? callerMethod = null,
        [CallerArgumentExpression(nameof(callback))]
        string? callerExpression = null)
    {
        LogDebug(callerMethod, callerExpression);

        return RunTask(callback);
    }

    public static T RunOnGlibThread<T>(Func<T> callback,
        [CallerMemberName] string? callerMethod = null,
        [CallerArgumentExpression(nameof(callback))]
        string? callerExpression = null)
    {
        LogDebug(callerMethod, callerExpression);

        if (CheckAccess())
        {
            return callback();
        }
        else
        {
            var task = RunTask(callback);
            return task.GetAwaiter().GetResult();
        }
    }

    public static void RunOnGlibThread(Action callback,
        [CallerMemberName] string? callerMethod = null,
        [CallerArgumentExpression(nameof(callback))]
        string? callerExpression = null)
    {
        LogDebug(callerMethod, callerExpression);

        if (CheckAccess())
        {
            callback();
        }
        else
        {
            var task = RunTask(callback);
            task.GetAwaiter().GetResult();
        }
    }

    [Conditional("DEBUG")]
    private static void LogDebug(string? callerMethod, string? callerExpression,
        [CallerMemberName] string? runMethod = null)
    {
#if DEBUG
        Debug.WriteLine($"[{runMethod}]: [{callerMethod}] {callerExpression}");
        Debug.WriteLine("");
#endif
    }

    public static Task RunTask(Action callback) => RunTask<object?>(() =>
    {
        callback();
        return null;
    });
    
    public static Task<T> RunTask<T>(Func<T> callback)
    {
        if (!OperatingSystemEx.IsLinux())
            throw new PlatformNotSupportedException("GTK is only supported on Linux");

        if (CachedDelegate.IsAvailable)
            return PrivateApi(CachedDelegate.StartGtk(), callback);
        else
            return PublicApi(callback);

        static async Task<T> PrivateApi(Task<bool> startGtk, Func<T> callback)
        {
            if (!await startGtk.ConfigureAwait(false))
                throw new InvalidOperationException("Unable to initialize GTK");

            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            StartCallback(() =>
            {
                try
                {
                    tcs.SetResult(callback());
                }
                catch (Exception ex)
                {
                    Debugger.Break();
                    tcs.TrySetException(ex);
                }
            });
            return await tcs.Task.ConfigureAwait(false);
        }

        static unsafe void StartCallback(Action callback)
        {
            var data = GCHandle.ToIntPtr(GCHandle.Alloc(callback));
            GtkInterop.g_timeout_add(0U, new((delegate* unmanaged[Cdecl]<IntPtr, int>)&SourceOnceFunc), data);
        }

        static async Task<T> PublicApi(Func<T> callback)
        {
            try
            {
                return await CachedDelegate<T>.Run(callback).ConfigureAwait(false);
            }
            catch (Exception)
            {
                Debugger.Break();
                throw;
            }
        }
    }
    
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int SourceOnceFunc(IntPtr userData)
    {
        var gcHandle = GCHandle.FromIntPtr(userData);
        var target = (Action) gcHandle.Target!;
        gcHandle.Free();
        target();
        return GtkInterop.False;
    }

    [SupportedOSPlatform("linux")]
    private static class CachedDelegate
    {
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Should be fine for generic ref types")]
        private static readonly Func<Task<bool>>? s_startGtk = Type
            .GetType("Avalonia.X11.NativeDialogs.Gtk, Avalonia.X11")?
            .GetMethod("StartGtk", BindingFlags.Public | BindingFlags.Static) is not { } method ?
            null :
            (Func<Task<bool>>?)Delegate.CreateDelegate(typeof(Func<Task<bool>>), method);

        public static bool IsAvailable => s_startGtk is not null;

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods,
            "Avalonia.X11.NativeDialogs.Gtk", "Avalonia.X11")]
        public static Task<bool> StartGtk()
        {
            if (s_startGtk is null)
                throw new InvalidOperationException("Avalonia.X11 is not referenced");
            return s_startGtk();
        }
    }

    [SupportedOSPlatform("linux")]
    private static class CachedDelegate<T>
    {
        // https://github.com/AvaloniaUI/Avalonia/blob/11.1.0/src/Avalonia.X11/Interop/GtkInteropHelper.cs#L9
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Should be fine for generic ref types")]
        private static readonly Func<Func<T>, Task<T>>? s_runOnGlibThread = Type
            .GetType("Avalonia.X11.Interop.GtkInteropHelper, Avalonia.X11")?
            .GetMethod("RunOnGlibThread", BindingFlags.Public | BindingFlags.Static)?
            .MakeGenericMethod(typeof(T)) is not { } method ?
            null :
            (Func<Func<T>, Task<T>>?)Delegate.CreateDelegate(typeof(Func<Func<T>, Task<T>>), method);

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Avalonia.X11.Interop.GtkInteropHelper",
            "Avalonia.X11")]
        public static Task<T> Run(Func<T> callback)
        {
            if (s_runOnGlibThread is null)
                throw new InvalidOperationException("Avalonia.X11 is not referenced");
            return s_runOnGlibThread(callback);
        }
    }
}
