using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls.Win.WebView1.Interop;

namespace Avalonia.Controls.Win.WebView1;

internal static class AsyncOperationEx
{
    public static TResult? SetResult<TResult>(
        this TaskCompletionSource<TResult> taskCompletionSource,
        AsyncStatus asyncStatus,
        Func<TResult> resolver)
        where TResult : class
    {
        if (asyncStatus == AsyncStatus.Started)
        {
            throw new InvalidOperationException("Async operation was not completed");
        }
        else if (asyncStatus == AsyncStatus.Canceled)
        {
            taskCompletionSource.TrySetCanceled();
        }
        else
        {
            try
            {
                var value = resolver();
                if (asyncStatus == AsyncStatus.Completed)
                {
                    taskCompletionSource.TrySetResult(value);
                    return value;
                }
                else
                {
                    taskCompletionSource.TrySetException(new InvalidOperationException());
                }
            }
            catch (COMException ex)
            {
                switch (ex.HResult)
                {
                    case unchecked((int)0x80020006):
                        taskCompletionSource.TrySetException(new JavaScriptException("There is no function"));
                        break;
                    case unchecked((int)0x80020101):
                        taskCompletionSource.TrySetException(new JavaScriptException("A JavaScript error or exception occured while executing function"));
                        break;
                    case unchecked((int)0x800a138a):
                        taskCompletionSource.TrySetException(new JavaScriptException("Is not a function"));
                        break;
                    default:
                        taskCompletionSource.TrySetException(new InvalidOperationException(ex.Message));
                        break;
                }
            }
        }

        return null;
    }
}
