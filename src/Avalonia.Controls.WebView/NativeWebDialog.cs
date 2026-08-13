using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Controls.Gtk;
using AvPlatform = Avalonia.Platform;
using Core = Avalonia.Controls;
using IPlatformHandle = Avalonia.Platform.IPlatformHandle;
#if WPF
using AvaloniaUI.Xpf.WpfAbstractions;
using Window = System.Windows.Window;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
#elif AVALONIA
using Window = Avalonia.Controls.Window;
using Color = Avalonia.Media.Color;
using Colors = Avalonia.Media.Colors;
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
        private object? _lastSource;
        private string? _initialTitle;
        private bool? _initialCanUserResize;
        private PixelSize? _initialSize;
        private PixelPoint? _initialPosition;
        private Color? _initialDefaultBackground;
        private bool _disposed;
        private bool _dialogInitialized;
        private bool _shown;
        private bool _focusRequested;

        static NativeWebDialog()
        {
#if WPF
            WpfWebViewDispatcher.Setup();
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

        /// <summary>
        /// Gets or sets the user agent string used by the WebView for HTTP requests.
        /// Returns null if the underlying adapter is not yet initialized or the platform does not support reading the user agent.
        /// Setting to null resets to the default user agent.
        /// </summary>
        public string? UserAgent
        {
            get => TryGetAdapter()?.UserAgent;
            set
            {
                if (TryGetAdapter() is { } adapter)
                    adapter.UserAgent = value;
            }
        }

        /// <inheritdoc/>
        public Uri Source
        {
            get => TryGetAdapter()?.Source ?? _lastSource as Uri ?? Core.WebViewHelper.EmptyPage;
            set
            {
                _lastSource = value;
                if (TryGetAdapter() is { } adapter)
                {
                    adapter.Source = value;
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
            _lastSource = url;
            TryGetAdapter()?.Navigate(url);
        }

        /// <inheritdoc/>
        public void NavigateToString([StringSyntax("html")] string text, Uri? baseUri = null)
        {
            _lastSource = (text, baseUri);
            TryGetAdapter()?.NavigateToString(text, baseUri);
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

        /// <summary>
        /// Gets or sets the background color of the dialog and of the webview hosted inside of it.
        /// If null, the owner background is used, falling back to white.
        /// </summary>
        public Color? DefaultBackground
        {
            get => _initialDefaultBackground;
            set
            {
                _initialDefaultBackground = value;
                if (value is { } color
                    && TryGetImpl() is { } impl)
                {
                    impl.DefaultBackground = new Media.Color(color.A,  color.R, color.G, color.B);
                }
            }
        }

        /// <summary>
        /// Gets or sets if the dialog moves keyboard focus to its web content when shown. Default is true.
        /// </summary>
        public bool ShowFocused { get; set; } = true;

        /// <inheritdoc cref="Core.INativeWebViewDialog.Closing"/>
        public event EventHandler? Closing;
        /// <inheritdoc cref="Core.INativeWebViewDialog.Show()"/>
        public async void Show()
        {
            (await GetOrInitialize()).Show();
            OnShown();
        }

#if WPF
        /// <summary>
        /// Opens the WebView dialog with <see cref="Window"/> owner.
        /// </summary>
        public async void Show(Window owner)
#elif AVALONIA
        /// <summary>
        /// Opens the WebView dialog with <see cref="TopLevel"/> owner.
        /// </summary>
        public async void Show(TopLevel owner)
#endif
        {
            var impl = await GetOrInitialize();

            // Not stored in _initialDefaultBackground, so the owner is still resolved again on the next Show call.
            if (_initialDefaultBackground is null)
            {
                var color = GetOwnerBackground(owner);
                impl.DefaultBackground = new Media.Color(color.A, color.R, color.G, color.B);
            }

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

            OnShown();
        }

        /// <summary>
        /// Activates the dialog and moves keyboard focus to the web content hosted inside of it.
        /// </summary>
        public void Focus()
        {
            _focusRequested = true;
            TryApplyFocus();
        }

        private void OnShown()
        {
            _shown = true;
            _focusRequested |= ShowFocused;
            TryApplyFocus();
        }

        private void TryApplyFocus()
        {
            // The adapter is typically created only after the dialog window was shown,
            // and native focus can't be moved before that.
            if (!_focusRequested || !_shown
                || TryGetImpl() is not { } impl
                || impl.TryGetAdapter() is null)
            {
                return;
            }

            _focusRequested = false;
            impl.Focus();
        }

#if WPF
        private static Color GetOwnerBackground(Window owner) =>
            owner.Background is System.Windows.Media.SolidColorBrush solid ?
                solid.Color : Colors.White;
#elif AVALONIA
        private static Color GetOwnerBackground(TopLevel owner) =>
            owner.Background is Media.ISolidColorBrush solid ?
                solid.Color :Colors.White;
#endif

        private async Task<Core.INativeWebViewDialog> GetOrInitialize()
        {
            if (TryGetImpl() is { } impl)
            {
                return impl;
            }
            await Initialize();
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

        /// <inheritdoc/>
        public void ShowPrintUI()
        {
            if (TryGetAdapter() is not Core.IWebViewWithPrint adapter
                || !adapter.ShowPrintUI())
            {
                InvokeScript("window.print();");
            }
        }

        /// <inheritdoc/>
        public Task<Stream> PrintToPdfStreamAsync() => TryGetAdapter() is Core.IWebViewWithPrint adapter ?
            adapter.PrintToPdfStreamAsync() :
            Task.FromException<Stream>(new PlatformNotSupportedException());

        /// <inheritdoc cref="PrintToPdfStreamAsync()"/>
        [UnsupportedOSPlatform("macos")]
        [UnsupportedOSPlatform("ios")]
        public Task<Stream> PrintToPdfStreamAsync(AvPlatform.WebViewPrintSettings printSettings) => TryGetAdapter() is Core.IWebViewWithPrintWithOptions adapter ?
            adapter.PrintToPdfStreamAsync(printSettings) :
            Task.FromException<Stream>(new PlatformNotSupportedException());

        /// <summary>
        /// If dialog is based on a <see cref="Window"/>, returns its instance to allow full control.
        /// </summary>
#if ANDROID || BROWSER
        public Window? TryGetWindow() => null;
#else
        public Window? TryGetWindow() => GetOrInitialize() is { IsCompleted: true, IsFaulted: false } task ?
            task.Result as WindowNativeWebViewDialog :
            null;
#endif

        internal async Task<bool> Show(IPlatformHandle owner) => (await GetOrInitialize()).Show(owner);

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task Initialize()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
#if ANDROID
            var dialogImpl = new Android.AndroidNativeWebViewDialog(args => EnvironmentRequested?.Invoke(this, args));
#elif BROWSER
            var dialogImpl = new Browser.BrowserWindowNativeWebViewDialog(args => EnvironmentRequested?.Invoke(this, args));
#else
            Core.INativeWebViewDialog dialogImpl;
            // Special case for GTK, as we want to use GTK window instead of Avalonia window there.
            if (OperatingSystem.IsLinux() && !Core.WebViewAdapter.UseHeadless)
            {
                dialogImpl = await GtkNativeWebViewDialog.CreateAsync(args => EnvironmentRequested?.Invoke(this, args));
            }
            else
            {
                // Don't await factoryTask here, we want to get Window accessible as early as possible
                var factoryTask = Core.WebViewAdapter.CreateFactory(args => EnvironmentRequested?.Invoke(this, args));   
                dialogImpl = new WindowNativeWebViewDialog(factoryTask);
            }
#endif

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
            if (_initialDefaultBackground is { } color)
                dialogImpl.DefaultBackground = new Media.Color(color.A,  color.R, color.G, color.B);

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
            _shown = false;
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
            if (_lastSource is Uri url)
                adapter.Source = url;
            else if (_lastSource is ValueTuple<string, Uri?> pair)
                adapter.NavigateToString(pair.Item1, pair.Item2);
            AdapterCreated?.Invoke(this, e);

            TryApplyFocus();
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
