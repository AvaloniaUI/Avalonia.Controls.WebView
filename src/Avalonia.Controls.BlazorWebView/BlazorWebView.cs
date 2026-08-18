using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using AvControls = Avalonia.Controls;

namespace Avalonia.Controls.BlazorWebView;

/// <summary>
/// Hosts Blazor components inside an Avalonia <see cref="AvControls.NativeWebView"/>.
/// </summary>
public class BlazorWebView : Decorator, IAsyncDisposable
{
    private readonly AvControls.NativeWebView _webView = new();
    private AvaloniaBlazorWebViewManager? _manager;
    private bool _isInitialized;

    /// <summary>
    /// Path to the host page within the application's static files (for example <c>wwwroot/index.html</c>).
    /// </summary>
    public static readonly StyledProperty<string?> HostPageProperty =
        AvaloniaProperty.Register<BlazorWebView, string?>(nameof(HostPage), "wwwroot/index.html");

    /// <summary>
    /// Application services used to create Blazor scopes. Must include Blazor WebView services
    /// (for example via <c>services.AddAvaloniaBlazorWebView()</c>).
    /// </summary>
    public static readonly StyledProperty<IServiceProvider?> ServicesProperty =
        AvaloniaProperty.Register<BlazorWebView, IServiceProvider?>(nameof(Services));

    /// <summary>
    /// Optional content root directory. Defaults to the application base directory.
    /// </summary>
    public static readonly StyledProperty<string?> ContentRootProperty =
        AvaloniaProperty.Register<BlazorWebView, string?>(nameof(ContentRoot));

    public BlazorWebView()
    {
        RootComponents = new ObservableCollection<RootComponent>();
        RootComponents.CollectionChanged += OnRootComponentsChanged;
        // Register custom scheme before the native adapter is created.
        _webView.EnvironmentRequested += (_, e) =>
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                && !e.CustomSchemes.Contains(AvaloniaBlazorWebViewManager.BlazorAppScheme, StringComparer.OrdinalIgnoreCase))
            {
                e.CustomSchemes.Add(AvaloniaBlazorWebViewManager.BlazorAppScheme);
            }
        };
        _webView.Background = Brushes.White;
        Child = _webView;
    }

    /// <summary>
    /// Path to the host page within the application's static files.
    /// </summary>
    public string? HostPage
    {
        get => GetValue(HostPageProperty);
        set => SetValue(HostPageProperty, value);
    }

    /// <summary>
    /// Application <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider? Services
    {
        get => GetValue(ServicesProperty);
        set => SetValue(ServicesProperty, value);
    }

    /// <summary>
    /// Content root for static files. Defaults to <see cref="AppContext.BaseDirectory"/>.
    /// </summary>
    public string? ContentRoot
    {
        get => GetValue(ContentRootProperty);
        set => SetValue(ContentRootProperty, value);
    }

    /// <summary>
    /// Root components to render into the host page.
    /// </summary>
    public ObservableCollection<RootComponent> RootComponents { get; }

    /// <summary>
    /// The underlying <see cref="AvControls.NativeWebView"/>.
    /// </summary>
    public AvControls.NativeWebView WebView => _webView;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _ = StartAsync();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _ = DisposeAsync();
    }

    private async Task StartAsync()
    {
        if (_isInitialized)
            return;

        try
        {
            await StartCoreAsync().ConfigureAwait(true);
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to start BlazorWebView.", ex);
        }
    }

    private async Task StartCoreAsync()
    {
        if (Services is null)
            throw new InvalidOperationException($"{nameof(Services)} must be set before {nameof(BlazorWebView)} is shown.");

        if (string.IsNullOrWhiteSpace(HostPage))
            throw new InvalidOperationException($"{nameof(HostPage)} must be set.");

        var contentRoot = ContentRoot ?? AppContext.BaseDirectory;
        var hostPageFullPath = Path.GetFullPath(Path.IsPathRooted(HostPage)
            ? HostPage
            : Path.Combine(contentRoot, HostPage));

        // Match WPF/MAUI: the file provider root is the directory that contains the host page (typically wwwroot).
        // The physical path need not exist when CreateFileProvider serves embedded (or other) content.
        var contentRootDir = Path.GetDirectoryName(hostPageFullPath)
            ?? throw new InvalidOperationException("Unable to resolve the Blazor content root.");
        var hostPageRelative = Path.GetRelativePath(contentRootDir, hostPageFullPath).Replace('\\', '/');
        var fileProvider = CreateFileProvider(contentRootDir);

        var jsComponents = new JSComponentConfigurationStore();
        _manager = new AvaloniaBlazorWebViewManager(
            _webView,
            Services,
            AvaloniaDispatcher.Instance,
            AvaloniaBlazorWebViewManager.AppBaseUri,
            fileProvider,
            jsComponents,
            hostPageRelative);

        foreach (var root in RootComponents)
        {
            if (root.ComponentType is null)
                continue;
            await _manager.AddRootComponentAsync(root.ComponentType, root.Selector, root.ToParameterView());
        }

        _manager.Navigate("/");
    }

    /// <summary>
    /// Creates the <see cref="IFileProvider"/> used for host-page and static assets under the content root
    /// (typically <c>wwwroot</c>). Override to serve embedded resources instead of files on disk.
    /// Combine with <see cref="CompositeFileProvider"/> if you also want the default physical provider.
    /// Framework scripts such as <c>_framework/blazor.webview.js</c> are still served from
    /// <c>Microsoft.AspNetCore.Components.WebView</c> embedded content.
    /// </summary>
    /// <param name="contentRootDir">Absolute path of the content root directory (may not exist on disk).</param>
    protected virtual IFileProvider CreateFileProvider(string contentRootDir)
    {
        if (Directory.Exists(contentRootDir))
            return new PhysicalFileProvider(contentRootDir);

        // Development / embedded scenarios: host page and app assets come from a custom provider
        // or Static Web Assets; framework files come from the WebView package.
        return new NullFileProvider();
    }

    private async void OnRootComponentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_manager is null || !_isInitialized)
            return;

        if (e.NewItems is not null)
        {
            foreach (RootComponent root in e.NewItems)
            {
                if (root.ComponentType is null)
                    continue;
                await _manager.AddRootComponentAsync(root.ComponentType, root.Selector, root.ToParameterView());
            }
        }

        if (e.OldItems is not null)
        {
            foreach (RootComponent root in e.OldItems)
                await _manager.RemoveRootComponentAsync(root.Selector);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_manager is not null)
        {
            await _manager.DisposeManagedAsync();
            _manager = null;
        }
        _isInitialized = false;
    }
}

/// <summary>
/// Service collection helpers for Avalonia Blazor Hybrid.
/// </summary>
public static class AvaloniaBlazorWebViewServiceCollectionExtensions
{
    /// <summary>
    /// Adds Blazor WebView services required by <see cref="BlazorWebView"/>.
    /// </summary>
    public static IServiceCollection AddAvaloniaBlazorWebView(this IServiceCollection services)
    {
        services.AddBlazorWebView();
        return services;
    }
}
