using System;
using System.ComponentModel;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Controls.Win.WebView2.Interop;
using Avalonia.Platform;

namespace Avalonia.Controls.Win.WebView2;

[SupportedOSPlatform("windows6.1")] // win7
internal partial class WebView2HwndAdapter(IPlatformHandle handle) : WebView2BaseAdapter(handle)
{
    public override IntPtr Handle { get; } = handle.Handle;
    public override string HandleDescriptor { get; } = handle.HandleDescriptor!; // Expected to be HWND always.

    protected override Task<ICoreWebView2Controller> CreateWebView2Controller(ICoreWebView2Environment env, IntPtr handle)
    {
        var handler = new WebView2ControllerHandler();
        env.CreateCoreWebView2Controller(handle, handler);
        return handler.Result.Task;
    }

#if COM_SOURCE_GEN
    [GeneratedComClass]
#endif
    private partial class WebView2ControllerHandler : GenericCompletedHandler<ICoreWebView2Controller>,
        ICoreWebView2CreateCoreWebView2ControllerCompletedHandler;
}
