using System;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Rendering.Composition;

#if AVALONIA
namespace Avalonia.Controls;
#elif WPF
namespace Avalonia.Xpf.Controls;
#endif

internal class NativeWebViewCompositorHost : Control, INativeWebViewControlImpl
{
    public static NativeWebViewCompositorHost? TryCreate()
    {
        if (OperatingSystemEx.IsLinux())
            return new NativeWebViewCompositorHost();
        return null;
    }

    public event EventHandler<IWebViewAdapter>? AdapterInitialized;
    public event EventHandler<IWebViewAdapter>? AdapterDeinitialized;
    public IWebViewAdapter? TryGetAdapter()
    {
        throw new NotImplementedException();
    }

    public Task<IWebViewAdapter> GetAdapterAsync()
    {
        throw new NotImplementedException();
    }

    public IDisposable BeginReparenting(bool yieldOnLayoutBeforeExiting) => EmptyDisposable.Instance;
    public IAsyncDisposable BeginReparentingAsync() => EmptyDisposable.Instance;

    internal class NativeWebViewCompositionCustomVisualHandler : CompositionCustomVisualHandler
    {
        public override void OnRender(ImmediateDrawingContext drawingContext)
        {
            //new WriteableBitmap().Lock().Address
            throw new System.NotImplementedException();
        }
    }

    private class EmptyDisposable : IDisposable, IAsyncDisposable
    {
        public static EmptyDisposable Instance { get; } = new();

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }
    }
}
