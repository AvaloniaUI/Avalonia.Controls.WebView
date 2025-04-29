using System;
using System.Threading.Tasks;
using Avalonia.Controls.Gtk;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.Composition;

#if AVALONIA
namespace Avalonia.Controls;
#elif WPF
namespace Avalonia.Xpf.Controls;
#endif

internal class NativeWebViewCompositorHost(Func<IWebViewAdapterWithOffscreenBuffer> webViewFactory) : Control, INativeWebViewControlImpl
{
    private TaskCompletionSource<IWebViewAdapterWithOffscreenBuffer> _webViewReadyCompletion = new();
    //private ReparentingScope? _reparentingScope;

    private CompositionCustomVisual? _customVisual;

    public static NativeWebViewCompositorHost? TryCreate()
    {
        if (OperatingSystemEx.IsLinux())
            return new NativeWebViewCompositorHost(() => new GtkOffscreenWebViewAdapter());
        return null;
    }

    public event EventHandler<IWebViewAdapter>? AdapterInitialized;
    public event EventHandler<IWebViewAdapter>? AdapterDeinitialized;

    public IWebViewAdapter? TryGetAdapter() => _webViewReadyCompletion.Task.Status == TaskStatus.RanToCompletion ?
        _webViewReadyCompletion.Task.Result :
        null;
    public async Task<IWebViewAdapter> GetAdapterAsync() => await _webViewReadyCompletion.Task;

    public IDisposable BeginReparenting(bool yieldOnLayoutBeforeExiting) => EmptyDisposable.Instance;
    public IAsyncDisposable BeginReparentingAsync() => EmptyDisposable.Instance;

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        if (_customVisual is not null)
        {
            _customVisual.Size = new Vector(size.Width, size.Height);
        }
        return size;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var adapter = webViewFactory();

        var compositorVisual = ElementComposition.GetElementVisual(this)!;
        _customVisual = compositorVisual.Compositor.CreateCustomVisual(new VisualHandler(adapter));
        _customVisual.Size = new Vector(Bounds.Width, Bounds.Height);
        _customVisual.SendHandlerMessage(VisualHandler.DrawRequested);
        ElementComposition.SetElementChildVisual(this, _customVisual);

        if (adapter.IsInitialized)
        {
            WebViewAdapterOnInitialized(adapter, EventArgs.Empty);
        }
        else
        {
            adapter.Initialized += WebViewAdapterOnInitialized;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        var adapter = (IWebViewAdapterWithOffscreenBuffer?)TryGetAdapter();

        _webViewReadyCompletion.TrySetCanceled();
        _webViewReadyCompletion = new TaskCompletionSource<IWebViewAdapterWithOffscreenBuffer>();

        if (adapter is not null)
        {
            adapter.DrawRequested -= OffscreenAdapter_OnDrawRequested;
            adapter.Initialized -= WebViewAdapterOnInitialized;
            AdapterDeinitialized?.Invoke(this, adapter);
            adapter.Dispose();
        }

        if (_customVisual is not null)
        {
            _customVisual.SendHandlerMessage(VisualHandler.Stop);
            ElementComposition.SetElementChildVisual(this, null);
            _customVisual = null;
        }
    }

    private void WebViewAdapterOnInitialized(object? sender, EventArgs e)
    {
        var adapter = (IWebViewAdapterWithOffscreenBuffer)sender!;
        _webViewReadyCompletion.TrySetResult(adapter);
        AdapterInitialized?.Invoke(this, adapter);
        adapter.DrawRequested += OffscreenAdapter_OnDrawRequested;
    }

    private void OffscreenAdapter_OnDrawRequested()
    {
        _customVisual?.SendHandlerMessage(VisualHandler.DrawRequested);
    }

    private class VisualHandler(IWebViewAdapterWithOffscreenBuffer offscreenBuffer) : CompositionCustomVisualHandler
    {
        public static readonly object DrawRequested = new();
        public static readonly object Stop = new();

        private WriteableBitmap? _bitmap;
        private TimeSpan _drawUntil;

        public override void OnMessage(object message)
        {
            if (message == DrawRequested)
            {
                // Keep rendering for another 100ms after it was requested, making webview smoother.
                // Even something plain as scroll will require this smoothness,
                // which might not be delivered on dispatcher-delivered DrawRequested messages.
                _drawUntil = CompositionNow + TimeSpan.FromMilliseconds(100);
                RegisterForNextAnimationFrameUpdate();
            }
            else if (message == Stop)
            {
                _drawUntil = TimeSpan.Zero;
            }

            base.OnMessage(message);
        }

        public override void OnRender(ImmediateDrawingContext drawingContext)
        {
            offscreenBuffer.UpdateWriteableBitmap(ref _bitmap);
            if (_bitmap is not null)
            {
                drawingContext.DrawBitmap(_bitmap, GetRenderBounds());
            }
        }

        public override void OnAnimationFrameUpdate()
        {
            Invalidate();
            if (_drawUntil > CompositionNow)
            {
                RegisterForNextAnimationFrameUpdate();
            }
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
