using System;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Controls.Win.WebView2.Interop;
using Avalonia.Platform;

namespace Avalonia.Controls.Win.WebView2;

[SupportedOSPlatform("windows6.1")] // win7
internal partial class WebView2HwndAdapter(IPlatformHandle handle, ICoreWebView2Controller controller)
    : WebView2BaseAdapter(controller)
{
    public override IntPtr Handle { get; } = handle.Handle;
    public override string HandleDescriptor { get; } = handle.HandleDescriptor!; // Expected to be HWND always.

    public static async Task<WebViewAdapter.NativeWebViewAdapterBuilder> CreateBuilder(
        WindowsWebView2EnvironmentRequestedEventArgs environmentArgs)
    {
        var env = await CoreWebView2Environment.CreateAsync(environmentArgs);

        return (parent, createChild) =>
        {
            var thisHandle = createChild(parent);
            WindowsUtility.MakeHwndTransparent(parent.Handle);
            var task = InitializeAsync(thisHandle, env, environmentArgs);
            return new WebViewAdapter.AdapterWrapper(thisHandle, task);

            static async Task<IWebViewAdapter> InitializeAsync(
                IPlatformHandle thisHandle, ICoreWebView2Environment env,
                WindowsWebView2EnvironmentRequestedEventArgs environmentArgs)
            {
                var handler = new WebView2ControllerHandler();

                var hasCustomOptions = environmentArgs.IsInPrivateModeEnabled
                                       || !string.IsNullOrEmpty(environmentArgs.ProfileName);
                if (hasCustomOptions && env is ICoreWebView2Environment10 env10)
                {
                    var options = env10.CreateCoreWebView2ControllerOptions();
                    options.SetIsInPrivateModeEnabled(environmentArgs.IsInPrivateModeEnabled);
                    if (environmentArgs.ProfileName is not null)
                        options.SetProfileName(environmentArgs.ProfileName);

                    env10.CreateCoreWebView2ControllerWithOptions(thisHandle.Handle, options, handler);
                }
                else
                {
                    env.CreateCoreWebView2Controller(thisHandle.Handle, handler);
                }

                var controller = await handler.Result.Task;
                var webView = new WebView2HwndAdapter(thisHandle, controller);
                await webView.InitializeAsync(environmentArgs);
                return webView;
            }
        };
    }

    [GeneratedComClass]
    private partial class WebView2ControllerHandler : GenericCompletedHandler<ICoreWebView2Controller>,
        ICoreWebView2CreateCoreWebView2ControllerCompletedHandler;
}
