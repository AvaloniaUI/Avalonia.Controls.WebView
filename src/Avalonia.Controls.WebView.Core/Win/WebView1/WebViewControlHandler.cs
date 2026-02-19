using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.Interop;
using Avalonia.Controls.Win.WebView1.Interop;

namespace Avalonia.Controls.Win.WebView1;

[GeneratedComClass]
internal partial class WebViewControlHandler
    : BaseHandler<IAsyncOperation_WebViewControl, IWebViewControl>, IAsyncOperationCompletedHandler_WebViewControl
{
    public override void Invoke(IAsyncOperation_WebViewControl asyncInfo, AsyncStatus asyncStatus)
    {
        SetResult(asyncInfo.GetResults, asyncStatus);
    }
}
