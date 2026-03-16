using System;
using System.Threading.Tasks;
using Avalonia.Controls.Rendering;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;

#if AVALONIA
namespace Avalonia.Controls;
#elif WPF
namespace Avalonia.Xpf.Controls;
#endif

internal class NativeWebViewCompositorHost(WebViewAdapter.CompositorHostAdapterFactory factory) : Control, INativeWebViewControlImpl
{
    private WebViewTextInputMethodClient? _imClient;
    private TaskCompletionSource<IWebViewAdapterWithOffscreenBuffer?>? _webViewReadyCompletion;
    //private ReparentingScope? _reparentingScope;
    private bool _firstDraw;
    private CompositionCustomVisual? _customVisual;
    private readonly BitmapFrameChain _frameChain = new(PixelFormats.Bgra8888);

    static NativeWebViewCompositorHost()
    {
        FocusableProperty.OverrideDefaultValue<NativeWebViewCompositorHost>(true);
        TextInputMethodClientRequestedEvent.AddClassHandler<NativeWebViewCompositorHost>((host, e) =>
        {
            e.Client = host._imClient ??= new WebViewTextInputMethodClient(host);
        });
    }

    /// <inheritdoc />
    public event EventHandler<IWebViewAdapter>? AdapterCreated;

    /// <inheritdoc />
    public event EventHandler<IWebViewAdapter>? AdapterDestroyed;

    /// <inheritdoc />
    public IWebViewAdapter? TryGetAdapter() => _webViewReadyCompletion?.Task.Status == TaskStatus.RanToCompletion ?
        _webViewReadyCompletion.Task.Result :
        null;

    /// <inheritdoc />
    public async Task<IWebViewAdapter?> GetAdapterAsync() =>
        _webViewReadyCompletion is null ? null : await _webViewReadyCompletion.Task;

    /// <inheritdoc />
    public IDisposable BeginReparenting(bool yieldOnLayoutBeforeExiting) => EmptyDisposable.Instance;

    /// <inheritdoc />
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

        _webViewReadyCompletion = new TaskCompletionSource<IWebViewAdapterWithOffscreenBuffer?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var adapterTask = factory.InvokeAsync(this);
        CompleteAdapter();

        var compositorVisual = ElementComposition.GetElementVisual(this)!;
        _firstDraw = true;
        _customVisual = compositorVisual.Compositor.CreateCustomVisual(new VisualHandler());
        _customVisual.Size = new Vector(Bounds.Width, Bounds.Height);
        _customVisual.SendHandlerMessage(_frameChain.Consumer);
        ElementComposition.SetElementChildVisual(this, _customVisual);

        // ReSharper disable once AsyncVoidMethod - let it flow to the dispatcher
        async void CompleteAdapter()
        {
            var adapter = await adapterTask;
            WebViewAdapterOnInitialized(adapter);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        var adapter = (IWebViewAdapterWithOffscreenBuffer?)TryGetAdapter();

        _webViewReadyCompletion?.TrySetCanceled();
        _webViewReadyCompletion = null;

        if (adapter is not null)
        {
            adapter.DrawRequested -= OffscreenAdapter_OnDrawRequested;
            if (adapter is IWebViewAdapterWithExplicitCursor cursorAdapter)
            {
                cursorAdapter.CursorChanged -= CursorAdapter_OnCursorChanged;
            }
            AdapterDestroyed?.Invoke(this, adapter);
            adapter.Dispose();
        }

        if (_customVisual is not null)
        {
            _customVisual.SendHandlerMessage(VisualHandler.Stop);
            ElementComposition.SetElementChildVisual(this, null);
            _customVisual = null;
        }
    }

    private void WebViewAdapterOnInitialized(IWebViewAdapterWithOffscreenBuffer adapter)
    {
        _webViewReadyCompletion?.TrySetResult(adapter);
        AdapterCreated?.Invoke(this, adapter);
        adapter.DrawRequested += OffscreenAdapter_OnDrawRequested;
        
        if (adapter is IWebViewAdapterWithExplicitCursor cursorAdapter)
        {
            Cursor = new Cursor(cursorAdapter.CurrentCursorType);
            cursorAdapter.CursorChanged += CursorAdapter_OnCursorChanged;
        }
    }

    private async void OffscreenAdapter_OnDrawRequested()
    {
        var adapter = (IWebViewAdapterWithOffscreenBuffer?)TryGetAdapter();
        if (adapter is null)
            return;

        var adapterSize = PixelSize.FromSize(Bounds.Size, TopLevel.GetTopLevel(this)!.RenderScaling);
        if (_firstDraw)
        {
            _firstDraw = false;
            adapter.SizeChanged(adapterSize);
        }

        await adapter.UpdateWriteableBitmap(adapterSize, _frameChain.Producer);
        _customVisual?.SendHandlerMessage(VisualHandler.DrawRequested);
    }

    private void CursorAdapter_OnCursorChanged(object? sender, EventArgs e)
    {
        Cursor = new Cursor(((IWebViewAdapterWithExplicitCursor)sender!).CurrentCursorType);
    }

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Brushes.Transparent, Bounds.WithX(0).WithY(0));
        base.Render(context);
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
            if (_frameConsumer?.CurrentFrame is { } frame)
            {
                drawingContext.DrawBitmap(frame, GetRenderBounds());
            }
        }
    }

    private sealed class WebViewTextInputMethodClient(NativeWebViewCompositorHost owner) : TextInputMethodClient
    {
        public override Visual TextViewVisual => owner;
        public override bool SupportsPreedit => false;
        public override bool SupportsSurroundingText => false;
        public override string SurroundingText => string.Empty;
        public override Rect CursorRectangle => new(0, 0, 1, 20);
        public override TextSelection Selection { get; set; }
    }
}
