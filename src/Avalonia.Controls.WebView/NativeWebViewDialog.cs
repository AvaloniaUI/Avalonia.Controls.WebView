using System;
using System.Threading.Tasks;
using Core = Avalonia.Controls;
using IPlatformHandle = Avalonia.Platform.IPlatformHandle;
#if WPF
using Avalonia.Controls;
using AvaloniaUI.Xpf.WpfAbstractions;
using Window = System.Windows.Window;
#elif AVALONIA
using Window = Avalonia.Controls.Window;
#endif

#if AVALONIA
namespace Avalonia.Controls
#elif WPF
namespace Avalonia.Xpf.Controls
#endif
{
    /// <summary>
    /// <see cref="NativeWebViewDialog"/> is a dialog window that hosts a native web browser implementation.
    /// It provides a way to display web content in a separate window, particularly useful for platforms like Linux where embedded WebView controls might not be available.
    /// </summary>
    public class NativeWebViewDialog : IWebView, INativeWebViewDialog
    {
        private readonly INativeWebViewDialog _impl;

        public NativeWebViewDialog()
        {
            _impl = OperatingSystemEx.IsLinux() ? new Core.Gtk.GtkNativeWebViewDialog() : new WindowNativeWebViewDialog();
            _impl.Closing += (_, args) => Closing?.Invoke(this, args);
            _impl.WebView.NavigationStarted += (_, args) => NavigationStarted?.Invoke(this, args);
            _impl.WebView.NavigationStarted += (_, args) => NavigationStarted?.Invoke(this, args);
            _impl.WebView.WebMessageReceived += (_, args) => WebMessageReceived?.Invoke(this, args);
        }

        /// <inheritdoc/>
        public bool CanGoBack => _impl.WebView.CanGoBack;
        /// <inheritdoc/>
        public bool CanGoForward => _impl.WebView.CanGoForward;
        /// <inheritdoc/>
        public Uri Source { get => _impl.WebView.Source; set => _impl.WebView.Source = value; }

        /// <inheritdoc/>
        public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
        /// <inheritdoc/>
        public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;
        /// <inheritdoc/>
        public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;

        /// <inheritdoc/>
        public bool GoBack() => _impl.WebView.GoBack();
        /// <inheritdoc/>
        public bool GoForward() => _impl.WebView.GoForward();
        /// <inheritdoc/>
        public Task<string?> InvokeScript(string script) => _impl.WebView.InvokeScript(script);
        /// <inheritdoc/>
        public void Navigate(Uri url) => _impl.WebView.Navigate(url);
        /// <inheritdoc/>
        public void NavigateToString(string text) => _impl.WebView.NavigateToString(text);
        /// <inheritdoc/>
        public bool Refresh() => _impl.WebView.Refresh();
        /// <inheritdoc/>
        public bool Stop() => _impl.WebView.Stop();

        /// <inheritdoc/>
        public void Dispose() => _impl.Dispose();

        /// <inheritdoc/>
        public string? Title { get => _impl.Title; set => _impl.Title = value; }

        /// <inheritdoc/>
        public bool CanUserResize { get => _impl.CanUserResize; set => _impl.CanUserResize = value; }

        /// <inheritdoc/>
        public event EventHandler? Closing;
        /// <inheritdoc/>
        public void Show() => _impl.Show();

#if WPF
        /// <summary>
        /// Opens the WebView dialog with <see cref="Window"/> owner.
        /// </summary>
        public void Show(Window owner)
#elif AVALONIA
        /// <summary>
        /// Opens the WebView dialog with <see cref="TopLevel"/> owner.
        /// </summary>
        public void Show(TopLevel owner)
#endif
        {
#if WPF
            var avTopLevel = XpfWpfAbstraction.GetAvaloniaTopLevelForWindow(owner);
#elif AVALONIA
            var avTopLevel = owner;
#endif

            if (owner is Window ownerWindow && TryGetWindow() is { } window)
            {
#if WPF
                window.Owner = ownerWindow;
                window.Show();
#elif AVALONIA
                window.Show(ownerWindow);
#endif
            }
            else if (avTopLevel?.TryGetPlatformHandle() is not { } platformHandle
                || !_impl.Show(platformHandle))
            {
                _impl.Show();
            }
        }

        /// <inheritdoc/>
        public void Close() => _impl.Close();

        /// <inheritdoc/>
        public bool Resize(int width, int height) => _impl.Resize(width, height);

        /// <inheritdoc/>
        public bool Move(int x, int y) => _impl.Move(x, y);

        /// <inheritdoc/>
        public IPlatformHandle? TryGetPlatformHandle() => _impl.TryGetPlatformHandle();

        /// <summary>
        /// Gets platform handle of the webview hosted inside the dialog.
        /// </summary>
        public IPlatformHandle? TryGetWebViewPlatformHandle() => _impl.WebView as IWebViewAdapter;

        /// <summary>
        /// Returns instance <see cref="NativeWebViewCommandManager"/> that allows executing common keyboard commands. Or null, if not supported by the platform.
        /// </summary>
        public NativeWebViewCommandManager? TryGetCommandManager() =>
            _impl.WebView is IWebViewAdapterWithCommands commands ? new NativeWebViewCommandManager(commands) : null;

        /// <summary>
        /// Returns instance <see cref="NativeWebViewCookieManager"/> that allows reading and settings cookies. Or null, if not supported by the platform.
        /// </summary>
        public NativeWebViewCookieManager? TryGetCookieManager() =>
            _impl.WebView is IWebViewAdapterWithCookieManager adapter ? new NativeWebViewCookieManager(adapter) : null;

        /// <summary>
        /// If dialog is based on a <see cref="Window"/>, returns its instance to allow full control.
        /// </summary>
        public Window? TryGetWindow() => _impl as WindowNativeWebViewDialog;

        IWebView INativeWebViewDialog.WebView => _impl.WebView;
        bool INativeWebViewDialog.Show(IPlatformHandle owner) => _impl.Show(owner);
    }
}
