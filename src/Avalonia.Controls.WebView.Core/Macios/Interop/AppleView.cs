using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Avalonia.Controls.Macios.Interop;

/// <summary>
/// NSView on macOS or UIView on iOS
/// </summary>
internal unsafe class AppleView : NSManagedObjectBase
{
    private static readonly void* s_performKeyEquivalent = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, int>)&OnPerformKeyEquivalent;
    private static readonly void* s_acceptsFirstResponder = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int>)&AcceptsFirstResponder;
    private static readonly void* s_becomeFirstResponder = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int>)&OnBecomeFirstResponder;
    private static readonly void* s_resignFirstResponder = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int>)&OnResignFirstResponder;

    private static readonly IntPtr s_copy = Libobjc.sel_getUid("copy:");
    private static readonly IntPtr s_paste = Libobjc.sel_getUid("paste:");
    private static readonly IntPtr s_cut = Libobjc.sel_getUid("cut:");
    private static readonly IntPtr s_selectAll = Libobjc.sel_getUid("selectAll:");
    private static readonly IntPtr s_undoManager = Libobjc.sel_getUid("undoManager");
    private static readonly IntPtr s_undoManagerCanRedo = Libobjc.sel_getUid("canRedo");
    private static readonly IntPtr s_undoManagerCanUndo = Libobjc.sel_getUid("canUndo");
    private static readonly IntPtr s_undoManagerRedo = Libobjc.sel_getUid("redo");
    private static readonly IntPtr s_undoManagerUndo = Libobjc.sel_getUid("undo");

    private static readonly IntPtr s_superview = Libobjc.sel_getUid("superview");
    private static readonly IntPtr s_window = Libobjc.sel_getUid("window");
    private static readonly IntPtr s_windowMakeFirstResponder = Libobjc.sel_getUid("makeFirstResponder:");
    private static readonly IntPtr s_windowFirstResponder = Libobjc.sel_getUid("firstResponder");
    private static readonly IntPtr s_removeFromSuperview = Libobjc.sel_getUid("removeFromSuperview");
    private static readonly IntPtr s_backgroundColor =  Libobjc.sel_getUid("backgroundColor");
    private static readonly IntPtr s_setBackgroundColor =  Libobjc.sel_getUid("setBackgroundColor:");
    private static readonly IntPtr s_opaque =  Libobjc.sel_getUid("isOpaque");
    private static readonly IntPtr s_setOpaque =  Libobjc.sel_getUid("setOpaque:");
    private static readonly IntPtr s_contentSize =  Libobjc.sel_getUid("contentSize"); 

    private static readonly NSString s_drawsBackground = NSString.Create("drawsBackground");

    protected static void RegisterMethods(IntPtr thisClass)
    {
        var performKeyEquivalentSel = Libobjc.sel_getUid("performKeyEquivalent:");
        AddMethod(thisClass, performKeyEquivalentSel, new IntPtr(s_performKeyEquivalent), "B@:@");

        var acceptsFirstResponderSel = Libobjc.sel_getUid("acceptsFirstResponder");
        AddMethod(thisClass, acceptsFirstResponderSel, new IntPtr(s_acceptsFirstResponder), "B@:");

        var becomeFirstResponderSel = Libobjc.sel_getUid("becomeFirstResponder");
        AddMethod(thisClass, becomeFirstResponderSel, new IntPtr(s_becomeFirstResponder), "B@:");

        var resignFirstResponderSel = Libobjc.sel_getUid("resignFirstResponder");
        AddMethod(thisClass, resignFirstResponderSel, new IntPtr(s_resignFirstResponder), "B@:");
    }

    public AppleView(IntPtr handle, bool owns) : base(handle, owns)
    {
    }

    protected AppleView(IntPtr classHandle) : base(classHandle)
    {
    }

    public event EventHandler<PerformKeyEquivalentEventArgs>? PerformKeyEquivalent;
    public event EventHandler? BecomeFirstResponder;
    public event EventHandler? ResignFirstResponder;

    [SupportedOSPlatform("macos")]
    public bool IsFirstResponder
    {
        get
        {
            var windowPtr = Libobjc.intptr_objc_msgSend(Handle, s_window);
            if (windowPtr != default)
            {
                return Libobjc.intptr_objc_msgSend(windowPtr, s_windowFirstResponder) == Handle;
            }

            return false;
        }
    }

    public AppleView? Superview => Libobjc.intptr_objc_msgSend(Handle, s_superview) is var val && val != IntPtr.Zero
        ? new AppleView(val, false) : null;

    public AppleColor? BackgroundColor
    {
        get => Libobjc.intptr_objc_msgSend(Handle, s_backgroundColor) is var val && val != IntPtr.Zero
            ? AppleColor.FromHandle(val) : null;
        set => Libobjc.void_objc_msgSend(Handle, s_setBackgroundColor, value?.Handle ?? IntPtr.Zero);
    }

    public bool Opaque
    {
        get => Libobjc.byte_objc_msgSend(Handle, s_opaque) != 0;
        set => Libobjc.void_objc_msgSend(Handle, s_setOpaque, value ? 1 : 0);
    }

    [SupportedOSPlatform("macos")]
    public bool DrawsBackground
    {
        get => ValueForKey(s_drawsBackground) == NSNumber.Yes.Handle;
        set => SetValueForKey(value ? NSNumber.Yes.Handle : NSNumber.No.Handle, s_drawsBackground);
    }

    public CGSize ContentSize
    {
        get => Libobjc.CGSize_objc_msgSend(Handle, s_contentSize);
    }

    public void Copy() => Libobjc.void_objc_msgSend(Handle, s_copy);
    public void Paste() => Libobjc.void_objc_msgSend(Handle, s_paste);
    public void Cut() => Libobjc.void_objc_msgSend(Handle, s_cut);
    public void SelectAll() => Libobjc.void_objc_msgSend(Handle, s_selectAll);
    public bool Undo()
    {
        var undoManagerPtr = Libobjc.intptr_objc_msgSend(Handle, s_undoManager);
        if (undoManagerPtr == IntPtr.Zero) return false;
        // Report unhandled when there is nothing to undo, so the key equivalent can continue to other
        // consumers (an NSView practically always has an undo manager, but not an undo stack).
        if (Libobjc.int_objc_msgSend(undoManagerPtr, s_undoManagerCanUndo) == 0) return false;
        Libobjc.void_objc_msgSend(undoManagerPtr, s_undoManagerUndo);
        return true;
    }
    public bool Redo()
    {
        var undoManagerPtr = Libobjc.intptr_objc_msgSend(Handle, s_undoManager);
        if (undoManagerPtr == IntPtr.Zero) return false;
        if (Libobjc.int_objc_msgSend(undoManagerPtr, s_undoManagerCanRedo) == 0) return false;
        Libobjc.void_objc_msgSend(undoManagerPtr, s_undoManagerRedo);
        return true;
    }

    [SupportedOSPlatform("macos")]
    public bool MakeFirstResponder()
    {
        var windowPtr = Libobjc.intptr_objc_msgSend(Handle, s_window);
        if (windowPtr != IntPtr.Zero)
        {
            return Libobjc.byte_objc_msgSend(windowPtr, s_windowMakeFirstResponder, Handle) != 0;
        }

        return false;
    }

    [SupportedOSPlatform("macos")]
    public bool RemoveFirstResponder()
    {
        var windowPtr = Libobjc.intptr_objc_msgSend(Handle, s_window);
        if (windowPtr != IntPtr.Zero)
        {
            var firstResponderPtr = Libobjc.intptr_objc_msgSend(windowPtr, s_windowFirstResponder);
            var avViewPtr = Libobjc.intptr_objc_msgSend(Libobjc.intptr_objc_msgSend(Handle, s_superview), s_superview);
            if (avViewPtr != default && firstResponderPtr == Handle)
            {
                return Libobjc.byte_objc_msgSend(windowPtr, s_windowMakeFirstResponder, avViewPtr) != 0;
            }
        }

        return false;
    }

    public void RemoveFromSuperview() => Libobjc.void_objc_msgSend(Handle, s_removeFromSuperview);

    public static IntPtr GetWindow(IntPtr view) => Libobjc.intptr_objc_msgSend(view, s_window);

    public class PerformKeyEquivalentEventArgs : HandledEventArgs
    {
        public required NSEvent Event { get; init; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int OnPerformKeyEquivalent(IntPtr self, IntPtr sel, IntPtr nsEvent)
    {
        var managedSelf = ReadManagedSelf<AppleView>(self);
        if (managedSelf is null)
            return 0;

        using var ev = new NSEvent(nsEvent, false);
        var args = new PerformKeyEquivalentEventArgs { Event = ev };
        managedSelf.PerformKeyEquivalent?.Invoke(managedSelf, args);

        if (args.Handled)
            return 1;

        return Libobjc.byte_objc_msgSendSuper(managedSelf.GetSuperRef(), sel, nsEvent) != 0 ? 1 : 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int AcceptsFirstResponder(IntPtr self, IntPtr sel)
    {
        var managedSelf = ReadManagedSelf(self);
        return managedSelf is null ? 0 : 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int OnBecomeFirstResponder(IntPtr self, IntPtr sel)
    {
        var managedSelf = ReadManagedSelf<AppleView>(self);
        if (managedSelf is null)
            return 0;

        if (Libobjc.byte_objc_msgSendSuper(managedSelf.GetSuperRef(), sel) == 0)
            return 0;

        var args = new CancelEventArgs();
        managedSelf.BecomeFirstResponder?.Invoke(managedSelf, args);
        return args.Cancel ? 0 : 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int OnResignFirstResponder(IntPtr self, IntPtr sel)
    {
        var managedSelf = ReadManagedSelf<AppleView>(self);
        if (managedSelf is null)
            return 0;

        if (Libobjc.byte_objc_msgSendSuper(managedSelf.GetSuperRef(), sel) == 0)
            return 0;

        var args = new CancelEventArgs();
        managedSelf.ResignFirstResponder?.Invoke(managedSelf, args);
        return args.Cancel ? 0 : 1;
    }
}
