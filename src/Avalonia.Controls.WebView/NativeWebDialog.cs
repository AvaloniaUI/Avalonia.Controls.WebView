using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Controls.Gtk;
using Avalonia.Platform;
using Core = Avalonia.Controls;
using IPlatformHandle = Avalonia.Platform.IPlatformHandle;
#if WPF
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
    /// <see cref="NativeWebDialog"/> is a dialog window that hosts a native web browser implementation.
    /// It provides a way to display web content in a separate window, particularly useful for platforms like Linux where embedded WebView controls might not be available.
    /// </summary>
    // ReSharper disable RedundantNameQualifier
    public class NativeWebDialog : Core.IWebView, Core.IWebViewHolder, IDisposable
    {
        private readonly TaskCompletionSource<Core.INativeWebViewDialog> _implTcs = new();
        private EventHandler<Core.WebViewNavigationCompletedEventArgs>? _navigationCompleted;
        private EventHandler<Core.WebViewNavigationStartingEventArgs>? _navigationStarted;
        private EventHandler<Core.WebViewNewWindowRequestedEventArgs>? _newWindowRequested;
        private EventHandler<Core.WebMessageReceivedEventArgs>? _webMessageReceived;
        private EventHandler<Core.WebResourceRequestedEventArgs>? _webResourceRequested;
        private object? _initialSource;
        private string? _initialTitle;
        private bool? _initialCanUserResize;
        private PixelSize? _initialSize;
        private PixelPoint? _initialPosition;
        private bool _disposed;
        private bool _dialogInitialized;

        public NativeWebDialog()
        {
            // XPF customers don't need a special license to use XPF controls.
#if !WPF
            Core.Licensing.ValidateWebView();
#endif
        }

        private Core.IWebViewAdapter? TryGetAdapter() => TryGetImpl()?.TryGetAdapter();
        private Core.INativeWebViewDialog? TryGetImpl()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NativeWebDialog));
            return _implTcs.Task.Status == TaskStatus.RanToCompletion ? _implTcs.Task.Result : null;
        }

        /// <inheritdoc/>
        public bool CanGoBack => TryGetAdapter()?.CanGoBack ?? false;
        /// <inheritdoc/>
        public bool CanGoForward => TryGetAdapter()?.CanGoForward ?? false;

        /// <inheritdoc/>
        public Uri Source
        {
            get => TryGetAdapter()?.Source ?? _initialSource as Uri ?? Core.WebViewHelper.EmptyPage;
            set
            {
                if (TryGetAdapter() is { } adapter)
                {
                    adapter.Source = value;
                }
                else
                {
                    _initialSource = value;
                }
            }
        }

        /// <inheritdoc/>
        public event EventHandler<Core.WebViewNavigationCompletedEventArgs>? NavigationCompleted
        {
            add
            {
                if (this._navigationCompleted is null
                    && TryGetAdapter() is { } adapter)
                {
                    adapter.NavigationCompleted += WebViewAdapterOnNavigationCompleted;
                }
                this._navigationCompleted += value;
            }
            remove
            {
                this._navigationCompleted -= value;
                if (this._navigationCompleted is null
                    && TryGetAdapter() is { } adapter)
                {
                    adapter.NavigationCompleted -= WebViewAdapterOnNavigationCompleted;
                }
            }
        }

        /// <inheritdoc/>
        public event EventHandler<Core.WebViewNavigationStartingEventArgs>? NavigationStarted
        {
            add
            {
                if (this._navigationStarted is null
                    && TryGetAdapter() is { } adapter)
                {
                    adapter.NavigationStarted += WebViewAdapterOnNavigationStarted;
                }
                this._navigationStarted += value;
            }
            remove
            {
                this._navigationStarted -= value;
                if (this._navigationStarted is null
                    && TryGetAdapter() is { } adapter)
                {
                    adapter.NavigationStarted -= WebViewAdapterOnNavigationStarted;
                }
            }
        }

        /// <inheritdoc/>
        public event EventHandler<Core.WebViewNewWindowRequestedEventArgs>? NewWindowRequested
        {
            add
            {
                if (this._newWindowRequested is null
                    && TryGetAdapter() is { } adapter)
                {
                    adapter.NewWindowRequested += WebViewAdapterOnNewWindowRequested;
                }
                this._newWindowRequested += value;
            }
            remove
            {
                this._newWindowRequested -= value;
                if (this._newWindowRequested is null
                    && TryGetAdapter() is { } adapter)
                {
                    adapter.NewWindowRequested -= WebViewAdapterOnNewWindowRequested;
                }
            }
        }

        /// <inheritdoc/>
        public event EventHandler<Core.WebMessageReceivedEventArgs>? WebMessageReceived
        {
            add
            {
                if (this._webMessageReceived is null
                    && TryGetAdapter() is { } adapter)
                {
                    adapter.WebMessageReceived += WebViewAdapterOnWebMessageReceived;
                }
                this._webMessageReceived += value;
            }
            remove
            {
                this._webMessageReceived -= value;
                if (this._webMessageReceived is null
                    && TryGetAdapter() is { } adapter)
                {
                    adapter.WebMessageReceived -= WebViewAdapterOnWebMessageReceived;
                }
            }
        }

        /// <inheritdoc/>
        public event EventHandler<Core.WebResourceRequestedEventArgs>? WebResourceRequested
        {
            add
            {
                if (this._webResourceRequested is null
                    && TryGetAdapter() is { } adapter)
                {
                    adapter.WebResourceRequested += WebViewAdapterOnWebResourceRequested;
                }
                this._webResourceRequested += value;
            }
            remove
            {
                this._webResourceRequested -= value;
                if (this._webResourceRequested is null
                    && TryGetAdapter() is { } adapter)
                {
                    adapter.WebResourceRequested -= WebViewAdapterOnWebResourceRequested;
                }
            }
        }

        /// <inheritdoc/>
        public bool GoBack() => TryGetAdapter()?.GoBack() ?? false;
        /// <inheritdoc/>
        public bool GoForward() => TryGetAdapter()?.GoForward() ?? false;
        /// <inheritdoc/>
        public Task<string?> InvokeScript(string script)
        {
            if (TryGetAdapter() is { } adapter)
                return adapter.InvokeScript(script);
            else
                return Task.FromException<string?>(new InvalidOperationException(
                    "Unable to invoke script before any page was loaded. Listen for NavigationCompleted event."));
        }

        /// <inheritdoc/>
        public void Navigate(Uri url)
        {
            if (TryGetAdapter() is { } adapter)
                adapter.Navigate(url);
            else
                _initialSource = url;
        }

        /// <inheritdoc/>
        public void NavigateToString(
#if NET8_0_OR_GREATER
            [StringSyntax("html")]
#endif
            string text)
        {
            if (TryGetAdapter() is { } adapter)
                adapter.NavigateToString(text);
            else
                _initialSource = text;
        }

        /// <inheritdoc/>
        public bool Refresh() => TryGetAdapter()?.Refresh() ?? false;
        /// <inheritdoc/>
        public bool Stop() => TryGetAdapter()?.Stop() ?? false;

        /// <inheritdoc/>
        public void Dispose()
        {
            TryGetImpl()?.Dispose();
            _disposed = true;
            _implTcs.TrySetException(new ObjectDisposedException(nameof(NativeWebDialog)));
        }

        /// <inheritdoc cref="Core.INativeWebViewDialog.Title"/>
        public string? Title
        {
            get => _initialTitle ?? TryGetImpl()?.Title;
            set
            {
                if (_initialTitle != value
                    && TryGetImpl() is { } impl)
                {
                    impl.Title = value;
                }
                _initialTitle = value;
            }
        }

        /// <inheritdoc cref="Core.INativeWebViewDialog.CanUserResize"/>
        public bool CanUserResize
        {
            get => _initialCanUserResize ?? TryGetImpl()?.CanUserResize ?? false;
            set
            {
                if (_initialCanUserResize != value
                    && TryGetImpl() is { } impl)
                {
                    impl.CanUserResize = value;
                }
                _initialCanUserResize = value;
            }
        }

        /// <inheritdoc cref="Core.INativeWebViewDialog.Closing"/>
        public event EventHandler? Closing;
        /// <inheritdoc cref="Core.INativeWebViewDialog.Show()"/>
        public void Show() => GetOrInitialize().Show();

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
            var impl = GetOrInitialize();

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
                || !impl.Show(platformHandle))
            {
                impl.Show();
            }
        }

        private Core.INativeWebViewDialog GetOrInitialize()
        {
            if (TryGetImpl() is { } impl)
            {
                return impl;
            }
            Initialize();
            return TryGetImpl() ?? throw new InvalidOperationException("");
        }

        /// <inheritdoc cref="Core.INativeWebViewDialog.Close()"/>
        public void Close() => TryGetImpl()?.Close();

        /// <inheritdoc cref="Core.INativeWebViewDialog.Resize(int, int)"/>
        public bool Resize(int width, int height)
        {
            _initialSize = new PixelSize(width, height);
            TryGetImpl()?.Resize(width, height);
            return true;
        }

        /// <inheritdoc cref="Core.INativeWebViewDialog.Move(int, int)"/>
        public bool Move(int x, int y)
        {
            _initialPosition = new PixelPoint(x, y);
            TryGetImpl()?.Move(x, y);
            return true;
        }

        /// <inheritdoc/>
        public IPlatformHandle? TryGetPlatformHandle() => TryGetImpl()?.TryGetPlatformHandle();

        /// <summary>
        /// Gets platform handle of the webview hosted inside the dialog.
        /// </summary>
        public IPlatformHandle? TryGetWebViewPlatformHandle() => TryGetAdapter();

        public event EventHandler<Core.WebViewAdapterEventArgs>? AdapterCreated;
        public event EventHandler<Core.WebViewAdapterEventArgs>? AdapterDestroyed;
        public event EventHandler<Core.WebViewEnvironmentRequestedEventArgs>? EnvironmentRequested;

        /// <inheritdoc/>
        public Core.NativeWebViewCommandManager? TryGetCommandManager() => TryGetAdapter() switch
        {
            Core.IWebViewAdapterWithCommands commands => new Core.NativeWebViewCommandManager(commands),
            { } adapter => new Core.GenericCommands(adapter),
            _ => null
        };

        /// <inheritdoc/>
        public Core.NativeWebViewCookieManager? TryGetCookieManager() => 
            TryGetAdapter() is Core.IWebViewAdapterWithCookieManager adapter ? new Core.NativeWebViewCookieManager(adapter) : null;

        /// <summary>
        /// If dialog is based on a <see cref="Window"/>, returns its instance to allow full control.
        /// </summary>
        public Window? TryGetWindow() => GetOrInitialize() as WindowNativeWebViewDialog;

        internal bool Show(IPlatformHandle owner) => GetOrInitialize().Show(owner);

        private void Initialize()
        {
            Core.INativeWebViewDialog dialogImpl;

            // Special case for GTK, as we want to use GTK window instead of Avalonia window there.
            if (Core.OperatingSystemEx.IsLinux())
            {
                var args = new GtkWebViewEnvironmentRequestedEventArgs();
                EnvironmentRequested?.Invoke(this, args);
                dialogImpl = new GtkNativeWebViewDialog(args);
            }
            else
            {
                var factory = Core.WebViewAdapter.CreateFactory(args => EnvironmentRequested?.Invoke(this, args));   
                dialogImpl = new WindowNativeWebViewDialog(factory);
            }

            dialogImpl.AdapterCreated += DialogImplOnAdapterCreated;
            dialogImpl.Closing += DialogImplOnClosing;

            if (_initialCanUserResize is not null)
                dialogImpl.CanUserResize = _initialCanUserResize.Value;
            if (_initialTitle is not null)
                dialogImpl.Title = _initialTitle;
            if (_initialPosition is { } position)
                dialogImpl.Move(position.X, position.Y);
            if (_initialSize is { } size)
                dialogImpl.Resize(size.Width, size.Height);

            _implTcs.SetResult(dialogImpl);

            if (dialogImpl.TryGetAdapter() is { } adapter && !_dialogInitialized)
                DialogImplOnAdapterCreated(dialogImpl, new Core.WebViewAdapterEventArgs(adapter));
        }

        private void DialogImplOnAdapterDestroyed(object? sender, Core.WebViewAdapterEventArgs e)
        {
            var dialog = (Core.INativeWebViewDialog)sender!;
            dialog.AdapterCreated -= DialogImplOnAdapterCreated;
            dialog.AdapterDestroyed -= DialogImplOnAdapterDestroyed;

            var adapter = (Core.IWebViewAdapter)e.TryGetPlatformHandle()!;
            adapter.NavigationStarted -= WebViewAdapterOnNavigationStarted;
            adapter.NavigationCompleted -= WebViewAdapterOnNavigationCompleted;
            adapter.WebMessageReceived -= WebViewAdapterOnWebMessageReceived;
            adapter.WebResourceRequested -= WebViewAdapterOnWebResourceRequested;
            adapter.NewWindowRequested -= WebViewAdapterOnNewWindowRequested;
            _dialogInitialized = false;
            AdapterDestroyed?.Invoke(this, e);
        }

        private void DialogImplOnAdapterCreated(object? sender, Core.WebViewAdapterEventArgs e)
        {
            if (_dialogInitialized)
            {
                throw new InvalidOperationException("Dialog was already initialized");
            }

            _dialogInitialized = true;
            var dialog = (Core.INativeWebViewDialog)sender!;
            dialog.AdapterCreated -= DialogImplOnAdapterCreated;
            dialog.AdapterDestroyed += DialogImplOnAdapterDestroyed;

            var adapter = (Core.IWebViewAdapter)e.TryGetPlatformHandle()!;
            if (_navigationStarted is not null)
                adapter.NavigationStarted += WebViewAdapterOnNavigationStarted;
            if (_navigationCompleted is not null)
                adapter.NavigationCompleted += WebViewAdapterOnNavigationCompleted;
            if (_webMessageReceived is not null)
                adapter.WebMessageReceived += WebViewAdapterOnWebMessageReceived;
            if (_webResourceRequested is not null)
                adapter.WebResourceRequested += WebViewAdapterOnWebResourceRequested;
            if (_newWindowRequested is not null)
                adapter.NewWindowRequested += WebViewAdapterOnNewWindowRequested;
            if (_initialSource is Uri url)
                adapter.Source = url;
            else if (_initialSource is string html)
                adapter.NavigateToString(html);
            AdapterCreated?.Invoke(this, e);
        }

        private void DialogImplOnClosing(object? sender, EventArgs e)
        {
            Closing?.Invoke(this, e);
        }

        private void WebViewAdapterOnWebMessageReceived(object? sender, Core.WebMessageReceivedEventArgs e)
        {
            _webMessageReceived?.Invoke(this, e);
        }

        private void WebViewAdapterOnWebResourceRequested(object? sender, Core.WebResourceRequestedEventArgs e)
        {
            _webResourceRequested?.Invoke(this, e);
        }

        private void WebViewAdapterOnNavigationStarted(object? sender, Core.WebViewNavigationStartingEventArgs e)
        {
            _navigationStarted?.Invoke(this, e);
        }

        private void WebViewAdapterOnNavigationCompleted(object? sender, Core.WebViewNavigationCompletedEventArgs e)
        {
            _navigationCompleted?.Invoke(this, e);
        }

        private void WebViewAdapterOnNewWindowRequested(object? sender, Core.WebViewNewWindowRequestedEventArgs e)
        {
            _newWindowRequested?.Invoke(this, e);
        }
    }
}
