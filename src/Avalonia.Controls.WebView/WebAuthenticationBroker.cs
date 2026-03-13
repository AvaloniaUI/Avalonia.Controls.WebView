using System;
using System.Threading.Tasks;
using Avalonia.Platform;
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
                OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() ||
                OperatingSystem.IsAndroid();

            if (!(supportsNativeWebDialog & options.PreferNativeWebDialog)
#if WPF
                && XpfWpfAbstraction.GetAvaloniaTopLevelForWindow(topLevel) is { } avTopLevel
#elif AVALONIA
                && topLevel is var avTopLevel
#endif
                )
            {
#if ANDROID
                if (OperatingSystem.IsAndroid())
                {
                    var uri = await Core.Android.AndroidWebAuthenticationBroker.AuthenticateAsync(avTopLevel,
                        options.RequestUri, options.RedirectUri);
                    return new WebAuthenticationResult(uri);
                }
#else
                if ((OperatingSystem.IsIOSVersionAtLeast(13, 0) || OperatingSystem.IsMacOSVersionAtLeast(10, 15)))
                {
                    var uri = await Core.Macios.MaciosWebAuthenticationBroker.AuthenticateAsync(avTopLevel,
                        options.RequestUri, options.RedirectUri.Scheme, options.NonPersistent);
                    return new WebAuthenticationResult(uri);
                }
                else if (OperatingSystem.IsBrowser())
                {
                    var uri = await Core.Browser.BrowserWebAuthenticationBroker.AuthenticateAsync(avTopLevel,
                        options.RequestUri, options.RedirectUri);
                    return new WebAuthenticationResult(uri);
                }
#endif
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
            var tcs = new TaskCompletionSource<WebAuthenticationResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            using var dialog = options.NativeWebDialogFactory?.Invoke() ?? DefaultFactory();
            dialog.EnvironmentRequested += (_, args) =>
            {
                if (args is WindowsWebView2EnvironmentRequestedEventArgs webView2
                    && options.NonPersistent)
                    webView2.IsInPrivateModeEnabled = true;
                else if (args is AppleWKWebViewEnvironmentRequestedEventArgs wkWebView
                         && options.NonPersistent)
                    wkWebView.NonPersistentDataStore = true;
                else if (args is GtkWebViewEnvironmentRequestedEventArgs gtkWebView
                         && options.NonPersistent)
                    gtkWebView.EphemeralDataManager = true;
                else if (args is AndroidWebViewEnvironmentRequestedEventArgs androidWebView
                         && options.NonPersistent)
                {
                    androidWebView.DisableCache = true;
                    androidWebView.DatabaseEnabled = false;
                    androidWebView.DomStorageEnabled = false;
                }
            };
            dialog.Closing += OnClosing;
            dialog.NavigationStarted += OnNavigationStarted;

            try
            {
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

        private static NativeWebDialog DefaultFactory()
        {
            var dialog = new NativeWebDialog();
            dialog.Title = "Authentication";
            dialog.CanUserResize = false;
            dialog.Resize(600, 700);
            return dialog;
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
        public bool PreferNativeWebDialog { get; init; }

        /// <summary>
        /// Hint for the platform implementation to not store any session data persistently.
        /// </summary>
        public bool NonPersistent { get; init; }

        /// <summary>
        /// Callback that can be used to override NativeWebDialog creation when WebAuthenticationBroker uses dialog implementation instead of system auth APIs.
        /// </summary>
        public Func<NativeWebDialog?>? NativeWebDialogFactory { get; init; }
    }

    /// <param name="CallbackUri">The response URI containing authentication data.</param>
    public record WebAuthenticationResult(Uri CallbackUri);
}
