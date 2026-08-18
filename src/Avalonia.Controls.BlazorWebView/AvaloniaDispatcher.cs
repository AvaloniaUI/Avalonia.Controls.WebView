using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components;
using AvaloniaDispatcherUI = Avalonia.Threading.Dispatcher;

namespace Avalonia.Controls.BlazorWebView;

internal sealed class AvaloniaDispatcher : Dispatcher
{
    public static AvaloniaDispatcher Instance { get; } = new();

    private AvaloniaDispatcher()
    {
    }

    public override bool CheckAccess() => AvaloniaDispatcherUI.UIThread.CheckAccess();

    public override async Task InvokeAsync(Action workItem)
    {
        try
        {
            if (CheckAccess())
                workItem();
            else
                await AvaloniaDispatcherUI.UIThread.InvokeAsync(workItem);
        }
        catch (Exception ex)
        {
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw;
        }
    }

    public override async Task InvokeAsync(Func<Task> workItem)
    {
        try
        {
            if (CheckAccess())
                await workItem();
            else
                await AvaloniaDispatcherUI.UIThread.InvokeAsync(workItem);
        }
        catch (Exception ex)
        {
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw;
        }
    }

    public override async Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
    {
        try
        {
            if (CheckAccess())
                return workItem();
            return await AvaloniaDispatcherUI.UIThread.InvokeAsync(workItem);
        }
        catch (Exception ex)
        {
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw;
        }
    }

    public override async Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
    {
        try
        {
            if (CheckAccess())
                return await workItem();
            return await AvaloniaDispatcherUI.UIThread.InvokeAsync(workItem);
        }
        catch (Exception ex)
        {
            ExceptionDispatchInfo.Capture(ex).Throw();
            throw;
        }
    }
}
