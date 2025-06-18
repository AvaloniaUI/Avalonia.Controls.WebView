#if AVALONIA || WPF
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using IPlatformHandle = Avalonia.Platform.IPlatformHandle;
using AvInput = Avalonia.Input;
using Core = Avalonia.Controls;
#if WPF
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Media;

#elif AVALONIA
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Interactivity;
#endif

#if AVALONIA
namespace Avalonia.Controls
#elif WPF
namespace Avalonia.Xpf.Controls
#endif
{
    /// <summary>
    /// NativeWebView is a control that provides a native web browser implementation for applications.
    /// It wraps platform-specific web controls and provides a unified API for web browsing functionality.
    /// </summary>
    // ReSharper disable RedundantNameQualifier
    public class NativeWebView : Control, Core.IWebView, Core.IWebViewHolder
    {
        private bool _ignoreNavigation;
        private bool _ignoreFocusChanges;
        private object? _initialSource;

        private EventHandler<Core.WebViewNavigationCompletedEventArgs>? _navigationCompleted;
        private EventHandler<Core.WebViewNavigationStartingEventArgs>? _navigationStarted;
        private EventHandler<Core.WebViewNewWindowRequestedEventArgs>? _newWindowRequested;
        private EventHandler<Core.WebMessageReceivedEventArgs>? _webMessageReceived;
        private EventHandler<Core.WebResourceRequestedEventArgs>? _webResourceRequested;

#if WPF
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source), typeof(Uri), typeof(NativeWebView),
            new PropertyMetadata(new Uri("about:blank"), SourcePropertyChangedCallback));
#elif AVALONIA
        public static readonly StyledProperty<Uri> SourceProperty = AvaloniaProperty.Register<NativeWebView, Uri>(
            nameof(Source), new Uri("about:blank"));
        public static readonly StyledProperty<IBrush?> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<NativeWebView>();
#endif

        private readonly TaskCompletionSource<INativeWebViewControlImpl> _controlHostImplTcs = new();

        static NativeWebView()
        {
#if WPF
            FocusableProperty.OverrideMetadata(typeof(NativeWebView), new FrameworkPropertyMetadata(true));
            BackgroundProperty.OverrideMetadata(typeof(NativeWebView), new FrameworkPropertyMetadata(BackgroundPropertyChangedCallback));
#elif AVALONIA
            FocusableProperty.OverrideDefaultValue<NativeWebView>(true);
#endif
        }

        public NativeWebView()
        {
            // XPF customers don't need a special license to use XPF controls.
#if !WPF
            Core.Licensing.ValidateWebView();
#endif
            
            Initialized += OnInitialized;
            _navigationCompleted += (_, e) =>
            {
                _ignoreNavigation = true;
                try
                {
                    SetCurrentValue(SourceProperty, e.Request ?? Core.WebViewHelper.EmptyPage);
                }
                finally
                {
                    _ignoreNavigation = false;
                }
            };
        }

        internal INativeWebViewControlImpl? TryGetImpl() =>
            _controlHostImplTcs.Task is { Status: TaskStatus.RanToCompletion } t ? t.Result : null;

        internal Core.IWebViewAdapter? TryGetAdapter() => TryGetImpl()?.TryGetAdapter();

#if WPF
        protected override int VisualChildrenCount => TryGetImpl() is not null ? 1 : 0;
        protected override System.Windows.Media.Visual? GetVisualChild(int index) => (System.Windows.Media.Visual?)TryGetImpl();
#endif

        /// <inheritdoc/>
        public event EventHandler<Core.WebViewEnvironmentRequestedEventArgs>? EnvironmentRequested;

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
        public event EventHandler<Core.WebViewAdapterEventArgs>? AdapterCreated;

        /// <inheritdoc/>
        public event EventHandler<Core.WebViewAdapterEventArgs>? AdapterDestroyed;

        /// <inheritdoc/>
        public Uri Source
        {
            get => (Uri)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

#if AVALONIA
        public IBrush? Background
        {
            get => GetValue(BackgroundProperty);
#else
        public new Brush? Background
        {
            get => (Brush)GetValue(BackgroundProperty);
#endif
            set => SetValue(BackgroundProperty, value);
        }

        /// <inheritdoc/>
        public Core.NativeWebViewCommandManager? TryGetCommandManager() =>
            TryGetAdapter() switch
            {
                Core.IWebViewAdapterWithCommands commands => new Core.NativeWebViewCommandManager(commands),
                { } adapter => new Core.GenericCommands(adapter),
                _ => null
            };

        /// <inheritdoc/>
        public Core.NativeWebViewCookieManager? TryGetCookieManager() =>
            TryGetAdapter() is Core.IWebViewAdapterWithCookieManager adapter ? new(adapter) : null;

        /// <inheritdoc/>
        public bool CanGoBack => TryGetAdapter()?.CanGoBack ?? false;

        /// <inheritdoc/>
        public bool CanGoForward => TryGetAdapter()?.CanGoForward ?? false;

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
        public IPlatformHandle? TryGetPlatformHandle() => TryGetAdapter();

        /// <inheritdoc cref="BeginReparentingAsync" />
        public IDisposable BeginReparenting(bool yieldOnLayoutBeforeExiting = true)
        {
            return TryGetImpl()?.BeginReparenting(yieldOnLayoutBeforeExiting) ?? EmptyDisposable.Instance;
        }

        /// <summary>
        /// This method delays destruction of the native control, ignoring any Loaded/Unloaded events and keeping control alive.
        /// When <see cref="BeginReparenting"/> scope is ended (return value is disposed),
        /// <see cref="NativeWebView"/> will reparent existing native control to a new parent, if it exist.
        /// Or destroys native control, if <see cref="NativeWebView"/> is not attached to any parent.
        /// Without <see cref="BeginReparenting"/> executed, native control will always be destroyed, when it's detached from parent before attaching to a new one.
        /// </summary>
        /// <returns>Reparenting scope. Disposing returned value will re-evaluate <see cref="NativeWebView"/> native control parenting.</returns>
        public IAsyncDisposable BeginReparentingAsync()
        {
            return TryGetImpl()?.BeginReparentingAsync() ?? EmptyDisposable.Instance;
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

        private void OnInitialized(object? sender, EventArgs e)
#if WPF
            // Due to differences in initialization order between Avalonia and WPF, we delay adapter creation on WPF,
            // because it's happening way too early there, even before subscribers were ready  
            => Dispatcher.CurrentDispatcher.InvokeAsync(() =>
#endif
        {
            var adapterFactory = Core.WebViewAdapter.CreateFactory(args => EnvironmentRequested?.Invoke(this, args));
            INativeWebViewControlImpl controlHostImpl = adapterFactory switch
            {
#if !WPF
                Core.WebViewAdapter.CompositorHostAdapterFactory comp => new NativeWebViewCompositorHost(comp),
#endif
                Core.WebViewAdapter.NativeHostAdapterFactory native => new NativeWebViewControlHost(native),
                _ => new EmptyNativeWebViewControlImpl()
            };

            controlHostImpl.AdapterCreated += ControlHostImplOnAdapterCreated;
            controlHostImpl.AdapterDestroyed += ControlHostImplOnAdapterDeinitialized;
#if AVALONIA
            VisualChildren.Add((Control)controlHostImpl);
#elif WPF
            IsVisibleChanged += OnIsVisibleChanged;
            AddVisualChild((System.Windows.Media.Visual)controlHostImpl);
            AddLogicalChild((System.Windows.Media.Visual)controlHostImpl);
#endif

            _controlHostImplTcs.TrySetResult(controlHostImpl);
        }
#if WPF
        );
#endif

        private void WithFocusOnGotFocus(object? sender, EventArgs e)
        {
            _ignoreFocusChanges = true;
            try
            {
#if WPF
                RaiseEvent(new RoutedEventArgs(GotFocusEvent));
                (s_getXpfHostDelegate(this) as Avalonia.Input.IInputElement)?.Focus();
                Keyboard.Focus(this);
#elif AVALONIA
                var focusManager = TopLevel.GetTopLevel(this)?.FocusManager;
                if (!ReferenceEquals(focusManager?.GetFocusedElement(), this))
                {
                    Focus();
                }
#endif
            }
            finally
            {
                _ignoreFocusChanges = false;
            }
        }

        private void WithFocusOnLostFocus(object? sender, Core.IWebViewAdapterWithFocus.LostFocusDirection e)
        {
            // TODO: add avalonia APIs once possible
#if WPF
            switch (e)
            {
                case Core.IWebViewAdapterWithFocus.LostFocusDirection.Next:
                    MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    break;
                case Core.IWebViewAdapterWithFocus.LostFocusDirection.Previous:
                    MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
                    break;
            }
            RaiseEvent(new RoutedEventArgs(LostFocusEvent));
#endif
        }

        private void WithInputOnInput(global::Avalonia.Interactivity.RoutedEventArgs obj)
        {
            AvInput.IInputElement? element;
#if AVALONIA
            element = this;
#elif WPF
            element = s_getXpfHostDelegate(this) as AvInput.IInputElement;
#endif
            element?.RaiseEvent(obj);
        }

        private void ControlHostImplOnAdapterDeinitialized(object? sender, Core.IWebViewAdapter adapter)
        {
            adapter.NavigationStarted -= WebViewAdapterOnNavigationStarted;
            adapter.NavigationCompleted -= WebViewAdapterOnNavigationCompleted;
            adapter.WebMessageReceived -= WebViewAdapterOnWebMessageReceived;
            adapter.WebResourceRequested -= WebViewAdapterOnWebResourceRequested;
            adapter.NewWindowRequested -= WebViewAdapterOnNewWindowRequested;
            if (adapter is Core.IWebViewAdapterWithFocus withFocus)
            {
                withFocus.LostFocus -= WithFocusOnLostFocus;
                withFocus.GotFocus -= WithFocusOnGotFocus;
            }
            if (adapter is Core.IWebViewAdapterWithInputRedirect withInput)
            {
                withInput.Input -= WithInputOnInput;
            }

            AdapterDestroyed?.Invoke(this, new Core.WebViewAdapterEventArgs(adapter));
        }

        private void ControlHostImplOnAdapterCreated(object? sender, Core.IWebViewAdapter adapter)
        {
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
            if (adapter is Core.IWebViewAdapterWithFocus withFocus)
            {
                withFocus.LostFocus += WithFocusOnLostFocus;
                withFocus.GotFocus += WithFocusOnGotFocus;

                if (IsFocused)
                {
                    withFocus.Focus();
                }
            }

            if (adapter is Core.IWebViewAdapterWithInputRedirect withInput)
            {
                withInput.Input += WithInputOnInput;
            }

            if (_initialSource is Uri url)
            {
                adapter.Navigate(url);
            }
            else if (_initialSource is string html)
            {
                adapter.NavigateToString(html);
            }

            EnsureBackground(Background);
            AdapterCreated?.Invoke(this, new Core.WebViewAdapterEventArgs(adapter));
        }

#if WPF
        private static void SourcePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var @this = (NativeWebView)d;
            if (!@this._ignoreNavigation
                && e.NewValue is Uri source)
            {
                @this.Navigate(source);
            }
        }

        private static void BackgroundPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var @this = (NativeWebView)d;
            if (e.NewValue is Brush background)
            {
                @this.EnsureBackground(background);
            }
        }

        private void OnIsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            _ = Dispatcher.InvokeAsync(() => TryGetAdapter()?.SizeChanged(PixelSize.FromSizeWithDpi(new Size(RenderSize.Width, RenderSize.Height), VisualTreeHelper.GetDpi(this).DpiScaleX)), DispatcherPriority.Background);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            TryGetAdapter()?.SizeChanged(PixelSize.FromSizeWithDpi(new Size(sizeInfo.NewSize.Width, sizeInfo.NewSize.Height), VisualTreeHelper.GetDpi(this).DpiScaleX));
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);
            TryGetAdapter()?.SizeChanged(PixelSize.FromSizeWithDpi(new Size(RenderSize.Width, RenderSize.Height), newDpi.DpiScaleX));
        }

        private void EnsureBackground(Brush? background)
        {
            if (TryGetAdapter() is { } adapter)
            {
                if (background is null)
                {
                    var window = Window.GetWindow(this);
                    background = window?.Background ?? Brushes.White;
                }
                adapter.DefaultBackground = background switch
                {
                    SolidColorBrush solid => new Avalonia.Media.Color(solid.Color.A, solid.Color.B, solid.Color.G, solid.Color.B),
                    // non-solid brush - try to emulate in OnRender with our own rendering
                    _ => Avalonia.Media.Colors.Transparent
                };
                InvalidateVisual();
            }
        }

#elif AVALONIA
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == SourceProperty)
            {
                if (!_ignoreNavigation
                    && change.GetNewValue<Uri?>() is { } source)
                {
                    Navigate(source);
                }
            }
            else if (change.Property == IsVisibleProperty)
            {
                _ = Dispatcher.UIThread.InvokeAsync(() => TryGetAdapter()?.SizeChanged(
                    PixelSize.FromSize(Bounds.Size, TopLevel.GetTopLevel(this)!.RenderScaling)), DispatcherPriority.Background);
            }
            else if (change.Property == BackgroundProperty)
            {
                EnsureBackground(change.GetNewValue<IBrush>());
            }
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            TryGetAdapter()?.SizeChanged(PixelSize.FromSize(e.NewSize, TopLevel.GetTopLevel(this)!.RenderScaling));
        }

        private void EnsureBackground(IBrush? background)
        {
            if (TryGetAdapter() is { } adapter)
            {
                if (background is null)
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    while (topLevel is IPopupHost)
                        topLevel = TopLevel.GetTopLevel(topLevel.Parent as Visual);
                    background = topLevel?.Background ?? Brushes.White;
                }
                adapter.DefaultBackground = background switch
                {
                    ISolidColorBrush solid => new Avalonia.Media.Color(solid.Color.A, solid.Color.R, solid.Color.G, solid.Color.B),
                    // non-solid brush - try to emulate in OnRender with our own rendering
                    _ => Avalonia.Media.Colors.Transparent
                };
                InvalidateVisual();
            }
        }
#endif

#if WPF
        protected override void OnGotFocus(RoutedEventArgs e)
#elif AVALONIA
        protected override void OnGotFocus(GotFocusEventArgs e)
#endif
        {
            base.OnGotFocus(e);
            if (!_ignoreFocusChanges
                && TryGetAdapter() is Core.IWebViewAdapterWithFocus adapterWithFocus)
            {
                _ = adapterWithFocus.Focus();
            }
        }

#if WPF
        protected override void OnLostFocus(RoutedEventArgs e)
#elif AVALONIA
        protected override void OnLostFocus(RoutedEventArgs e)
#endif
        {
            base.OnLostFocus(e);
            if (!_ignoreFocusChanges
                && TryGetAdapter() is Core.IWebViewAdapterWithFocus adapterWithFocus)
            {
                _ = adapterWithFocus.ResignFocus();
            }
        }
#endif

#if WPF
        protected override void OnRender(DrawingContext context)
        {
            var backgroundToDraw = Background is { } brush and not SolidColorBrush ? brush : Brushes.Transparent;
            context.DrawRectangle(backgroundToDraw, null, new System.Windows.Rect(default, RenderSize));
            base.OnRender(context);
        }
#elif AVALONIA

        public override void Render(DrawingContext context)
        {
            var backgroundToDraw = Background is { } brush and not ISolidColorBrush ? brush : Brushes.Transparent;
            context.DrawRectangle(backgroundToDraw, null, Bounds);
            base.Render(context);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (TryGetAdapter() is IWebViewAdapterWithOffscreenInput input)
            {
                e.Handled = input.PointerInput(e.GetCurrentPoint(this), e.KeyModifiers);
            }
            base.OnPointerPressed(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (TryGetAdapter() is IWebViewAdapterWithOffscreenInput input)
            {
                e.Handled = input.PointerInput(e.GetCurrentPoint(this), e.KeyModifiers);
            }
            base.OnPointerReleased(e);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (TryGetAdapter() is IWebViewAdapterWithOffscreenInput input)
            {
                e.Handled = input.PointerInput(e.GetCurrentPoint(this), e.KeyModifiers);
            }
            base.OnPointerMoved(e);
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            if (TryGetAdapter() is IWebViewAdapterWithOffscreenInput input)
            {
                e.Handled = input.PointerWheelInput(e.Delta, e.GetCurrentPoint(this), e.KeyModifiers);
            }
            base.OnPointerWheelChanged(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (TryGetAdapter() is IWebViewAdapterWithOffscreenInput input)
            {
                e.Handled = input.KeyInput(true, e.PhysicalKey, e.KeySymbol, e.KeyModifiers);
            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (TryGetAdapter() is IWebViewAdapterWithOffscreenInput input)
            {
                e.Handled = input.KeyInput(false, e.PhysicalKey, e.KeySymbol, e.KeyModifiers);
            }
            base.OnKeyUp(e);
        }
#endif

#if WPF
        private static readonly Func<System.Windows.Media.Visual?, object?> s_getXpfHostDelegate = GetXpfHostDelegate();
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Atlantis.AtlantisPresentationCoreExtensions", "PresentationCore")]
        private static Func<System.Windows.Media.Visual?, object?> GetXpfHostDelegate()
        {
            var members = Type.GetType("Atlantis.AtlantisPresentationCoreExtensions, PresentationCore", false)
                ?.FindMembers(
                    MemberTypes.Method,
                    BindingFlags.Public | BindingFlags.Static,
                    static (m, _) => m.Name == "GetXpfHost" && ((MethodInfo)m).GetParameters().Length == 1,
                    null);
            if (members?.Length == 1)
            {
                return (Func<System.Windows.Media.Visual?, object?>)Delegate.CreateDelegate(typeof(Func<System.Windows.Media.Visual?, object?>), null, (MethodInfo)members[0]);
            }
            return _ => null;
        }
#endif
    }
}
