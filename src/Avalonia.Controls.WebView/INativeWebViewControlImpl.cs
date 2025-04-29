using System;
using System.Threading.Tasks;

namespace Avalonia.Controls;

internal interface INativeWebViewControlImpl
{
    event EventHandler<IWebViewAdapter>? AdapterInitialized;
    event EventHandler<IWebViewAdapter>? AdapterDeinitialized;
    IWebViewAdapter? TryGetAdapter();
    Task<IWebViewAdapter> GetAdapterAsync();
    IDisposable BeginReparenting(bool yieldOnLayoutBeforeExiting);
    IAsyncDisposable BeginReparentingAsync();
}
