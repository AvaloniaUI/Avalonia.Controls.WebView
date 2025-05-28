using System;
using System.Threading.Tasks;
using Avalonia.Controls.Win.WebView1.Interop;
using Avalonia.Controls.Win.WebView2;

namespace Avalonia.Controls.Win.WebView1;

internal abstract class BaseHandler<TAsyncInfo, TResult> : CallbackBase
    where TAsyncInfo : IInspectable
    where TResult : class
{
    private readonly TaskCompletionSource<TResult> _taskCompletionSource = new();
    public Task<TResult> Task => _taskCompletionSource.Task;

    private Action<TResult>? _result;

    public void AddHandler(Action<TResult> result)
    {
        _result = result;
    }

    public abstract void Invoke(TAsyncInfo asyncInfo, AsyncStatus asyncStatus);

    protected void SetResult(Func<TResult> resultResolver, AsyncStatus asyncStatus)
    {
        var result = _taskCompletionSource.SetResult(asyncStatus, resultResolver);
        if (result is not null)
        {
            _result?.Invoke(result);
        }
    }
}
