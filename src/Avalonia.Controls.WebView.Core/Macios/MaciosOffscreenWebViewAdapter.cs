using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Macios.Interop;
using Avalonia.Controls.Macios.Interop.WebKit;
using Avalonia.Controls.Rendering;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.Controls.Macios;

/// <summary>
/// macOS offscreen WKWebView adapter that renders into a WriteableBitmap via takeSnapshot API,
/// and synthesizes input events using CGEvent APIs.
/// Requires macOS 10.15+ for takeSnapshot support.
/// </summary>
[SupportedOSPlatform("macos10.15")]
internal sealed class MaciosOffscreenWebViewAdapter : MaciosWebViewAdapter,
    IWebViewAdapterWithOffscreenBuffer, IWebViewAdapterWithOffscreenInput, IWebViewAdapterWithExplicitCursor
{
    private static readonly IntPtr s_takeSnapshotWithConfiguration =
        Libobjc.sel_getUid("takeSnapshotWithConfiguration:completionHandler:");

    private static readonly IntPtr s_NSWindowClass = Libobjc.objc_getClass("NSWindow");
    private static readonly IntPtr s_NSWindowAlloc = Libobjc.sel_getUid("alloc");
    private static readonly IntPtr s_NSWindowInit =
        Libobjc.sel_getUid("initWithContentRect:styleMask:backing:defer:");
    private static readonly IntPtr s_NSWindowSetContentView = Libobjc.sel_getUid("setContentView:");
    private static readonly IntPtr s_NSWindowOrderOut = Libobjc.sel_getUid("orderOut:");
    private static readonly IntPtr s_NSWindowClose = Libobjc.sel_getUid("close");
    private static readonly IntPtr s_NSWindowContentView = Libobjc.sel_getUid("contentView");
    private static readonly IntPtr s_NSWindowSetReleasedWhenClosed = Libobjc.sel_getUid("setReleasedWhenClosed:");

    private static readonly IntPtr s_setFrame = Libobjc.sel_getUid("setFrame:");
    private static readonly IntPtr s_setFrameDisplay = Libobjc.sel_getUid("setFrame:display:");
    private static readonly IntPtr s_setNeedsDisplay = Libobjc.sel_getUid("setNeedsDisplay:");
    private static readonly IntPtr s_display = Libobjc.sel_getUid("display");
    private static readonly IntPtr s_window = Libobjc.sel_getUid("window");

    // NSResponder event methods — called directly on the WKWebView to bypass window focus requirement
    private static readonly IntPtr s_mouseDown = Libobjc.sel_getUid("mouseDown:");
    private static readonly IntPtr s_mouseUp = Libobjc.sel_getUid("mouseUp:");
    private static readonly IntPtr s_mouseMoved = Libobjc.sel_getUid("mouseMoved:");
    private static readonly IntPtr s_mouseDragged = Libobjc.sel_getUid("mouseDragged:");
    private static readonly IntPtr s_mouseEntered = Libobjc.sel_getUid("mouseEntered:");
    private static readonly IntPtr s_mouseExited = Libobjc.sel_getUid("mouseExited:");
    private static readonly IntPtr s_rightMouseDown = Libobjc.sel_getUid("rightMouseDown:");
    private static readonly IntPtr s_rightMouseUp = Libobjc.sel_getUid("rightMouseUp:");
    private static readonly IntPtr s_rightMouseDragged = Libobjc.sel_getUid("rightMouseDragged:");
    private static readonly IntPtr s_otherMouseDown = Libobjc.sel_getUid("otherMouseDown:");
    private static readonly IntPtr s_otherMouseUp = Libobjc.sel_getUid("otherMouseUp:");
    private static readonly IntPtr s_otherMouseDragged = Libobjc.sel_getUid("otherMouseDragged:");
    private static readonly IntPtr s_scrollWheel = Libobjc.sel_getUid("scrollWheel:");
    private static readonly IntPtr s_keyDown = Libobjc.sel_getUid("keyDown:");
    private static readonly IntPtr s_keyUp = Libobjc.sel_getUid("keyUp:");

    // NSEvent factory selectors
    private static readonly IntPtr s_NSEventClass = Libobjc.objc_getClass("NSEvent");
    private static readonly IntPtr s_mouseEventWithType =
        Libobjc.sel_getUid("mouseEventWithType:location:modifierFlags:timestamp:windowNumber:context:eventNumber:clickCount:pressure:");
    private static readonly IntPtr s_keyEventWithType =
        Libobjc.sel_getUid("keyEventWithType:location:modifierFlags:timestamp:windowNumber:context:characters:charactersIgnoringModifiers:isARepeat:keyCode:");
    private static readonly IntPtr s_windowNumber = Libobjc.sel_getUid("windowNumber");

    // NSImage -> CGImage
    private static readonly IntPtr s_CGImageForProposedRect =
        Libobjc.sel_getUid("CGImageForProposedRect:context:hints:");

    // CGImage pixel data
    private static readonly IntPtr s_CGImageGetWidth = Libobjc.sel_getUid("width");
    private static readonly IntPtr s_CGImageGetHeight = Libobjc.sel_getUid("height");

    private static readonly unsafe IntPtr s_takeSnapshotCallback =
        new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)&TakeSnapshotCallback);

    private static readonly IntPtr s_makeFirstResponder = Libobjc.sel_getUid("makeFirstResponder:");

    private IntPtr _offscreenWindow;
    private PixelSize _currentSize;
    private DisplayLinkTimer? _displayLink;
    private double _backingScaleFactor = 1.0;
    private double _screenHeightPts;
    private double _windowHeightPts;

    public MaciosOffscreenWebViewAdapter(AppleWKWebViewEnvironmentRequestedEventArgs options) : base(options)
    {
        // Get backing scale factor from main screen
        var nsScreenClass = Libobjc.objc_getClass("NSScreen");
        var mainScreen = Libobjc.intptr_objc_msgSend(nsScreenClass, Libobjc.sel_getUid("mainScreen"));
        _backingScaleFactor = Libobjc.double_objc_msgSend(mainScreen, Libobjc.sel_getUid("backingScaleFactor"));
        if (_backingScaleFactor <= 0) _backingScaleFactor = 1.0;

        // Get screen height in points for CG coordinate conversion
        var displayBounds = CGDisplayBounds(CGMainDisplayID());
        _screenHeightPts = displayBounds.Height;

        _offscreenWindow = CreateOffscreenWindow(100, 100);

        // Set the WKWebView as the content view of the offscreen window
        Libobjc.void_objc_msgSend(_offscreenWindow, s_NSWindowSetContentView, Handle);

        // Make WKWebView first responder for keyboard input
        Libobjc.void_objc_msgSend(_offscreenWindow, s_makeFirstResponder, Handle);

        _displayLink = new DisplayLinkTimer(OnDisplayLinkFired);
    }

    public event Action? DrawRequested;
    public event EventHandler? CursorChanged;

    public StandardCursorType CurrentCursorType => StandardCursorType.Arrow;

    public static async Task<WebViewAdapter.OffscreenWebViewAdapterBuilder> CreateBuilder(
        AppleWKWebViewEnvironmentRequestedEventArgs environmentArgs)
    {
        await Task.Yield();
        return (parent) =>
        {
            var adapter = new MaciosOffscreenWebViewAdapter(environmentArgs);
            return Task.FromResult<IWebViewAdapterWithOffscreenBuffer>(adapter);
        };
    }

    public new void SizeChanged(PixelSize containerSize)
    {
        if (_offscreenWindow == IntPtr.Zero || containerSize.Width <= 0 || containerSize.Height <= 0)
            return;

        _currentSize = containerSize;

        // Convert pixel size to macOS points (pixels / backingScaleFactor)
        var widthPts = containerSize.Width / _backingScaleFactor;
        var heightPts = containerSize.Height / _backingScaleFactor;
        _windowHeightPts = heightPts;

        // Resize the offscreen window and WKWebView in points
        var frame = new CGRect(0, 0, widthPts, heightPts);
        Libobjc.void_objc_msgSend(_offscreenWindow, s_setFrameDisplay, frame, 1);
        Libobjc.void_objc_msgSend(Handle, s_setFrame, frame);

        _displayLink?.RequestNextFrame();
    }

    public async Task UpdateWriteableBitmap(PixelSize currentSize,
        FrameChainBase<WriteableBitmap, PixelSize>.IProducer producer)
    {
        if (_offscreenWindow == IntPtr.Zero || currentSize.Width <= 0 || currentSize.Height <= 0)
            return;

        var nsImage = await TakeSnapshotAsync(currentSize);
        if (nsImage == IntPtr.Zero)
            return;

        try
        {
            // Get CGImage from NSImage
            var cgImage = Libobjc.intptr_objc_msgSend(nsImage, s_CGImageForProposedRect,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            if (cgImage == IntPtr.Zero)
                return;

            var width = (int)CGImageGetWidth(cgImage);
            var height = (int)CGImageGetHeight(cgImage);

            if (width == 0 || height == 0)
                return;

            var size = new PixelSize(width, height);

            // Get the pixel data from CGImage
            var colorSpace = CGColorSpaceCreateDeviceRGB();
            var bytesPerRow = width * 4;
            var dataSize = bytesPerRow * height;
            var rawData = Marshal.AllocHGlobal(dataSize);

            try
            {
                var context = CGBitmapContextCreate(
                    rawData,
                    (nuint)width,
                    (nuint)height,
                    8, // bits per component
                    (nuint)bytesPerRow,
                    colorSpace,
                    (uint)(CGBitmapFlags.ByteOrder32Little | CGBitmapFlags.PremultipliedFirst)); // BGRA

                if (context != IntPtr.Zero)
                {
                    CGContextDrawImage(context, new CGRectNative(0, 0, width, height), cgImage);
                    CGContextRelease(context);

                    using (producer.GetNextFrame(size, out var frame))
                    {
                        using var buf = frame.Lock();
                        unsafe
                        {
                            var copyBytes = Math.Min(buf.RowBytes * height, dataSize);
                            Buffer.MemoryCopy(
                                source: (void*)rawData,
                                destination: (void*)buf.Address,
                                destinationSizeInBytes: buf.RowBytes * height,
                                sourceBytesToCopy: copyBytes);
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(rawData);
                CGColorSpaceRelease(colorSpace);
            }
        }
        finally
        {
            // Release the NSImage
            Libobjc.void_objc_msgSend(nsImage, Libobjc.sel_getUid("release"));
        }

        // Request next frame
        _displayLink?.RequestNextFrame();
    }

    public bool KeyInput(bool press, PhysicalKey physical, string? symbol, KeyModifiers modifiers)
    {
        if (_offscreenWindow == IntPtr.Zero)
            return false;

        var keyCode = KeyTransformReverse.GetKeyCodeForPhysicalKey(physical);
        if (keyCode < 0)
            return false;

        var nsEventType = press ? NSEventType.KeyDown : NSEventType.KeyUp;
        var nsModifiers = ToNSEventModifierFlags(modifiers);
        var windowNum = Libobjc.int_objc_msgSend(_offscreenWindow, s_windowNumber);

        // Create characters string (use symbol if available, else empty)
        var characters = symbol != null
            ? Libobjc.intptr_objc_msgSend(
                Libobjc.objc_getClass("NSString"), Libobjc.sel_getUid("stringWithUTF8String:"),
                Marshal.StringToHGlobalAnsi(symbol))
            : CFStringCreateEmpty();
        var charactersIgnoring = characters;

        var nsEvent = NSEvent_keyEventWithType(
            s_NSEventClass, s_keyEventWithType,
            nsEventType,
            new CGPointNative(0, 0), // location in window
            nsModifiers,
            0.0, // timestamp
            windowNum,
            IntPtr.Zero, // context (nil)
            characters,
            charactersIgnoring,
            false, // isARepeat
            (ushort)keyCode);

        if (nsEvent == IntPtr.Zero)
            return false;

        var sel = press ? s_keyDown : s_keyUp;
        Libobjc.void_objc_msgSend(Handle, sel, nsEvent);
        return true;
    }

    public bool PointerInput(PointerPoint point, int clickCount, double dpi, KeyModifiers modifiers)
    {
        if (_offscreenWindow == IntPtr.Zero)
            return false;

        var (nsEventType, buttonNumber, viewSelector) = point.Properties.PointerUpdateKind switch
        {
            PointerUpdateKind.LeftButtonPressed => (NSEventType.LeftMouseDown, 0, s_mouseDown),
            PointerUpdateKind.LeftButtonReleased => (NSEventType.LeftMouseUp, 0, s_mouseUp),
            PointerUpdateKind.RightButtonPressed => (NSEventType.RightMouseDown, 1, s_rightMouseDown),
            PointerUpdateKind.RightButtonReleased => (NSEventType.RightMouseUp, 1, s_rightMouseUp),
            PointerUpdateKind.MiddleButtonPressed => (NSEventType.OtherMouseDown, 2, s_otherMouseDown),
            PointerUpdateKind.MiddleButtonReleased => (NSEventType.OtherMouseUp, 2, s_otherMouseUp),
            PointerUpdateKind.Other => (NSEventType.MouseMoved, 0, s_mouseMoved),
            _ => (NSEventType.MouseMoved, -1, IntPtr.Zero)
        };

        if (viewSelector == IntPtr.Zero)
            return false;

        // If it's a move while a button is held, adjust the type
        if (nsEventType == NSEventType.MouseMoved)
        {
            if (point.Properties.IsLeftButtonPressed)
            {
                nsEventType = NSEventType.LeftMouseDragged;
                viewSelector = s_mouseDragged;
            }
            else if (point.Properties.IsRightButtonPressed)
            {
                nsEventType = NSEventType.RightMouseDragged;
                viewSelector = s_rightMouseDragged;
            }
            else if (point.Properties.IsMiddleButtonPressed)
            {
                nsEventType = NSEventType.OtherMouseDragged;
                viewSelector = s_otherMouseDragged;
            }
        }

        // point.Position is in logical coordinates (= macOS points).
        // NSEvent locationInWindow uses window coordinates: origin at bottom-left.
        var locationInWindow = new CGPointNative(
            point.Position.X,
            _windowHeightPts - point.Position.Y);

        var nsModifiers = ToNSEventModifierFlags(modifiers);
        var windowNum = Libobjc.int_objc_msgSend(_offscreenWindow, s_windowNumber);

        var nsEvent = NSEvent_mouseEventWithType(
            s_NSEventClass, s_mouseEventWithType,
            nsEventType,
            locationInWindow,
            nsModifiers,
            0.0, // timestamp
            windowNum,
            IntPtr.Zero, // context (nil)
            0, // eventNumber
            clickCount,
            (buttonNumber == 0 && clickCount == 0) ? 0.0f : 1.0f); // pressure

        if (nsEvent == IntPtr.Zero)
            return false;

        Libobjc.void_objc_msgSend(Handle, viewSelector, nsEvent);
        return true;
    }

    public bool PointerLeaveInput(PointerPoint point, double dpi, KeyModifiers modifiers)
    {
        if (_offscreenWindow == IntPtr.Zero)
            return false;

        var locationInWindow = new CGPointNative(
            point.Position.X,
            _windowHeightPts - point.Position.Y);

        var windowNum = Libobjc.int_objc_msgSend(_offscreenWindow, s_windowNumber);

        var nsEvent = NSEvent_mouseEventWithType(
            s_NSEventClass, s_mouseEventWithType,
            NSEventType.MouseExited,
            locationInWindow,
            ToNSEventModifierFlags(modifiers),
            0.0,
            windowNum,
            IntPtr.Zero,
            0, 0, 0.0f);

        if (nsEvent == IntPtr.Zero)
            return false;

        Libobjc.void_objc_msgSend(Handle, s_mouseExited, nsEvent);
        return true;
    }

    public bool PointerWheelInput(Vector delta, PointerPoint point, double dpi, KeyModifiers modifiers)
    {
        if (_offscreenWindow == IntPtr.Zero)
            return false;

        // Create a CGEvent-based scroll wheel event and convert to NSEvent,
        // because NSEvent doesn't have a direct factory for scroll wheel events.
        var cgEvent = CGEventCreateScrollWheelEvent2(IntPtr.Zero,
            CGScrollEventUnit.Pixel, 2,
            (int)(delta.Y * 40), (int)(delta.X * 40));

        if (cgEvent == IntPtr.Zero)
            return false;

        try
        {
            CGEventSetFlags(cgEvent, ToCGEventFlags(modifiers));

            // Convert to NSEvent — scroll wheel events work even without window focus
            // because they carry their own location data.
            var nsEvent = NSEventFromCGEvent(cgEvent);
            if (nsEvent == IntPtr.Zero)
                return false;

            Libobjc.void_objc_msgSend(Handle, s_scrollWheel, nsEvent);
            return true;
        }
        finally
        {
            CFRelease(cgEvent);
        }
    }

    public new void Dispose()
    {
        Interlocked.Exchange(ref _displayLink, null)?.Dispose();

        var window = Interlocked.Exchange(ref _offscreenWindow, IntPtr.Zero);
        if (window != IntPtr.Zero)
        {
            Libobjc.void_objc_msgSend(window, s_NSWindowClose);
            Libobjc.void_objc_msgSend(window, Libobjc.sel_getUid("release"));
        }

        base.Dispose();
    }

    private void OnDisplayLinkFired()
    {
        WebViewDispatcher.InvokeAsync(() => DrawRequested?.Invoke());
    }

    private Task<IntPtr> TakeSnapshotAsync(PixelSize size)
    {
        var tcs = new TaskCompletionSource<IntPtr>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stateHandle = GCHandle.Alloc(tcs);

        try
        {
            var block = BlockLiteral.GetBlockForFunctionPointer(
                s_takeSnapshotCallback, GCHandle.ToIntPtr(stateHandle));

            // Pass nil for configuration — uses the view's current bounds automatically.
            Libobjc.void_objc_msgSend(Handle, s_takeSnapshotWithConfiguration, IntPtr.Zero, block);
        }
        catch (Exception ex)
        {
            stateHandle.Free();
            tcs.TrySetException(ex);
        }

        return tcs.Task;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void TakeSnapshotCallback(IntPtr block, IntPtr nsImage, IntPtr nsError)
    {
        var statePtr = BlockLiteral.TryGetBlockState(block);
        if (statePtr == IntPtr.Zero)
            return;

        var stateHandle = GCHandle.FromIntPtr(statePtr);
        if (stateHandle.Target is not TaskCompletionSource<IntPtr> tcs)
            return;

        try
        {
            if (nsError != IntPtr.Zero)
            {
                tcs.TrySetException(NSError.ToException(nsError));
                return;
            }

            if (nsImage != IntPtr.Zero)
            {
                // Retain the image so it survives beyond the callback
                Libobjc.intptr_objc_msgSend(nsImage, Libobjc.sel_getUid("retain"));
            }

            tcs.TrySetResult(nsImage);
        }
        finally
        {
            stateHandle.Free();
        }
    }

    private static IntPtr CreateOffscreenWindow(int width, int height)
    {
        // NSWindow style: NSWindowStyleMaskBorderless = 0
        var windowPtr = Libobjc.intptr_objc_msgSend(s_NSWindowClass, s_NSWindowAlloc);

        // initWithContentRect:styleMask:backing:defer:
        // styleMask = 0 (borderless), backing = 2 (NSBackingStoreBuffered), defer = 0
        windowPtr = Libobjc.intptr_objc_msgSend(windowPtr, s_NSWindowInit,
            new CGRect(0, 0, width, height), 0, 2, 0);

        // setReleasedWhenClosed: NO
        Libobjc.void_objc_msgSend(windowPtr, s_NSWindowSetReleasedWhenClosed, 0);

        // Order out so it's not visible
        Libobjc.void_objc_msgSend(windowPtr, s_NSWindowOrderOut, IntPtr.Zero);

        return windowPtr;
    }

    private static NSEventModifierFlags ToNSEventModifierFlags(KeyModifiers modifiers)
    {
        var flags = NSEventModifierFlags.None;
        if (modifiers.HasFlag(KeyModifiers.Shift))
            flags |= NSEventModifierFlags.Shift;
        if (modifiers.HasFlag(KeyModifiers.Control))
            flags |= NSEventModifierFlags.Control;
        if (modifiers.HasFlag(KeyModifiers.Alt))
            flags |= NSEventModifierFlags.Option;
        if (modifiers.HasFlag(KeyModifiers.Meta))
            flags |= NSEventModifierFlags.Command;
        return flags;
    }

    private static CGEventFlags ToCGEventFlags(KeyModifiers modifiers)
    {
        var flags = CGEventFlags.None;
        if (modifiers.HasFlag(KeyModifiers.Shift))
            flags |= CGEventFlags.MaskShift;
        if (modifiers.HasFlag(KeyModifiers.Control))
            flags |= CGEventFlags.MaskControl;
        if (modifiers.HasFlag(KeyModifiers.Alt))
            flags |= CGEventFlags.MaskAlternate;
        if (modifiers.HasFlag(KeyModifiers.Meta))
            flags |= CGEventFlags.MaskCommand;
        return flags;
    }

    #region Native Interop

    private const string CoreGraphics =
        "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string CoreFoundation =
        "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string AppKit =
        "/System/Library/Frameworks/AppKit.framework/AppKit";
    private const string libobjc = "/usr/lib/libobjc.dylib";

    // CGEvent APIs — still needed for scroll wheel events (no NSEvent factory for scroll)
    [DllImport(CoreGraphics, EntryPoint = "CGEventCreateScrollWheelEvent2")]
    private static extern IntPtr CGEventCreateScrollWheelEvent2(IntPtr source, CGScrollEventUnit units, uint wheelCount, int value1, int value2);

    [DllImport(CoreGraphics)]
    private static extern void CGEventSetFlags(IntPtr @event, CGEventFlags flags);

    // CGImage / CGBitmapContext
    [DllImport(CoreGraphics)]
    private static extern nuint CGImageGetWidth(IntPtr image);

    [DllImport(CoreGraphics)]
    private static extern nuint CGImageGetHeight(IntPtr image);

    [DllImport(CoreGraphics)]
    private static extern IntPtr CGColorSpaceCreateDeviceRGB();

    [DllImport(CoreGraphics)]
    private static extern void CGColorSpaceRelease(IntPtr colorSpace);

    [DllImport(CoreGraphics)]
    private static extern IntPtr CGBitmapContextCreate(
        IntPtr data, nuint width, nuint height, nuint bitsPerComponent,
        nuint bytesPerRow, IntPtr colorSpace, uint bitmapInfo);

    [DllImport(CoreGraphics)]
    private static extern void CGContextDrawImage(IntPtr context, CGRectNative rect, IntPtr image);

    [DllImport(CoreGraphics)]
    private static extern void CGContextRelease(IntPtr context);

    [DllImport(CoreGraphics)]
    private static extern uint CGMainDisplayID();

    [DllImport(CoreGraphics)]
    private static extern CGRectNative CGDisplayBounds(uint display);

    [DllImport(CoreFoundation)]
    private static extern void CFRelease(IntPtr obj);

    // NSEvent from CGEvent (for scroll wheel)
    private static readonly IntPtr s_eventWithCGEvent = Libobjc.sel_getUid("eventWithCGEvent:");

    private static IntPtr NSEventFromCGEvent(IntPtr cgEvent)
    {
        return Libobjc.intptr_objc_msgSend(s_NSEventClass, s_eventWithCGEvent, cgEvent);
    }

    // NSEvent factory: +[NSEvent mouseEventWithType:location:modifierFlags:timestamp:windowNumber:context:eventNumber:clickCount:pressure:]
    // Signature: id, SEL, NSEventType(long), CGPoint(double,double), NSEventModifierFlags(ulong), double, long, id, long, long, float
    [DllImport(libobjc, EntryPoint = "objc_msgSend")]
    private static extern IntPtr NSEvent_mouseEventWithType(
        IntPtr cls, IntPtr sel,
        NSEventType type,
        CGPointNative location,
        NSEventModifierFlags modifierFlags,
        double timestamp,
        long windowNumber,
        IntPtr context,
        long eventNumber,
        long clickCount,
        float pressure);

    // NSEvent factory: +[NSEvent keyEventWithType:location:modifierFlags:timestamp:windowNumber:context:characters:charactersIgnoringModifiers:isARepeat:keyCode:]
    [DllImport(libobjc, EntryPoint = "objc_msgSend")]
    private static extern IntPtr NSEvent_keyEventWithType(
        IntPtr cls, IntPtr sel,
        NSEventType type,
        CGPointNative location,
        NSEventModifierFlags modifierFlags,
        double timestamp,
        long windowNumber,
        IntPtr context,
        IntPtr characters,
        IntPtr charactersIgnoringModifiers,
        [MarshalAs(UnmanagedType.Bool)] bool isARepeat,
        ushort keyCode);

    private static IntPtr CFStringCreateEmpty()
    {
        var cls = Libobjc.objc_getClass("NSString");
        var sel = Libobjc.sel_getUid("string");
        return Libobjc.intptr_objc_msgSend(cls, sel);
    }

    private enum NSEventType : ulong
    {
        LeftMouseDown = 1,
        LeftMouseUp = 2,
        RightMouseDown = 3,
        RightMouseUp = 4,
        MouseMoved = 5,
        LeftMouseDragged = 6,
        RightMouseDragged = 7,
        KeyDown = 10,
        KeyUp = 11,
        FlagsChanged = 12,
        ScrollWheel = 22,
        MouseEntered = 8,
        MouseExited = 9,
        OtherMouseDown = 25,
        OtherMouseUp = 26,
        OtherMouseDragged = 27,
    }

    [Flags]
    private enum NSEventModifierFlags : ulong
    {
        None = 0,
        CapsLock = 1UL << 16,
        Shift = 1UL << 17,
        Control = 1UL << 18,
        Option = 1UL << 19,
        Command = 1UL << 20,
        NumericPad = 1UL << 21,
        Help = 1UL << 22,
        Function = 1UL << 23,
    }

    [Flags]
    private enum CGEventFlags : ulong
    {
        None = 0,
        MaskShift = 0x00020000,
        MaskControl = 0x00040000,
        MaskAlternate = 0x00080000,
        MaskCommand = 0x00100000,
    }

    private enum CGScrollEventUnit : uint
    {
        Pixel = 0,
        Line = 1,
    }

    [Flags]
    private enum CGBitmapFlags : uint
    {
        None = 0,
        PremultipliedLast = 1,
        PremultipliedFirst = 2,
        Last = 3,
        First = 4,
        NoneSkipLast = 5,
        NoneSkipFirst = 6,
        ByteOrder32Little = 1 << 13,
        ByteOrder32Big = 1 << 12,
    }

    [StructLayout(LayoutKind.Sequential)]
    private record struct CGPointNative(double X, double Y);

    [StructLayout(LayoutKind.Sequential)]
    private record struct CGRectNative(double X, double Y, double Width, double Height);

    #endregion
}

/// <summary>
/// Reverse mapping from PhysicalKey to macOS virtual key codes.
/// </summary>
internal static class KeyTransformReverse
{
    public static int GetKeyCodeForPhysicalKey(PhysicalKey key)
    {
        return key switch
        {
            PhysicalKey.Backquote => 0x32,
            PhysicalKey.Backslash => 0x2A,
            PhysicalKey.BracketLeft => 0x21,
            PhysicalKey.BracketRight => 0x1E,
            PhysicalKey.Comma => 0x2B,
            PhysicalKey.Digit0 => 0x1D,
            PhysicalKey.Digit1 => 0x12,
            PhysicalKey.Digit2 => 0x13,
            PhysicalKey.Digit3 => 0x14,
            PhysicalKey.Digit4 => 0x15,
            PhysicalKey.Digit5 => 0x17,
            PhysicalKey.Digit6 => 0x16,
            PhysicalKey.Digit7 => 0x1A,
            PhysicalKey.Digit8 => 0x1C,
            PhysicalKey.Digit9 => 0x19,
            PhysicalKey.Equal => 0x18,
            PhysicalKey.IntlBackslash => 0x0A,
            PhysicalKey.IntlRo => 0x5E,
            PhysicalKey.IntlYen => 0x5D,
            PhysicalKey.A => 0x00,
            PhysicalKey.B => 0x0B,
            PhysicalKey.C => 0x08,
            PhysicalKey.D => 0x02,
            PhysicalKey.E => 0x0E,
            PhysicalKey.F => 0x03,
            PhysicalKey.G => 0x05,
            PhysicalKey.H => 0x04,
            PhysicalKey.I => 0x22,
            PhysicalKey.J => 0x26,
            PhysicalKey.K => 0x28,
            PhysicalKey.L => 0x25,
            PhysicalKey.M => 0x2E,
            PhysicalKey.N => 0x2D,
            PhysicalKey.O => 0x1F,
            PhysicalKey.P => 0x23,
            PhysicalKey.Q => 0x0C,
            PhysicalKey.R => 0x0F,
            PhysicalKey.S => 0x01,
            PhysicalKey.T => 0x11,
            PhysicalKey.U => 0x20,
            PhysicalKey.V => 0x09,
            PhysicalKey.W => 0x0D,
            PhysicalKey.X => 0x07,
            PhysicalKey.Y => 0x10,
            PhysicalKey.Z => 0x06,
            PhysicalKey.Minus => 0x1B,
            PhysicalKey.Period => 0x2F,
            PhysicalKey.Quote => 0x27,
            PhysicalKey.Semicolon => 0x29,
            PhysicalKey.Slash => 0x2C,
            PhysicalKey.AltLeft => 0x3A,
            PhysicalKey.AltRight => 0x3D,
            PhysicalKey.Backspace => 0x33,
            PhysicalKey.CapsLock => 0x39,
            PhysicalKey.ContextMenu => 0x6E,
            PhysicalKey.ControlLeft => 0x3B,
            PhysicalKey.ControlRight => 0x3E,
            PhysicalKey.Enter => 0x24,
            PhysicalKey.MetaLeft => 0x37,
            PhysicalKey.MetaRight => 0x36,
            PhysicalKey.ShiftLeft => 0x38,
            PhysicalKey.ShiftRight => 0x3C,
            PhysicalKey.Space => 0x31,
            PhysicalKey.Tab => 0x30,
            PhysicalKey.Delete => 0x75,
            PhysicalKey.End => 0x77,
            PhysicalKey.Home => 0x73,
            PhysicalKey.Insert => 0x72,
            PhysicalKey.PageDown => 0x79,
            PhysicalKey.PageUp => 0x74,
            PhysicalKey.ArrowDown => 0x7D,
            PhysicalKey.ArrowLeft => 0x7B,
            PhysicalKey.ArrowRight => 0x7C,
            PhysicalKey.ArrowUp => 0x7E,
            PhysicalKey.NumLock => 0x47,
            PhysicalKey.NumPad0 => 0x52,
            PhysicalKey.NumPad1 => 0x53,
            PhysicalKey.NumPad2 => 0x54,
            PhysicalKey.NumPad3 => 0x55,
            PhysicalKey.NumPad4 => 0x56,
            PhysicalKey.NumPad5 => 0x57,
            PhysicalKey.NumPad6 => 0x58,
            PhysicalKey.NumPad7 => 0x59,
            PhysicalKey.NumPad8 => 0x5B,
            PhysicalKey.NumPad9 => 0x5C,
            PhysicalKey.NumPadAdd => 0x45,
            PhysicalKey.NumPadComma => 0x5F,
            PhysicalKey.NumPadDecimal => 0x41,
            PhysicalKey.NumPadDivide => 0x4B,
            PhysicalKey.NumPadEnter => 0x4C,
            PhysicalKey.NumPadEqual => 0x51,
            PhysicalKey.NumPadMultiply => 0x43,
            PhysicalKey.NumPadSubtract => 0x4E,
            PhysicalKey.Escape => 0x35,
            PhysicalKey.F1 => 0x7A,
            PhysicalKey.F2 => 0x78,
            PhysicalKey.F3 => 0x63,
            PhysicalKey.F4 => 0x76,
            PhysicalKey.F5 => 0x60,
            PhysicalKey.F6 => 0x61,
            PhysicalKey.F7 => 0x62,
            PhysicalKey.F8 => 0x64,
            PhysicalKey.F9 => 0x65,
            PhysicalKey.F10 => 0x6D,
            PhysicalKey.F11 => 0x67,
            PhysicalKey.F12 => 0x6F,
            _ => -1
        };
    }
}
