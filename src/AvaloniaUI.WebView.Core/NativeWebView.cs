#if AVALONIA || WPF
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AvaloniaUI.WebView.Core;
using AvaloniaUI.WebView.Core.Native;
using AvaloniaUI.WebView.Core.Win;
using IPlatformHandle = Avalonia.Platform.IPlatformHandle;
#if WPF
using System.Windows;
using NativeControlHost = AvaloniaUI.Xpf.WpfAbstractions.NativeControlHost;
#elif AVALONIA
using Avalonia;
using Avalonia.Controls;
#endif

namespace AvaloniaUI.WebView;

public class NativeWebView : NativeControlHost, IWebView
{
    private bool _ignoreNavigation = false;
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

    public Uri Source
    {
        get => (Uri)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

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
        IWebViewAdapter? adapter = null;

        if (OperatingSystemEx.IsMacOS())
        {
            adapter = new NativeWebViewAdapter();
        }
        // if (OperatingSystemEx.IsLinux())
        // {
        //     new Gtk.GtkWebView2Adapter();
        //
        //     return base.CreateNativeControlCore(parent);
        //     // adapter = new Gtk.GtkWebView2Adapter();
        // }
        // else if (OperatingSystemEx.IsBrowser())
        // {
        //     adapter = new BrowserIFrameAdapter();
        // }
#if !NETSTANDARD2_0
        else if (OperatingSystemEx.IsWindows())
        {
            if (WebViewCapabilities.IsMsWebView2Available)
            {
                adapter = new WebView2Adapter(base.CreateNativeControlCore(parent));
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

    private void WebViewAdapterOnInitialized(object? sender, EventArgs e)
    {
        var adapter = (IWebViewAdapter)sender!;
        adapter.Initialized -= WebViewAdapterOnInitialized;
        adapter.NavigationStarted += WebViewAdapterOnNavigationStarted;
        adapter.NavigationCompleted += WebViewAdapterOnNavigationCompleted;
        adapter.WebMessageReceived += WebViewAdapterOnWebMessageReceived;

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
    }
#endif

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        if (control is IWebViewAdapter adapter)
        {
            Debug.Assert(!(TryGetAdapter() is { } oldAdapter && oldAdapter != adapter));

            _webViewReadyCompletion.TrySetCanceled();
            _webViewReadyCompletion = new TaskCompletionSource<IWebViewAdapter>();
            adapter.NavigationStarted -= WebViewAdapterOnNavigationStarted;
            adapter.NavigationCompleted -= WebViewAdapterOnNavigationCompleted;
            adapter.WebMessageReceived -= WebViewAdapterOnWebMessageReceived;
            adapter.Initialized -= WebViewAdapterOnInitialized;
            adapter.Dispose();
        }
    }
}
#endif
