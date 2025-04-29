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

internal class NativeWebViewCompositorHost : Control, INativeWebViewControlImpl
{
    private readonly TaskCompletionSource<IWebViewAdapterWithOffscreenBuffer> _webViewReadyCompletion = new();
    private readonly VisualHandler _customVisualHandler;
    private CompositionCustomVisual? _customVisual;

    private NativeWebViewCompositorHost(IWebViewAdapterWithOffscreenBuffer webViewAdapter)
    {
        _customVisualHandler = new VisualHandler(webViewAdapter);
        if (!webViewAdapter.IsInitialized)
        {
            webViewAdapter.Initialized += (sender, args) =>
            {
                _webViewReadyCompletion.TrySetResult(webViewAdapter);
                AdapterInitialized?.Invoke(this, webViewAdapter);
            };
        }
        else
        {
            _webViewReadyCompletion.TrySetResult(webViewAdapter);
        }
    }

    public static NativeWebViewCompositorHost? TryCreate()
    {
        if (OperatingSystemEx.IsLinux())
            return new NativeWebViewCompositorHost(new GtkOffscreenWebViewAdapter());
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

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var compositorVisual = ElementComposition.GetElementVisual(this)!;
        _customVisual = compositorVisual.Compositor.CreateCustomVisual(_customVisualHandler);
        _customVisual.Size = new Vector(Bounds.Width, Bounds.Height);
        _customVisual.SendHandlerMessage(VisualHandler.Start);
        ElementComposition.SetElementChildVisual(this, _customVisual);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_customVisual is not null)
        {
            _customVisual.SendHandlerMessage(VisualHandler.Stop);
            ElementComposition.SetElementChildVisual(this, null);
            _customVisual = null;
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        if (_customVisual is not null)
        {
            _customVisual.Size = new Vector(size.Width, size.Height);
        }
        return size;
    }

    private class VisualHandler(IWebViewAdapterWithOffscreenBuffer offscreenBuffer) : CompositionCustomVisualHandler
    {
        public static readonly object Start = new();
        public static readonly object Stop = new();

        private WriteableBitmap? _bitmap;
        private bool _isRunning = false;

        public override void OnMessage(object message)
        {
            if (message == Start)
            {
                _isRunning = true;
                RegisterForNextAnimationFrameUpdate();
            }
            else if (message == Stop)
            {
                _isRunning = false;
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

            if (_isRunning)
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
