using System;
using System.Threading.Tasks;
using IWebViewAdapter = Avalonia.Controls.IWebViewAdapter;

#if AVALONIA
namespace Avalonia.Controls;
#elif WPF
namespace Avalonia.Xpf.Controls;
#endif

internal interface INativeWebViewControlImpl
{
    /// <inheritdoc cref="NativeWebView.AdapterCreated"/>.
    event EventHandler<IWebViewAdapter>? AdapterCreated;

    /// <inheritdoc cref="NativeWebView.AdapterDestroyed"/>.
    event EventHandler<IWebViewAdapter>? AdapterDestroyed;

    /// <summary>
    /// Returns adapter, if both control and adapter are initialized and ready to use.
    /// </summary>
    IWebViewAdapter? TryGetAdapter();

    /// <summary>
    /// If control is loaded and adapter is initializing, returns task with the loading adapter.
    /// If control is unloaded and not pending initialization, return Task(null).
    /// </summary>
    Task<IWebViewAdapter?> GetAdapterAsync();

    /// <summary>
    /// Moves keyboard focus to the hosted web content, if it's already initialized.
    /// </summary>
    void FocusWebView();

    /// <inheritdoc cref="NativeWebView.BeginReparenting"/>.
    IDisposable BeginReparenting(bool yieldOnLayoutBeforeExiting);

    /// <inheritdoc cref="NativeWebView.BeginReparentingAsync"/>.
    IAsyncDisposable BeginReparentingAsync();
}
