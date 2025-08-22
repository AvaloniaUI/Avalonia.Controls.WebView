using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
#endif
    }

    public static Task<T> RunOnGlibThreadAsync<T>(Func<T> callback,
        [CallerMemberName] string? callerMethod = null,
        [CallerArgumentExpression(nameof(callback))]
        string? callerExpression = null)
    {
        LogDebug(callerMethod, callerExpression);

        return CachedDelegate<T>.Run(callback);
    }

    public static Task RunOnGlibThreadAsync(Action callback,
        [CallerMemberName] string? callerMethod = null,
        [CallerArgumentExpression(nameof(callback))]
        string? callerExpression = null)
    {
        LogDebug(callerMethod, callerExpression);

        return CachedDelegate.Run(callback);
    }

    public static T RunOnGlibThread<T>(Func<T> callback,
        [CallerMemberName] string? callerMethod = null,
        [CallerArgumentExpression(nameof(callback))]
        string? callerExpression = null)
    {
        LogDebug(callerMethod, callerExpression);

        var task = CachedDelegate<T>.Run(callback);
        return task.GetAwaiter().GetResult();
    }

    public static void RunOnGlibThread(Action callback,
        [CallerMemberName] string? callerMethod = null,
        [CallerArgumentExpression(nameof(callback))]
        string? callerExpression = null)
    {
        LogDebug(callerMethod, callerExpression);

        var task = CachedDelegate.Run(callback);
        task.GetAwaiter().GetResult();
    }

    public static T RunOnGlibThreadFrame<T>(Func<T> callback,
        [CallerMemberName] string? callerMethod = null,
        [CallerArgumentExpression(nameof(callback))]
        string? callerExpression = null)
    {
        LogDebug(callerMethod, callerExpression);

        var task = CachedDelegate<T>.Run(callback);
        if (!task.IsCompleted)
        {
            WebViewDispatcher.PushFrameForTask(task);
        }

        return task.GetAwaiter().GetResult();
    }

    public static void RunOnGlibThreadFrame(Action callback,
        [CallerMemberName] string? callerMethod = null,
        [CallerArgumentExpression(nameof(callback))]
        string? callerExpression = null)
    {
        LogDebug(callerMethod, callerExpression);

        var task = CachedDelegate.Run(callback);
        if (!task.IsCompleted)
        {
            WebViewDispatcher.PushFrameForTask(task);
        }

        task.GetAwaiter().GetResult();
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
        public static async Task<T> Run(Func<T> callback)
        {
            if (s_runOnGlibThread is null)
                throw new InvalidOperationException("Avalonia.X11 is not referenced");
#if DEBUG
            try
            {
                return await s_runOnGlibThread(callback).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Debugger.Break();
                throw;
            }
#else
            return await s_runOnGlibThread(callback).ConfigureAwait(false);
#endif
        }
    }

    private static class CachedDelegate
    {
        public static Task Run(Action callback)
        {
            return CachedDelegate<object?>.Run(() =>
            {
                callback();
                return null;
            });
        }
    }
}
