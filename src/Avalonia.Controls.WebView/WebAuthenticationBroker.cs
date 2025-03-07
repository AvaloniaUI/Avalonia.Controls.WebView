using System;
using System.Threading.Tasks;
using Core = Avalonia.Controls;
#if WPF
using AvaloniaUI.Xpf.WpfAbstractions;
using Avalonia.Controls;
#elif AVALONIA
using AvTopLevel = Avalonia.Controls.TopLevel;
#endif

#if AVALONIA
namespace Avalonia.Controls
#elif WPF
namespace Avalonia.Xpf.Controls
#endif
{
    /// <summary>
    /// <see cref="WebAuthenticationBroker"/> is a utility class that facilitates OAuth and other web-based authentication flows by providing a secure way to handle web authentication in applications.
    /// </summary>
    public static class WebAuthenticationBroker
    {
        /// <summary>
        /// Starts an authentication flow by navigating to the specified start URI and monitoring for navigation to the end URI.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">Platform is not supported.</exception>
        /// <exception cref="OperationCanceledException">Operation was cancelled programmatically or by user.</exception>
        public static async Task<WebAuthenticationResult> AuthenticateAsync
#if WPF
            (System.Windows.Window topLevel, WebAuthenticatorOptions options)
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
                var uri = await Core.Macios.MaciosWebAuthenticationBroker.AuthenticateAsync(avTopLevel, options.RequestUri, options.RedirectUri.Scheme);
                return new WebAuthenticationResult(uri);
            }

            if (supportsNativeWebDialog)
            {
                return await AuthenticateDialogAsync(topLevel, options);
            }

            throw new PlatformNotSupportedException();
        }

        private static async Task<WebAuthenticationResult> AuthenticateDialogAsync
#if WPF
            (System.Windows.Window topLevel, WebAuthenticatorOptions options)
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
                dialog.CanUserResize = false;
                dialog.Resize(600, 700);

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
                if (e.Request is not null && IsCallbackUri(e.Request, options.RedirectUri))
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

    /// <summary>
    /// Authentication options that control the broker's behavior.
    /// </summary>
    /// <param name="RequestUri">The initial URI that starts the authentication flow.</param>
    /// <param name="RedirectUri">URI that indicates the completion of the authentication flow.</param>
    public record WebAuthenticatorOptions(Uri RequestUri, Uri RedirectUri)
    {
        /// <summary>
        /// If true, WebAuthenticationBroker will avoid platform specific implementation option, and will use webview dialog window.
        /// </summary>
        public bool PreferNativeWebViewDialog { get; init; }
    }

    /// <param name="CallbackUri">The response URI containing authentication data.</param>
    public record WebAuthenticationResult(Uri CallbackUri);
}
