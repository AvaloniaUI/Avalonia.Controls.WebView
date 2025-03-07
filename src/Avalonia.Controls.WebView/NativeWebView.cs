#if AVALONIA || WPF
using System;
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
using System.Diagnostics.CodeAnalysis;
#elif AVALONIA
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
    public class NativeWebView : Control, Core.IWebView
    {
        private bool _ignoreNavigation;
        private bool _ignoreFocusChanges;

#if WPF
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source), typeof(Uri), typeof(NativeWebView),
            new PropertyMetadata(new Uri("about:blank"), SourcePropertyChangedCallback));
#elif AVALONIA
        public static readonly StyledProperty<Uri> SourceProperty = AvaloniaProperty.Register<NativeWebView, Uri>(
            nameof(Source), new Uri("about:blank"));
#endif

        private readonly NativeWebViewControlHost _controlHostImpl;

        public NativeWebView()
        {
            _controlHostImpl = new NativeWebViewControlHost();
            _controlHostImpl.AdapterInitialized += ControlHostImplOnAdapterInitialized;
            _controlHostImpl.AdapterDeinitialized += ControlHostImplOnAdapterDeinitialized;
#if AVALONIA
            VisualChildren.Add(_controlHostImpl);
#elif WPF
            IsVisibleChanged += OnIsVisibleChanged;
            AddVisualChild(_controlHostImpl);
            AddLogicalChild(_controlHostImpl);
#endif
        }

#if WPF
        protected override int VisualChildrenCount => 1;
        protected override System.Windows.Media.Visual? GetVisualChild(int index) => _controlHostImpl;
#endif

        /// <inheritdoc/>
        public event EventHandler<Core.WebViewNavigationCompletedEventArgs>? NavigationCompleted;
        /// <inheritdoc/>
        public event EventHandler<Core.WebViewNavigationStartingEventArgs>? NavigationStarted;
        /// <inheritdoc/>
        public event EventHandler<Core.WebMessageReceivedEventArgs>? WebMessageReceived;

        /// <inheritdoc/>
        public Uri Source
        {
            get => (Uri)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Returns instance <see cref="NativeWebViewCommandManager"/> that allows executing common keyboard commands. Or null, if not supported by the platform.
        /// </summary>
        public Core.NativeWebViewCommandManager? TryGetCommandManager() =>
            _controlHostImpl.TryGetAdapter() is Core.IWebViewAdapterWithCommands commands ? new(commands) : null;

        /// <summary>
        /// Returns instance <see cref="NativeWebViewCookieManager"/> that allows reading and settings cookies. Or null, if not supported by the platform.
        /// </summary>
        public Core.NativeWebViewCookieManager? TryGetCookieManager() =>
            _controlHostImpl.TryGetAdapter() is Core.IWebViewAdapterWithCookieManager adapter ? new(adapter) : null;

        /// <inheritdoc/>
        public bool CanGoBack => _controlHostImpl.TryGetAdapter()?.CanGoBack ?? false;

        /// <inheritdoc/>
        public bool CanGoForward => _controlHostImpl.TryGetAdapter()?.CanGoForward ?? false;

        /// <inheritdoc/>
        public bool GoBack() => _controlHostImpl.TryGetAdapter()?.GoBack() ?? false;

        /// <inheritdoc/>
        public bool GoForward() => _controlHostImpl.TryGetAdapter()?.GoForward() ?? false;

        /// <inheritdoc/>
        public async Task<string?> InvokeScript(string scriptName)
        {
            try
            {
                var adapter = await _controlHostImpl.GetAdapterAsync();
                return await adapter.InvokeScript(scriptName);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public async void Navigate(Uri url)
        {
            try
            {
                var adapter = await _controlHostImpl.GetAdapterAsync();
                adapter.Navigate(url);
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <inheritdoc/>
        public async void NavigateToString(string text)
        {
            try
            {
                var adapter = await _controlHostImpl.GetAdapterAsync();
                adapter.NavigateToString(text);
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <inheritdoc/>
        public bool Refresh() => _controlHostImpl.TryGetAdapter()?.Refresh() ?? false;

        /// <inheritdoc/>
        public bool Stop() => _controlHostImpl.TryGetAdapter()?.Stop() ?? false;

        /// <summary>
        /// Returns a platform handle of the native control.
        /// </summary>
        /// <remarks>
        /// Return handle can be used to access additional native APIs by using it with PInvokes. 
        /// </remarks>
        public IPlatformHandle? TryGetPlatformHandle() => _controlHostImpl.TryGetAdapter();

        /// <inheritdoc cref="BeginReparentingAsync" />
        public IDisposable BeginReparenting(bool yieldOnLayoutBeforeExiting = true)
        {
            return _controlHostImpl.BeginReparenting(yieldOnLayoutBeforeExiting);
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
            return _controlHostImpl.BeginReparentingAsync();
        }

        private void WebViewAdapterOnWebMessageReceived(object? sender, Core.WebMessageReceivedEventArgs e)
        {
            WebMessageReceived?.Invoke(this, e);
        }

        private void WebViewAdapterOnNavigationStarted(object? sender, Core.WebViewNavigationStartingEventArgs e)
        {
            NavigationStarted?.Invoke(this, e);
        }

        private void WebViewAdapterOnNavigationCompleted(object? sender, Core.WebViewNavigationCompletedEventArgs e)
        {
            _ignoreNavigation = true;
            try
            {
                SetCurrentValue(SourceProperty, e.Request);
                NavigationCompleted?.Invoke(this, e);
            }
            finally
            {
                _ignoreNavigation = false;
            }
        }

        private void WithFocusOnGotFocus(object sender, EventArgs e)
        {
            _ignoreFocusChanges = true;
            try
            {
#if WPF
                (s_getXpfHostDelegate(this) as Avalonia.Input.IInputElement)?.Focus();
                Keyboard.Focus(this);
#elif AVALONIA
                var focusManager = TopLevel.GetTopLevel(this)?.FocusManager;
                if (focusManager != this)
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

        private void WithFocusOnLostFocus(object sender, EventArgs e)
        {
            // no-op?
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

        private void ControlHostImplOnAdapterDeinitialized(object sender, Core.IWebViewAdapter adapter)
        {
            adapter.NavigationStarted -= WebViewAdapterOnNavigationStarted;
            adapter.NavigationCompleted -= WebViewAdapterOnNavigationCompleted;
            adapter.WebMessageReceived -= WebViewAdapterOnWebMessageReceived;
            if (adapter is Core.IWebViewAdapterWithFocus withFocus)
            {
                withFocus.LostFocus -= WithFocusOnLostFocus;
                withFocus.GotFocus -= WithFocusOnGotFocus;
            }
            if (adapter is Core.IWebViewAdapterWithInputRedirect withInput)
            {
                withInput.Input -= WithInputOnInput;
            }
        }

        private void ControlHostImplOnAdapterInitialized(object sender, Core.IWebViewAdapter adapter)
        {
            adapter.NavigationStarted += WebViewAdapterOnNavigationStarted;
            adapter.NavigationCompleted += WebViewAdapterOnNavigationCompleted;
            adapter.WebMessageReceived += WebViewAdapterOnWebMessageReceived;
            if (adapter is Core.IWebViewAdapterWithFocus withFocus)
            {
                withFocus.LostFocus += WithFocusOnLostFocus;
                withFocus.GotFocus += WithFocusOnGotFocus;
            }

            if (adapter is Core.IWebViewAdapterWithInputRedirect withInput)
            {
                withInput.Input += WithInputOnInput;
            }

#if WPF
            if (ReadLocalValue(SourceProperty) != DependencyProperty.UnsetValue
#elif AVALONIA
            if (IsSet(SourceProperty)
#endif
                && Source is { } source
                && adapter.Source != source)
            {
                adapter.Navigate(source);
            }
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

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _ = Dispatcher.InvokeAsync(() => _controlHostImpl.TryGetAdapter()?.SizeChanged(), DispatcherPriority.Background);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            _controlHostImpl.TryGetAdapter()?.SizeChanged();
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);
            _controlHostImpl.TryGetAdapter()?.SizeChanged();
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
                _ = Dispatcher.UIThread.InvokeAsync(() => _controlHostImpl.TryGetAdapter()?.SizeChanged(), DispatcherPriority.Background);
            }
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            _controlHostImpl.TryGetAdapter()?.SizeChanged();
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
                && _controlHostImpl.TryGetAdapter() is Core.IWebViewAdapterWithFocus adapterWithFocus)
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
                && _controlHostImpl.TryGetAdapter() is Core.IWebViewAdapterWithFocus adapterWithFocus)
            {
                _ = adapterWithFocus.ResignFocus();
            }
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
