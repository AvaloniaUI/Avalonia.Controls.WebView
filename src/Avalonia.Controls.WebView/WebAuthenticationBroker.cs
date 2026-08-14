using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Authentication;
using Avalonia.Platform;
using Core = Avalonia.Controls;
#if WPF
using AvaloniaUI.Xpf.WpfAbstractions;
using Avalonia.Controls;
using AvTopLevel = Avalonia.Controls.TopLevel;
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
        /// <exception cref="OperationCanceledException">Operation was canceled programmatically or by user.</exception>
        public static async Task<WebAuthenticationResult> AuthenticateAsync
#if WPF
            (System.Windows.Window topLevel, WebAuthenticatorOptions options)
#elif AVALONIA
            (AvTopLevel topLevel, WebAuthenticatorOptions options)
#endif
        {
            var mode = GetEffectiveMode(options);

#if WPF
            var avTopLevel = XpfWpfAbstraction.GetAvaloniaTopLevelForWindow(topLevel);
#elif AVALONIA
            var avTopLevel = topLevel;
#endif

            if (avTopLevel is null)
            {
                throw new ArgumentNullException(nameof(topLevel));
            }

            switch (mode)
            {
#pragma warning disable CA1416
                case WebAuthenticatorMode.Browser:
                    return await AuthenticateSystemBrowserAsync(avTopLevel, options);
#if ANDROID
                case WebAuthenticatorMode.System when OperatingSystem.IsAndroid():
                {
                    var uri = await Core.Android.AndroidWebAuthenticationBroker.AuthenticateAsync(avTopLevel,
                        options.RequestUri, options.RedirectUri);
                    return new WebAuthenticationResult(uri);
                }
#else
                case WebAuthenticatorMode.System when (OperatingSystem.IsIOSVersionAtLeast(13, 0) || OperatingSystem.IsMacOSVersionAtLeast(10, 15)):
                {
                    var uri = await Core.Macios.MaciosWebAuthenticationBroker.AuthenticateAsync(avTopLevel,
                        options.RequestUri, options.RedirectUri.Scheme, options.NonPersistent);
                    return new WebAuthenticationResult(uri);
                }
                case WebAuthenticatorMode.System when OperatingSystem.IsBrowser():
                {
                    var uri = await Core.Browser.BrowserWebAuthenticationBroker.AuthenticateAsync(avTopLevel,
                        options.RequestUri, options.RedirectUri);
                    return new WebAuthenticationResult(uri);
                }
#endif
                case WebAuthenticatorMode.NativeWebDialog:
                    return await AuthenticateDialogAsync(topLevel, options);
                default:
                    throw new PlatformNotSupportedException();
#pragma warning restore CA1416
            }
        }

        private static WebAuthenticatorMode GetEffectiveMode(WebAuthenticatorOptions options)
        {
#pragma warning disable CA1416
#pragma warning disable CS0618
            var supportsNativeWebDialog = OperatingSystem.IsWindows() || OperatingSystem.IsLinux() ||
                                          OperatingSystem.IsMacOS() || OperatingSystem.IsAndroid();
            var supportsSystem = OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() ||
                                 OperatingSystem.IsAndroid() || OperatingSystem.IsBrowser();
            var supportsBrowserLauncher = !OperatingSystem.IsBrowser(); // can't launch browser from the browser, duh.

            var mode = options is { Mode: WebAuthenticatorMode.Auto, PreferNativeWebDialog: true } ?
                WebAuthenticatorMode.NativeWebDialog :
                options.Mode;

            return mode switch
            {
                WebAuthenticatorMode.Auto when supportsSystem => WebAuthenticatorMode.System,
                WebAuthenticatorMode.Auto when supportsNativeWebDialog => WebAuthenticatorMode.NativeWebDialog,
                WebAuthenticatorMode.Auto when supportsBrowserLauncher => WebAuthenticatorMode.Browser,

                WebAuthenticatorMode.System when supportsSystem => WebAuthenticatorMode.System,
                WebAuthenticatorMode.NativeWebDialog when supportsNativeWebDialog => WebAuthenticatorMode.NativeWebDialog,
                WebAuthenticatorMode.Browser when supportsBrowserLauncher => WebAuthenticatorMode.Browser,

                _ => throw new PlatformNotSupportedException(
                    $"WebAuthenticatorMode.{mode} is not supported on the current platform")
            };
#pragma warning restore CS0618
#pragma warning restore CA1416
        }

        private static async Task<WebAuthenticationResult> AuthenticateSystemBrowserAsync(
            AvTopLevel topLevel, WebAuthenticatorOptions options)
        {
            var browserOptions = options.BrowserOptions ?? new BrowserOptions();

            var callbackUri = await SystemBrowserWebAuthenticationBroker.AuthenticateAsync(
                options.RequestUri,
                options.RedirectUri,
                browserOptions.Timeout,
                uri => topLevel.Launcher.LaunchUriAsync(uri),
                browserOptions.CallbackFilter is { } filter ?
                    uri => filter.Invoke(new WebAuthenticationResult(uri)) :
                    null,
                browserOptions.ResponseHandler is not null ?
                    (uri, response) => browserOptions
                        .ResponseHandler.Invoke(new WebAuthenticationResult(uri), response) :
                    null,
                CancellationToken.None);

            return new WebAuthenticationResult(callbackUri);
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
            dialog.CanUserResize = true;
            dialog.Resize(600, 700);
            return dialog;
        }
    }

    /// <param name="CallbackUri">The response URI containing authentication data.</param>
    public record WebAuthenticationResult(Uri CallbackUri)
    {
        private Uri? _parsedFor;

        /// <summary>
        /// Gets the parameters of the <see cref="CallbackUri"/> query string.
        /// </summary>
        public IReadOnlyDictionary<string, string> Parameters
        {
            get
            {
                if (!ReferenceEquals(_parsedFor, CallbackUri))
                {
                    field = UriQuery.Parse(CallbackUri);
                    _parsedFor = CallbackUri;
                }

                return field!;
            }
        }

        /// <summary>
        /// Gets the OAuth 2.0 <c>code</c> parameter, or null when it is absent or repeated.
        /// </summary>
        public string? Code => GetParameter("code");

        /// <summary>
        /// Gets the OAuth 2.0 <c>state</c> parameter, or null when it is absent or repeated.
        /// </summary>
        public string? State => GetParameter("state");

        /// <summary>
        /// Gets the OAuth 2.0 <c>error</c> parameter, or null when the callback is not an error response.
        /// </summary>
        public string? Error => GetParameter("error");

        /// <summary>
        /// Gets the OAuth 2.0 <c>error_description</c> parameter, if the server sent one.
        /// </summary>
        public string? ErrorDescription => GetParameter("error_description");

        private string? GetParameter(string name) =>
            Parameters.TryGetValue(name, out var value) && value.Length > 0 ? value : null;
    }
}
