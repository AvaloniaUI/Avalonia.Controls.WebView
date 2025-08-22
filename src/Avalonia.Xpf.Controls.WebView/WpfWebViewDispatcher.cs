using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using Avalonia.Controls;
using AvaloniaUI.Xpf.WpfAbstractions;

namespace Avalonia.Xpf.Controls;

internal sealed class WpfWebViewDispatcher : WebViewDispatcher
{
    private static bool s_setupCompleted;

    public override void CheckAccessHandler() =>
        Dispatcher.CurrentDispatcher.CheckAccess();

    public override Task InvokeAsyncHandler(Action a) =>
        Dispatcher.CurrentDispatcher.InvokeAsync(a).Task;

    public override void InvokeHandler(Action a) =>
        Dispatcher.CurrentDispatcher.Invoke(a);

    public override void InvokeInputHandler(Action a) =>
        Dispatcher.CurrentDispatcher.Invoke(a, DispatcherPriority.Input);

    public override void PushFrameForTaskHandler(Task task)
    {
        var frame = new DispatcherFrame();
        _ = task.ContinueWith(static (_, s) => ((DispatcherFrame)s!).Continue = false, frame);
        Dispatcher.PushFrame(frame);
    }

    public static void Setup()
    {
        if (s_setupCompleted)
            return;
        s_setupCompleted = true;

        if (!XpfWpfAbstraction.IsRunningOnXpf)
        {
            Current = new WpfWebViewDispatcher();
        }
    }
}
