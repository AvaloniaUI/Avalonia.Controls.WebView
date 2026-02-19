using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using Avalonia.Controls.Win.Interop;
using Avalonia.Controls.Win.WebView1.Interop;

namespace Avalonia.Controls.Win.WebView1;

[GeneratedComClass]
[SupportedOSPlatform("windows")]
internal partial class HStringResultHandler
    : BaseHandler<IAsyncOperation_HString, string>, IAsyncOperationCompletedHandler_HString
{
    public override void Invoke(IAsyncOperation_HString asyncInfo, AsyncStatus asyncStatus)
    {
        SetResult(() => HStringInterop.FromIntPtr(asyncInfo.GetResults())!, asyncStatus);
    }
}
