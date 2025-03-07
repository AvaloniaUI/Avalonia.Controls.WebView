using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Avalonia.Controls.Gtk;

internal static class AvaloniaGtk
{
    public static Task<T> RunOnGlibThreadAsync<T>(Func<T> callback) => CachedDelegate<T>.Run(callback);

    public static T RunOnGlibThread<T>(Func<T> callback)
    {
        var task = CachedDelegate<T>.Run(callback);
        if (!task.IsCompleted)
        {
            var frame = new DispatcherFrame();
            _ = task.ContinueWith(static (_, s) => ((DispatcherFrame)s!).Continue = false, frame);
            Dispatcher.UIThread.PushFrame(frame);
        }

        return task.GetAwaiter().GetResult();
    }

    public static void RunOnGlibThread(Action callback)
    {
        _ = RunOnGlibThread(() =>
        {
            callback();
            return (object?)null;
        });
    }

    private static class CachedDelegate<T>
    {
        // https://github.com/AvaloniaUI/Avalonia/blob/11.1.0/src/Avalonia.X11/Interop/GtkInteropHelper.cs#L9
        private static readonly Func<Func<T>, Task<T>>? s_runOnGlibThread = Type
            .GetType("Avalonia.X11.Interop.GtkInteropHelper, Avalonia.X11")?
            .GetMethod("RunOnGlibThread", BindingFlags.Public | BindingFlags.Static)?
            .MakeGenericMethod(typeof(T)) is not { } method
            ? null : (Func<Func<T>, Task<T>>?)Delegate.CreateDelegate(typeof(Func<Func<T>, Task<T>>), method);

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Avalonia.X11.Interop.GtkInteropHelper",
            "Avalonia.X11")]
        public static Task<T> Run(Func<T> callback) => s_runOnGlibThread?.Invoke(callback)
                                                       ?? throw new InvalidOperationException("Avalonia.X11 is not referenced");
    }
}
