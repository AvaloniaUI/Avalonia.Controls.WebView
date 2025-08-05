using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Avalonia.Controls.Win.WebView2;

internal abstract class GenericCompletedHandler<TResult> : CallbackBase
{
    public TaskCompletionSource<TResult> Result { get; } = new();

    public void Invoke(int errorCode, TResult result)
    {
        if (errorCode != 0)
            Result.SetException(Marshal.GetExceptionForHR(errorCode) ?? new Win32Exception(errorCode));
        else
            Result?.TrySetResult(result);
    }
}
