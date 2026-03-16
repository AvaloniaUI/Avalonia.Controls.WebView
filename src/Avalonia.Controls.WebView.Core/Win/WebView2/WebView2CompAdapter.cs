using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;
using Avalonia.Controls.Rendering;
using Avalonia.Controls.Win.Interop;
using Avalonia.Controls.Win.WebView2.Interop;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.Win32.SafeHandles;

namespace Avalonia.Controls.Win.WebView2;

// With compositor backend, we create a dedicated Compositor instance on the UI thread,
// and attach WebView2 to that compositor's tree.
// Each new frame, we request a commit async on the compositor, and when it's done,
// we use UNDOCUMENTED API - CompositionCapture - to render the WebView2 content into a writeable bitmap.
// TODO: we can use D3D interop to avoid CPU copies here.
// TODO: for some reason when WebView is recreated, dispatcher queue (?) gets stuck with no resizing, but rendering continues.
[SupportedOSPlatform("windows10.0.17763.0")]
internal partial class WebView2CompAdapter
    // ICoreWebView2Controller can be queried from ICoreWebView2CompositionController. 
    // ReSharper disable once SuspiciousTypeConversion.Global
    : WebView2BaseAdapter, IWebViewAdapterWithOffscreenBuffer,
        IWebViewAdapterWithOffscreenInput, IWebViewAdapterWithExplicitCursor
{
    private static readonly Lazy<ICompositor> s_compositor = new(() =>
    {
        _ = DispatcherQueueStatics.GetOrCreateOnCurrentThread();
        var compositor = NativeWinRTMethods.CreateInstance<ICompositor>("Windows.UI.Composition.Compositor")
                         ?? throw new InvalidOperationException("Failed to create Compositor instance.");
        return compositor;
    });

    private readonly CommitAsyncLoopHandler _commitAsyncLoopHandler;
    private readonly ICoreWebView2CompositionController _controller;
    private EventRegistrationToken _cursorChangedToken;
    private EventHandler? _cursorChangedHandler;
    private Action? _drawRequestedHandler;

    public WebView2CompAdapter(
        IContainerVisual visual, ICompositor compositor, ICoreWebView2CompositionController controller)
        // ReSharper disable once SuspiciousTypeConversion.Global
        : base((ICoreWebView2Controller)controller)
    {
        _controller = controller;
        controller.SetRootVisualTarget(visual);

        unsafe
        {
            Handle = (IntPtr)ComInterfaceMarshaller<IContainerVisual>.ConvertToUnmanaged(visual);
        }

        // ReSharper disable once SuspiciousTypeConversion.Global
        _commitAsyncLoopHandler = new CommitAsyncLoopHandler(this, (ICompositor5)compositor);
    }

    public event Action? DrawRequested
    {
        add
        {
            if (_drawRequestedHandler is null)
            {
                _commitAsyncLoopHandler.RegisterNext();
            }
            _drawRequestedHandler += value;
        }
        remove => _drawRequestedHandler -= value;
    }

    public event EventHandler? CursorChanged
    {
        add => _cursorChangedHandler += value;
        remove => _cursorChangedHandler -= value;
    }

    public StandardCursorType CurrentCursorType => WindowsUtility.MapCursor(_controller.GetSystemCursorId());

    public override IntPtr Handle { get; }
    public override string HandleDescriptor => "Windows.UI.Composition.ContainerVisual";

    public static async Task<WebViewAdapter.OffscreenWebViewAdapterBuilder> CreateBuilder(
        WindowsWebView2EnvironmentRequestedEventArgs environmentArgs)
    {
        var env = await CoreWebView2Environment.CreateAsync(environmentArgs);

        return async (parent) =>
        {
            var parentHandle = TopLevel.GetTopLevel(parent)?.TryGetPlatformHandle()?.Handle
                               ?? throw new InvalidOperationException("Parent must be a TopLevel control.");
            var handler = new WebView2CompositionControllerHandler();

            var hasCustomOptions = environmentArgs.IsInPrivateModeEnabled
                                   || !string.IsNullOrEmpty(environmentArgs.ProfileName);
            if (hasCustomOptions && env is ICoreWebView2Environment10 env10)
            {
                var options = env10.CreateCoreWebView2ControllerOptions();
                options.SetIsInPrivateModeEnabled(environmentArgs.IsInPrivateModeEnabled);
                if (environmentArgs.ProfileName is not null)
                    options.SetProfileName(environmentArgs.ProfileName);

                env10.CreateCoreWebView2CompositionControllerWithOptions(parentHandle, options, handler);
            }
            else if (env is ICoreWebView2Environment3 env3)
            {
                env3.CreateCoreWebView2CompositionController(parentHandle, handler);
            }
            else
            {
                throw new NotSupportedException();
            }

            var controller = await handler.Result.Task;

            var compositionVisual = s_compositor.Value.CreateContainerVisual();

            var webView = new WebView2CompAdapter(compositionVisual, s_compositor.Value, controller);
            await webView.InitializeAsync(environmentArgs);

            return webView;
        };
    }

    public async Task UpdateWriteableBitmap(PixelSize currentSize,
        FrameChainBase<WriteableBitmap, PixelSize>.IProducer producer)
    {
        var target = _controller.GetRootVisualTarget();

        if (target is null || currentSize.Height == 0 || currentSize.Width == 0)
            return;

        // ReSharper disable once SuspiciousTypeConversion.Global
        var compositor = ((ICompositionObject)target).Compositor();
        // ReSharper disable once SuspiciousTypeConversion.Global
        var compositorCapture = (ICompositionCaptureTest)compositor;

        // https://github.com/ocalvo/WorkTests/blob/a2f18151be579addc60d4464b1e2a9f54f8e3314/MediaTestManaged/DCompHelpers.cs#L162

        //   Render Visual:
        //   * This function is basically async and returns immediately after putting
        //     a MILCMD onto the batch for the application channel, and marks the device dirty.
        //   * It returns two handles by reference.
        //   * The first handle (hMap) is to a map of bits.
        //   * The second handle (hEvent) is to an event.
        //   * The event is signaled when a commit has happened 
        //      and the after actual renderpass has been rendered and presented.
        //   * Once signaled, the bits are ready for us to grab.
        //   * Any changes to the tree before the implicit commit sends the batch to the 
        //      Compositor will be reflected in the capture, even if made after the initial
        //      RenderVisual function call.
        //   * Any changes to the tree after the implicit commit may safely modify the tree,
        //      as they will be processed in a separate batch
        var hMap = IntPtr.Zero;
        var hEvent = IntPtr.Zero;
        
        var hr = compositorCapture.RenderVisual(
            target,
            0, // offset X
            0, // offset y
            (uint)currentSize.Width,
            (uint)currentSize.Height,
            CompositionCaptureTestBitmapPixelFormat.Bgra8,
            ref hMap,
            ref hEvent,
            out var cbMap);
        if (hr != 0)
        {
            throw new COMException("Render Visual Failed", new Win32Exception(hr));
        }

        try
        {
            using var wh = new ManualResetEvent(false);
            wh.SafeWaitHandle = new SafeWaitHandle(hEvent, ownsHandle: false);

            var tcs = new TaskCompletionSource<bool>();
            ThreadPool.RegisterWaitForSingleObject(wh, static (state, timedOut) =>
            {
                var tcs = (TaskCompletionSource<bool>)state!;
                tcs.SetResult(!timedOut);
            }, tcs, TimeSpan.FromMilliseconds(100), true);

            if (!await tcs.Task)
            {
                // Timeout, ignore
                return;
            }

            using (producer.GetNextFrame(currentSize, out var frame))
            {
                var pbMap = PInvoke.MapViewOfFile(
                    new HANDLE(hMap), FILE_MAP.FILE_MAP_WRITE, 0, 0, UIntPtr.Zero);

                if (pbMap != IntPtr.Zero)
                {
                    try
                    {
                        using var buf = frame.Lock();
                        unsafe
                        {
                            Buffer.MemoryCopy(
                                source: pbMap,
                                destination: (void*)buf.Address,
                                destinationSizeInBytes: buf.RowBytes * currentSize.Height,
                                sourceBytesToCopy: cbMap
                            );
                        }
                    }
                    finally
                    {
                        PInvoke.UnmapViewOfFile(pbMap);
                    }
                }
            }
        }
        finally
        {
            PInvoke.CloseHandle(new HANDLE(hMap));
            PInvoke.CloseHandle(new HANDLE(hEvent));
        }
        
        _commitAsyncLoopHandler.RegisterNext();
    }

    internal EventHandler? GetCursorChanged() => _cursorChangedHandler;

    protected override void RegisterCallbacks(WebViewCallbacks callbacks)
    {
        _controller.add_CursorChanged(callbacks, out _cursorChangedToken);
        base.RegisterCallbacks(callbacks);
    }

    protected override void UnregisterCallbacks()
    {
        _controller.remove_CursorChanged(_cursorChangedToken);
        base.UnregisterCallbacks();
    }

    protected override void SizeChangedCore(PixelSize containerSize)
    {
        var target = _controller.GetRootVisualTarget();
        target?.SetSize(new winrtVector2 { X = containerSize.Width, Y = containerSize.Height });
        base.SizeChangedCore(containerSize);
    }

    [GeneratedComClass]
    private partial class WebView2CompositionControllerHandler :
        GenericCompletedHandler<ICoreWebView2CompositionController>,
        ICoreWebView2CreateCoreWebView2CompositionControllerCompletedHandler;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _controller.SetRootVisualTarget(null);
        }

        base.Dispose(disposing);
    }

    public bool KeyInput(bool press, PhysicalKey physical, string? symbol, KeyModifiers modifiers)
    {
        // Will be implicitly handled by Windows itself.
        return true;
    }

    public bool TextInput(string text)
    {
        // Will be implicitly handled by Windows itself.
        return false;
    }

    public bool PointerInput(PointerPoint point, int clickCount, double dpi, KeyModifiers modifiers)
    {
        var virtualKeys = KeyModifiersToVirtualKey(modifiers, point);
        var position = ToPoint(point.Position, dpi);
        var changeType = point.Properties.PointerUpdateKind switch
        {
            PointerUpdateKind.LeftButtonPressed when clickCount == 2 => COREWEBVIEW2_MOUSE_EVENT_KIND
                .COREWEBVIEW2_MOUSE_EVENT_KIND_LEFT_BUTTON_DOUBLE_CLICK,
            PointerUpdateKind.LeftButtonPressed => COREWEBVIEW2_MOUSE_EVENT_KIND
                .COREWEBVIEW2_MOUSE_EVENT_KIND_LEFT_BUTTON_DOWN,
            PointerUpdateKind.MiddleButtonPressed when clickCount == 2 => COREWEBVIEW2_MOUSE_EVENT_KIND
                .COREWEBVIEW2_MOUSE_EVENT_KIND_MIDDLE_BUTTON_DOUBLE_CLICK,
            PointerUpdateKind.MiddleButtonPressed => COREWEBVIEW2_MOUSE_EVENT_KIND
                .COREWEBVIEW2_MOUSE_EVENT_KIND_MIDDLE_BUTTON_DOWN,
            PointerUpdateKind.RightButtonPressed when clickCount == 2 => COREWEBVIEW2_MOUSE_EVENT_KIND
                .COREWEBVIEW2_MOUSE_EVENT_KIND_RIGHT_BUTTON_DOUBLE_CLICK,
            PointerUpdateKind.RightButtonPressed => COREWEBVIEW2_MOUSE_EVENT_KIND
                .COREWEBVIEW2_MOUSE_EVENT_KIND_RIGHT_BUTTON_DOWN,
            PointerUpdateKind.XButton1Pressed when clickCount == 2 => COREWEBVIEW2_MOUSE_EVENT_KIND
                .COREWEBVIEW2_MOUSE_EVENT_KIND_X_BUTTON_DOUBLE_CLICK,
            PointerUpdateKind.XButton1Pressed => COREWEBVIEW2_MOUSE_EVENT_KIND
                .COREWEBVIEW2_MOUSE_EVENT_KIND_X_BUTTON_DOWN,
            PointerUpdateKind.XButton2Pressed when clickCount == 2 => COREWEBVIEW2_MOUSE_EVENT_KIND
                .COREWEBVIEW2_MOUSE_EVENT_KIND_X_BUTTON_DOUBLE_CLICK,
            PointerUpdateKind.XButton2Pressed => COREWEBVIEW2_MOUSE_EVENT_KIND
                .COREWEBVIEW2_MOUSE_EVENT_KIND_X_BUTTON_DOWN,
            PointerUpdateKind.LeftButtonReleased => COREWEBVIEW2_MOUSE_EVENT_KIND
                .COREWEBVIEW2_MOUSE_EVENT_KIND_LEFT_BUTTON_UP,
            PointerUpdateKind.MiddleButtonReleased => COREWEBVIEW2_MOUSE_EVENT_KIND
                .COREWEBVIEW2_MOUSE_EVENT_KIND_MIDDLE_BUTTON_UP,
            PointerUpdateKind.RightButtonReleased => COREWEBVIEW2_MOUSE_EVENT_KIND
                .COREWEBVIEW2_MOUSE_EVENT_KIND_RIGHT_BUTTON_UP,
            PointerUpdateKind.XButton1Released => COREWEBVIEW2_MOUSE_EVENT_KIND
                .COREWEBVIEW2_MOUSE_EVENT_KIND_X_BUTTON_UP,
            PointerUpdateKind.XButton2Released => COREWEBVIEW2_MOUSE_EVENT_KIND
                .COREWEBVIEW2_MOUSE_EVENT_KIND_X_BUTTON_UP,
            PointerUpdateKind.Other => COREWEBVIEW2_MOUSE_EVENT_KIND
                .COREWEBVIEW2_MOUSE_EVENT_KIND_MOVE,
            _ => throw new ArgumentOutOfRangeException(nameof(point.Properties.PointerUpdateKind))
        };
        _controller.SendMouseInput(changeType, virtualKeys, 0, position);
        return true;
    }

    public bool PointerLeaveInput(PointerPoint point, double dpi, KeyModifiers modifiers)
    {
        // WebView2 expects leave events without position or modifier info.
        _controller.SendMouseInput(
            COREWEBVIEW2_MOUSE_EVENT_KIND.COREWEBVIEW2_MOUSE_EVENT_KIND_LEAVE,
            COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS.COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS_NONE,
            0,
            default);
        return true;
    }

    public bool PointerWheelInput(Vector delta, PointerPoint point, double dpi, KeyModifiers modifiers)
    {
        var virtualKeys = KeyModifiersToVirtualKey(modifiers, point);
        var position = ToPoint(point.Position, dpi);
        if (delta.Y != 0)
        {
            _controller.SendMouseInput(
                COREWEBVIEW2_MOUSE_EVENT_KIND.COREWEBVIEW2_MOUSE_EVENT_KIND_WHEEL,
                virtualKeys, (int)(delta.Y * 120), position);
        }

        if (delta.X != 0)
        {
            _controller.SendMouseInput(
                COREWEBVIEW2_MOUSE_EVENT_KIND.COREWEBVIEW2_MOUSE_EVENT_KIND_HORIZONTAL_WHEEL,
                virtualKeys, (int)(delta.X * 120), position);
        }

        return true;
    }

    private static tagPOINT ToPoint(Point point, double dpi)
    {
        return new tagPOINT { x = (int)(point.X * dpi), y = (int)(point.Y * dpi) };
    }

    private static COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS KeyModifiersToVirtualKey(
        KeyModifiers modifiers, PointerPoint point)
    {
        var flags = COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS.COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS_NONE;
        if (modifiers.HasFlag(KeyModifiers.Shift))
            flags |= COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS.COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS_SHIFT;
        if (modifiers.HasFlag(KeyModifiers.Control))
            flags |= COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS.COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS_CONTROL;
        if (point.Properties.IsLeftButtonPressed)
            flags |= COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS.COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS_LEFT_BUTTON;
        if (point.Properties.IsRightButtonPressed)
            flags |= COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS.COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS_RIGHT_BUTTON;
        if (point.Properties.IsMiddleButtonPressed)
            flags |= COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS.COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS_MIDDLE_BUTTON;
        if (point.Properties.IsXButton1Pressed)
            flags |= COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS.COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS_X_BUTTON1;
        if (point.Properties.IsXButton2Pressed)
            flags |= COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS.COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS_X_BUTTON2;
        return flags;
    }

    [GeneratedComClass]
    private partial class CommitAsyncLoopHandler(WebView2CompAdapter adapter, ICompositor5 compositor) : IAsyncActionCompletedHandler
    {
        public void RegisterNext()
        {
            var commitAsync = compositor.RequestCommitAsync();
            commitAsync.SetCompleted(this);
        }

        public void Invoke(IAsyncAction asyncInfo, AsyncStatus asyncStatus)
        {
            if (asyncStatus == AsyncStatus.Completed && !adapter.Disposed)
            {
                adapter._drawRequestedHandler?.Invoke();
            }
        }
    }
}
