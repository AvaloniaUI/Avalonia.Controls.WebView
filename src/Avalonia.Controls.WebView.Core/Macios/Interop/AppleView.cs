using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Avalonia.Controls.Macios.Interop;

/// <summary>
/// NSView on macOS or UIView on iOS
/// </summary>
internal abstract unsafe class AppleView : NSManagedObjectBase
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
    private static readonly IntPtr s_undoManagerRedo = Libobjc.sel_getUid("redo");
    private static readonly IntPtr s_undoManagerUndo = Libobjc.sel_getUid("undo");

    private static readonly IntPtr s_superview = Libobjc.sel_getUid("superview");
    private static readonly IntPtr s_window = Libobjc.sel_getUid("window");
    private static readonly IntPtr s_windowMakeFirstResponder = Libobjc.sel_getUid("makeFirstResponder:");
    private static readonly IntPtr s_windowFirstResponder = Libobjc.sel_getUid("firstResponder");
    private static readonly IntPtr s_removeFromSuperview = Libobjc.sel_getUid("removeFromSuperview");

    protected static void RegisterMethods(IntPtr thisClass)
    {
        var performKeyEquivalentSel = Libobjc.sel_getUid("performKeyEquivalent:");
        var result = Libobjc.class_addMethod(thisClass, performKeyEquivalentSel, s_performKeyEquivalent, "B@:@");
        Debug.Assert(result == 1);

        var acceptsFirstResponderSel = Libobjc.sel_getUid("acceptsFirstResponder");
        result = Libobjc.class_addMethod(thisClass, acceptsFirstResponderSel, s_acceptsFirstResponder, "B@:");
        Debug.Assert(result == 1);

        var becomeFirstResponderSel = Libobjc.sel_getUid("becomeFirstResponder");
        result = Libobjc.class_addMethod(thisClass, becomeFirstResponderSel, s_becomeFirstResponder, "B@:");
        Debug.Assert(result == 1);

        var resignFirstResponderSel = Libobjc.sel_getUid("resignFirstResponder");
        result = Libobjc.class_addMethod(thisClass, resignFirstResponderSel, s_resignFirstResponder, "B@:");
        Debug.Assert(result == 1);
    }

    protected AppleView(IntPtr handle, bool owns) : base(handle, owns)
    {
    }

    protected AppleView(IntPtr classHandle) : base(classHandle)
    {
    }

    public event EventHandler<PerformKeyEquivalentEventArgs> PerformKeyEquivalent;
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

    public void Copy() => Libobjc.void_objc_msgSend(Handle, s_copy);
    public void Paste() => Libobjc.void_objc_msgSend(Handle, s_paste);
    public void Cut() => Libobjc.void_objc_msgSend(Handle, s_cut);
    public void SelectAll() => Libobjc.void_objc_msgSend(Handle, s_selectAll);
    public bool Undo()
    {
        var undoManagerPtr = Libobjc.intptr_objc_msgSend(Handle, s_undoManager);
        if (undoManagerPtr == IntPtr.Zero) return false;
        Libobjc.void_objc_msgSend(undoManagerPtr, s_undoManagerUndo);
        return true;
    }
    public bool Redo()
    {
        var undoManagerPtr = Libobjc.intptr_objc_msgSend(Handle, s_undoManager);
        if (undoManagerPtr == IntPtr.Zero) return false;
        Libobjc.void_objc_msgSend(undoManagerPtr, s_undoManagerRedo);
        return true;
    }

    [SupportedOSPlatform("macos")]
    public bool MakeFirstResponder()
    {
        var windowPtr = Libobjc.intptr_objc_msgSend(Handle, s_window);
        if (windowPtr != IntPtr.Zero)
        {
            return Libobjc.int_objc_msgSend(windowPtr, s_windowMakeFirstResponder, Handle) == 1;
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
                return Libobjc.int_objc_msgSend(windowPtr, s_windowMakeFirstResponder, avViewPtr) == 1;   
            }
        }

        return false;
    }

    public void RemoveFromSuperview() => Libobjc.void_objc_msgSend(Handle, s_removeFromSuperview);

    public static IntPtr GetWindow(IntPtr view) => Libobjc.intptr_objc_msgSend(view, s_window);

    public class PerformKeyEquivalentEventArgs : HandledEventArgs
    {
        public NSEvent Event { get; init; }
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

        return Libobjc.int_objc_msgSendSuper(managedSelf.GetSuperRef(), sel, nsEvent);
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

        if (Libobjc.int_objc_msgSendSuper(managedSelf.GetSuperRef(), sel) == 0)
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

        if (Libobjc.int_objc_msgSendSuper(managedSelf.GetSuperRef(), sel) == 0)
            return 0;

        var args = new CancelEventArgs();
        managedSelf.ResignFirstResponder?.Invoke(managedSelf, args);
        return args.Cancel ? 0 : 1;
    }
}
