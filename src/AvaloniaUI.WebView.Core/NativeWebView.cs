#if AVALONIA || WPF
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AvaloniaUI.WebView.Macios;
using IPlatformHandle = Avalonia.Platform.IPlatformHandle;
#if WPF
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NativeControlHost = AvaloniaUI.Xpf.WpfAbstractions.NativeControlHost;
#elif AVALONIA
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Interactivity;
#endif

namespace AvaloniaUI.WebView;

public class NativeWebView : NativeControlHost, IWebView
{
    private bool _ignoreNavigation;
    private bool _ignoreFocusChanges;
    private TaskCompletionSource<IWebViewAdapter> _webViewReadyCompletion = new();

    public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;

    public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;

#if WPF
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(Uri), typeof(NativeWebView),
        new PropertyMetadata(new Uri("about:blank"), SourcePropertyChangedCallback));
#elif AVALONIA
    public static readonly StyledProperty<Uri> SourceProperty = AvaloniaProperty.Register<NativeWebView, Uri>(
        nameof(Source), new Uri("about:blank"));
#endif

    static NativeWebView()
    {
#if WPF
        FocusableProperty.OverrideMetadata(typeof(NativeWebView), new UIPropertyMetadata(true));
#elif AVALONIA
        FocusableProperty.OverrideDefaultValue<NativeWebView>(true);
#endif
    }

    public NativeWebView()
    {
#if WPF
        IsVisibleChanged += OnIsVisibleChanged;
#endif
    }

    public Uri Source
    {
        get => (Uri)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public NativeWebViewCommandManager? TryGetCommandManager() =>
        TryGetAdapter() is IWebViewAdapterWithCommands commands ? new NativeWebViewCommandManager(commands) : null;

    public NativeWebViewCookieManager? TryGetCookieManager() =>
        TryGetAdapter() is IWebViewAdapterWithCookieManager adapter ? new NativeWebViewCookieManager(adapter) : null;

    public bool CanGoBack => TryGetAdapter()?.CanGoBack ?? false;

    public bool CanGoForward => TryGetAdapter()?.CanGoForward ?? false;

    public bool GoBack() => TryGetAdapter()?.GoBack() ?? false;

    public bool GoForward() => TryGetAdapter()?.GoForward() ?? false;

    public async Task<string?> InvokeScript(string scriptName)
    {
        try
        {
            var adapter = await _webViewReadyCompletion.Task;
            return await adapter.InvokeScript(scriptName);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    public async void Navigate(Uri url)
    {
        try
        {
            var adapter = await _webViewReadyCompletion.Task;
            adapter.Navigate(url);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async void NavigateToString(string text)
    {
        try
        {
            var adapter = await _webViewReadyCompletion.Task;
            adapter.NavigateToString(text);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public bool Refresh() => TryGetAdapter()?.Refresh() ?? false;

    public bool Stop() => TryGetAdapter()?.Stop() ?? false;

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        if (_reparentingScope is not null)
        {
            return _reparentingScope.ReparentRequested(parent);
        }

        IWebViewAdapter? adapter = null;

#if !NETFRAMEWORK
        if (OperatingSystemEx.IsMacOS() || OperatingSystemEx.IsIOS())
        {
            adapter = new MaciosWebViewAdapter();
        }
        else
#endif
        // if (OperatingSystemEx.IsLinux())
        // {
        //     new Gtk.GtkWebView2Adapter();
        //
        //     return base.CreateNativeControlCore(parent);
        //     // adapter = new Gtk.GtkWebView2Adapter();
        // } else
        // if (OperatingSystemEx.IsBrowser())
        // {
        //     adapter = new BrowserIFrameAdapter();
        // } else
#if NET6_0_OR_GREATER || NETFRAMEWORK
        if (OperatingSystemEx.IsWindows())
        {
            if (WebViewHelper.IsMsWebView2Available)
            {
                adapter = new Win.WebView2Adapter(base.CreateNativeControlCore(parent));
            }
            // else if (WebViewCapabilities.IsMsWebView1Available)
            // {
            //     adapter = new Win.WebView1Adapter(base.CreateNativeControlCore(parent));
            // }
            // else if (IE Supported)
            // {
            //    adapter = new Win.WebBrowserAdapter();
            // }
        }
#endif
        if (adapter is null)
        {
            return base.CreateNativeControlCore(parent);
        }

        if (adapter.IsInitialized)
        {
            WebViewAdapterOnInitialized(adapter, EventArgs.Empty);
        }
        else
        {
            adapter.Initialized += WebViewAdapterOnInitialized;
        }

        return adapter;
    }

    private IWebViewAdapter? TryGetAdapter() => _webViewReadyCompletion.Task.Status == TaskStatus.RanToCompletion ?
        _webViewReadyCompletion.Task.Result :
        null;

    private void WebViewAdapterOnWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        WebMessageReceived?.Invoke(this, e);
    }

    private void WebViewAdapterOnNavigationStarted(object? sender, WebViewNavigationStartingEventArgs e)
    {
        NavigationStarted?.Invoke(this, e);
    }

    private void WebViewAdapterOnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
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

    private void WithInputOnInput(Avalonia.Interactivity.RoutedEventArgs obj)
    {
        Avalonia.Input.IInputElement? element;
#if AVALONIA
        element = this;
#elif WPF
        element = s_getXpfHostDelegate(this) as Avalonia.Input.IInputElement;
#endif
        element?.RaiseEvent(obj);
    }

    private void WebViewAdapterOnInitialized(object? sender, EventArgs e)
    {
        var adapter = (IWebViewAdapter)sender!;
        adapter.Initialized -= WebViewAdapterOnInitialized;
        adapter.NavigationStarted += WebViewAdapterOnNavigationStarted;
        adapter.NavigationCompleted += WebViewAdapterOnNavigationCompleted;
        adapter.WebMessageReceived += WebViewAdapterOnWebMessageReceived;
        if (adapter is IWebViewAdapterWithFocus withFocus)
        {
            withFocus.LostFocus += WithFocusOnLostFocus;
            withFocus.GotFocus += WithFocusOnGotFocus;
        }

        if (adapter is IWebViewAdapterWithInputRedirect withInput)
        {
            withInput.Input += WithInputOnInput;
        }

        _webViewReadyCompletion.TrySetResult(adapter);

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
        _ = Dispatcher.InvokeAsync(() => TryGetAdapter()?.SizeChanged(), DispatcherPriority.Background);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        TryGetAdapter()?.SizeChanged();
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        TryGetAdapter()?.SizeChanged();
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
        else if (change.Property == BoundsProperty)
        {
            TryGetAdapter()?.SizeChanged();
        }
        else if (change.Property == IsVisibleProperty)
        {
            _ = Dispatcher.UIThread.InvokeAsync(() => TryGetAdapter()?.SizeChanged(), DispatcherPriority.Background);
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
            && TryGetAdapter() is IWebViewAdapterWithFocus adapterWithFocus)
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
            && TryGetAdapter() is IWebViewAdapterWithFocus adapterWithFocus)
        {
            _ = adapterWithFocus.ResignFocus();
        }
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        if (control is IWebViewAdapter adapter)
        {
            if (_reparentingScope is not null)
            {
                _reparentingScope.SetDestroyingAdapter(adapter);
                return;
            }

            Debug.Assert(!(TryGetAdapter() is { } oldAdapter && oldAdapter != adapter));

            _webViewReadyCompletion.TrySetCanceled();
            _webViewReadyCompletion = new TaskCompletionSource<IWebViewAdapter>();
            adapter.NavigationStarted -= WebViewAdapterOnNavigationStarted;
            adapter.NavigationCompleted -= WebViewAdapterOnNavigationCompleted;
            adapter.WebMessageReceived -= WebViewAdapterOnWebMessageReceived;
            adapter.Initialized -= WebViewAdapterOnInitialized;
            if (adapter is IWebViewAdapterWithFocus withFocus)
            {
                withFocus.LostFocus -= WithFocusOnLostFocus;
                withFocus.GotFocus -= WithFocusOnGotFocus;
            }
            if (adapter is IWebViewAdapterWithInputRedirect withInput)
            {
                withInput.Input -= WithInputOnInput;
            }
            adapter.Dispose();
        }
    }

    /// <summary>
    /// Returns a platform handle of the native control.
    /// </summary>
    /// <remarks>
    /// Return handle can be used to access additional native APIs by using it with PInvokes. 
    /// </remarks>
    public IPlatformHandle? TryGetPlatformHandle()
    {
        return TryGetAdapter();
    }

    private ReparentingScope? _reparentingScope;

    /// <inheritdoc cref="BeginReparentingAsync" />
    public IDisposable BeginReparenting(bool yieldOnLayoutBeforeExiting = true)
    {
        if (_reparentingScope is not null)
            throw new InvalidOperationException("Nested BeginReparenting is not allowed.");

        return _reparentingScope = new ReparentingScope(this, yieldOnLayoutBeforeExiting);
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
        if (_reparentingScope is not null)
            throw new InvalidOperationException("Nested BeginReparenting is not allowed.");

        return _reparentingScope = new ReparentingScope(this, true);
    }

#if WPF
    private static readonly Func<Visual?, object?> s_getXpfHostDelegate = GetXpfHostDelegate();
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Atlantis.AtlantisPresentationCoreExtensions", "PresentationCore")]
    private static Func<Visual?, object?> GetXpfHostDelegate()
    {
        var members = Type.GetType("Atlantis.AtlantisPresentationCoreExtensions, PresentationCore", false)
            ?.FindMembers(
                MemberTypes.Method,
                BindingFlags.Public | BindingFlags.Static,
                static (m, _) => m.Name == "GetXpfHost" && ((MethodInfo)m).GetParameters().Length == 1,
                null);
        if (members?.Length == 1)
        {
            return (Func<Visual?, object?>)Delegate.CreateDelegate(typeof(Func<Visual?, object?>), null, (MethodInfo)members[0]);
        }
        return _ => null;
    }
#endif

    private sealed class ReparentingScope(NativeWebView webView, bool yieldOnLayoutBeforeExiting) : IDisposable, IAsyncDisposable
    {
        private IWebViewAdapter? _pendingAdapter;
        public void Dispose()
        {
            var task = DisposeAsync().AsTask();
            if (!task.IsCompleted)
            {
                var frame = new DispatcherFrame();
                _ = task.ContinueWith(static (_, s) => ((DispatcherFrame)s).Continue = false, frame);
#if WPF
                Dispatcher.PushFrame(frame);
#elif AVALONIA
                Dispatcher.UIThread.PushFrame(frame);
#endif
            }

            task.GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            if (_pendingAdapter is not null && yieldOnLayoutBeforeExiting)
            {
#if WPF
                await Dispatcher.Yield(DispatcherPriority.Render);
#elif AVALONIA
                var tcs = new TaskCompletionSource<bool>();
                Dispatcher.UIThread.Post(_ => tcs.TrySetResult(true), DispatcherPriority.Render);
                await tcs.Task.ConfigureAwait(false);
#endif
            }

            webView._reparentingScope = null;
            if (_pendingAdapter is not null
#if WPF
                && PresentationSource.FromVisual(webView) is null)
#elif AVALONIA
                && TopLevel.GetTopLevel(webView) is null)
#endif
            {
                webView.DestroyNativeControlCore(_pendingAdapter);
            }
        }

        public void SetDestroyingAdapter(IWebViewAdapter adapter)
        {
            if (_pendingAdapter is not null)
            {
                throw new InvalidOperationException("NativeWebView was detached second time without being attached.");
            }

            _pendingAdapter = adapter;
        }

        public IPlatformHandle ReparentRequested(IPlatformHandle parent)
        {
            if (_pendingAdapter is null)
            {
                throw new InvalidOperationException("NativeWebView wasn't detached from previous parent.");
            }

            var currentAdapter = _pendingAdapter;
            _pendingAdapter = null;

            currentAdapter.SetParent(parent);
            return currentAdapter;
        }
    }
}
#endif
