using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Controls.Macios.Interop;
using Avalonia.Controls.Macios.Interop.AuthenticationServices;

namespace Avalonia.Controls.Macios;

[SupportedOSPlatform("macos10.15")]
[SupportedOSPlatform("ios13.0")]
internal static class MaciosWebAuthenticationBroker
{
    public static async Task<Uri> AuthenticateAsync(TopLevel topLevel, Uri requestUri, string scheme)
    {
        var tcs = new TaskCompletionSource<Uri>();

        using var context = new ASWebAuthenticationPresentationContextProviding(GetWindowHandle(topLevel));

        using var session = CreateSession(requestUri, scheme, tcs);
        session.PresentationContextProvider = context;
        session.Start();

        var result = await tcs.Task;
        return result;
    }

    private static ASWebAuthenticationSession CreateSession(Uri requestUri, string scheme, TaskCompletionSource<Uri> completion)
    {
        using var requestUrlStr = NSString.Create(requestUri.ToString());
        using var requestUrl = new NSUrl(requestUrlStr);
        using var schemeStr = NSString.Create(scheme);

        return OperatingSystemEx.IsIOSVersionAtLeast(17, 4)
            ? ASWebAuthenticationSession.InitWithURL(requestUrl, ASWebAuthenticationSessionCallback.FromCustomScheme(schemeStr), completion)
            : ASWebAuthenticationSession.InitWithURL(requestUrl, schemeStr, completion);
    }

    private static IntPtr GetWindowHandle(TopLevel topLevel)
    {
        if (topLevel is Window window)
        {
            return window.TryGetPlatformHandle()!.Handle;
        }

        if (topLevel.TryGetPlatformHandle() is { } platformHandle)
            return AppleView.GetWindow(platformHandle.Handle);

        return AppleView.GetWindow(GetAvaloniaViewHandle(topLevel));

        // WE MUST PROPERLY IMPLEMENT TryGetPlatformHandle ON IOS
        static IntPtr GetAvaloniaViewHandle(TopLevel topLevel)
        {
            var implType = topLevel.PlatformImpl?.GetType();
            var view = implType?.GetProperty("View")?.GetValue(topLevel.PlatformImpl);
            var viewType = view?.GetType();
            var nativeHandle = viewType?.GetProperty("Handle")?.GetValue(view);
            var nativeHandleType = nativeHandle?.GetType();
            return nativeHandleType?.GetProperty("Handle")?.GetValue(nativeHandle) as IntPtr? ?? IntPtr.Zero;
        }
    }
}
