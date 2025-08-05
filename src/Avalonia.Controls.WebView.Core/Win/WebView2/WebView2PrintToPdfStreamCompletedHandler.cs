using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;
using Avalonia.Controls.Win.Interop;
using Avalonia.Controls.Win.WebView2.Interop;

namespace Avalonia.Controls.Win.WebView2;

#if COM_SOURCE_GEN
[GeneratedComClass]
#endif
internal partial class WebView2PrintToPdfStreamCompletedHandler : CallbackBase, ICoreWebView2PrintToPdfStreamCompletedHandler
{
    public TaskCompletionSource<Stream> Result { get; } = new();

    public void Invoke(int errorCode, IComStream result)
    {
        if (errorCode != 0)
            Result.SetException(Marshal.GetExceptionForHR(errorCode) ?? new Win32Exception(errorCode));
        else
            Result.SetResult(new DataStreamFromComStream(result));
    }
}
