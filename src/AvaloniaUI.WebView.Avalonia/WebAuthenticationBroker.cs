using System;
using System.Threading.Tasks;
using AvTopLevel = Avalonia.Controls.TopLevel;
#if WPF
using System.Windows;
using AvaloniaUI.Xpf.WpfAbstractions;
#endif

namespace AvaloniaUI.WebView;

public static class WebAuthenticationBroker
{
    public static Task<WebAuthenticationResult> AuthenticateAsync
#if WPF
        (Window topLevel, WebAuthenticatorOptions options)
#elif AVALONIA
        (AvTopLevel topLevel, WebAuthenticatorOptions options)
#endif
    {
        var supportsNativeWebDialog =
            OperatingSystemEx.IsWindows() || OperatingSystemEx.IsLinux() || OperatingSystemEx.IsMacOS();

        if (!(supportsNativeWebDialog & options.PreferNativeWebViewDialog)
#if WPF
            && XpfWpfAbstraction.GetAvaloniaTopLevelForWindow(topLevel) is { } avTopLevel
#elif AVALONIA
            && topLevel is var avTopLevel
#endif
            && (OperatingSystemEx.IsIOSVersionAtLeast(13, 0) || OperatingSystemEx.IsMacOSVersionAtLeast(10, 15)))
        {
            return MaciosWebAuthenticationBroker.AuthenticateAsync(avTopLevel, options);
        }

        if (supportsNativeWebDialog)
        {
            return AuthenticateDialogAsync(topLevel, options);
        }

        throw new PlatformNotSupportedException();
    }

    private static async Task<WebAuthenticationResult> AuthenticateDialogAsync
#if WPF
        (Window topLevel, WebAuthenticatorOptions options)
#elif AVALONIA
        (AvTopLevel topLevel, WebAuthenticatorOptions options)
#endif
    {
        using var dialog = new NativeWebViewDialog();
        var tcs = new TaskCompletionSource<WebAuthenticationResult>();

        dialog.Closing += OnClosing;
        dialog.NavigationStarted += OnNavigationStarted;

        try
        {
            dialog.Title = "Authentication";
            dialog.Source = options.RequestUri;
            dialog.Show(topLevel);

            return await tcs.Task;
        }
        finally
        {
            dialog.Closing -= OnClosing;
            dialog.NavigationStarted -= OnNavigationStarted;
            dialog.Close();
        }

        void OnClosing(object? sender, EventArgs e)
        {
            tcs.SetCanceled();
        }
        void OnNavigationStarted(object? sender, WebViewNavigationStartingEventArgs e)
        {
            if (e.Request is not null && IsCallbackUri(e.Request, options.CallbackUri))
            {
                e.Cancel = true;
                tcs.SetResult(new WebAuthenticationResult(e.Request));
            }
        }
    }

    private static bool IsCallbackUri(Uri navigatingUri, Uri callbackUri)
    {
        return navigatingUri.Scheme == callbackUri.Scheme
               && navigatingUri.Host == callbackUri.Host
               && navigatingUri.AbsolutePath == callbackUri.AbsolutePath;
    }
}
