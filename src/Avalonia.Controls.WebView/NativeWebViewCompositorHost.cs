using System;
using System.Threading.Tasks;
using Avalonia.Controls.Gtk;
using Avalonia.Controls.Rendering;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

#if AVALONIA
namespace Avalonia.Controls;
#elif WPF
namespace Avalonia.Xpf.Controls;
#endif

internal class NativeWebViewCompositorHost : Control, INativeWebViewControlImpl
{
    private readonly Func<NativeWebViewCompositorHost, IWebViewAdapterWithOffscreenBuffer> _webViewFactory;
    private TaskCompletionSource<IWebViewAdapterWithOffscreenBuffer> _webViewReadyCompletion = new();
    //private ReparentingScope? _reparentingScope;
    private bool _firstDraw;
    private CompositionCustomVisual? _customVisual;
    private readonly BitmapFrameChain _frameChain;

    private NativeWebViewCompositorHost(Func<NativeWebViewCompositorHost, IWebViewAdapterWithOffscreenBuffer> webViewFactory)
    {
        _webViewFactory = webViewFactory;
        _frameChain = new BitmapFrameChain();
    }

    public static NativeWebViewCompositorHost? TryCreate()
    {
        if (WebViewHelper.GtkOffscreenAvailable() && OperatingSystemEx.IsLinux())
            return new NativeWebViewCompositorHost(c => new GtkOffscreenAvaloniaWebViewAdapter(c));
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

        var adapter = _webViewFactory(this);

        var compositorVisual = ElementComposition.GetElementVisual(this)!;
        _firstDraw = true;
        _customVisual = compositorVisual.Compositor.CreateCustomVisual(new VisualHandler());
        _customVisual.Size = new Vector(Bounds.Width, Bounds.Height);
        _customVisual.SendHandlerMessage(_frameChain.Consumer);
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

    private async void OffscreenAdapter_OnDrawRequested()
    {
        var adapter = (IWebViewAdapterWithOffscreenBuffer)TryGetAdapter()!;

        if (_firstDraw)
        {
            _firstDraw = false;
            adapter.SizeChanged(PixelSize.FromSize(Bounds.Size, TopLevel.GetTopLevel(this)!.RenderScaling));
        }

        await adapter.UpdateWriteableBitmap(_frameChain.Producer);
        _customVisual?.SendHandlerMessage(VisualHandler.DrawRequested);
    }

    private class VisualHandler : CompositionCustomVisualHandler
    {
        public static readonly object DrawRequested = new();
        public static readonly object Stop = new();

        private FrameChainBase<WriteableBitmap, PixelSize>.IConsumer? _frameConsumer;

        public override void OnMessage(object message)
        {
            if (message is FrameChainBase<WriteableBitmap, PixelSize>.IConsumer consumer)
            {
                _frameConsumer = consumer;
                RegisterForNextAnimationFrameUpdate();
            }
            else if (message == DrawRequested)
            {
                RegisterForNextAnimationFrameUpdate();
            }
            else if (message == Stop)
            {
                _frameConsumer = null;
                RegisterForNextAnimationFrameUpdate();
            }

            base.OnMessage(message);
        }

        public override void OnAnimationFrameUpdate()
        {
            var hasNextFrame = _frameConsumer?.NextFrame() == true;
            if (hasNextFrame)
                Invalidate();
        }

        public override void OnRender(ImmediateDrawingContext drawingContext)
        {
            if (_frameConsumer is not null
                && _frameConsumer.CurrentFrame is { } frame)
            {
                drawingContext.DrawBitmap(frame, GetRenderBounds());
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
