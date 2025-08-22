using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Avalonia.Controls;

internal class WebViewDispatcher
{
    public static WebViewDispatcher Current { get; set; } = new();

    public virtual void CheckAccessHandler() => Dispatcher.UIThread.CheckAccess();
    public virtual Task InvokeAsyncHandler(Action a) => Dispatcher.UIThread.InvokeAsync(a).GetTask();
    public virtual void InvokeHandler(Action a) => Dispatcher.UIThread.Invoke(a);
    public virtual void InvokeInputHandler(Action a) => Dispatcher.UIThread.Invoke(a, DispatcherPriority.Input);

    public virtual void PushFrameForTaskHandler(Task task)
    {
        var frame = new DispatcherFrame();
        _ = task.ContinueWith(static (_, s) => ((DispatcherFrame)s!).Continue = false, frame);
        Dispatcher.UIThread.PushFrame(frame);
    }

    public static void PushFrameForTask(Task task) => Current.PushFrameForTaskHandler(task);
    public static async void InvokeAsync(Action action)
    {
        await Current.InvokeAsyncHandler(action).ConfigureAwait(false);
    }

    public static void Invoke(Action action) => Current.InvokeHandler(action);
    public static void InvokeInput(Action action) => Current.InvokeInputHandler(action);
    public static void CheckAccess() => Current.CheckAccessHandler();
}
