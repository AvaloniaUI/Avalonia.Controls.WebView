using System;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Controls.Win.WebView2.Interop;
using Avalonia.Platform;

namespace Avalonia.Controls.Win.WebView2;

[SupportedOSPlatform("windows10.0.17763.0")]
internal partial class WebView2CompAdapter : WebView2BaseAdapter
{
    private WebView2CompAdapter(IPlatformHandle handle) : base(handle)
    {
    }

    public override IntPtr Handle => default;

    public override string HandleDescriptor => "Windows.UI.Composition.ContainerVisual";

    protected override async Task<ICoreWebView2Controller> CreateWebView2Controller(ICoreWebView2Environment env, IntPtr handle)
    {
        if (env is ICoreWebView2Environment3 environment3)
        {
            var handler = new WebView2CompositionControllerHandler();
            environment3.CreateCoreWebView2CompositionController(handle, handler);
            // ICoreWebView2Controller can be queried from ICoreWebView2CompositionController. 
            // ReSharper disable once SuspiciousTypeConversion.Global
            return (ICoreWebView2Controller)await handler.Result.Task;
        }

        throw new NotSupportedException();
    }

#if COM_SOURCE_GEN
    [GeneratedComClass]
#endif
    private partial class WebView2CompositionControllerHandler : GenericCompletedHandler<ICoreWebView2CompositionController>,
        ICoreWebView2CreateCoreWebView2CompositionControllerCompletedHandler;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
        }
        base.Dispose(disposing);
    }
}
