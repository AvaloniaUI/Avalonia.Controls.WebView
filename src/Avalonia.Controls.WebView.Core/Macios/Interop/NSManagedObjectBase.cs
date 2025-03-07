using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Macios.Interop;

internal unsafe class NSManagedObjectBase : NSObject
{
#if DEBUG
    private static readonly void* s_dealloc = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)&DeallocCallback;
    private static readonly void* s_retain = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr>)&RetainCallback;
    private static readonly void* s_release = (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)&ReleaseCallback;
#endif
    private GCHandle _managedHandle;

    public NSManagedObjectBase(IntPtr handle, bool owns) : base(handle, owns)
    {
        WriteManagedSelf();
    }

    public NSManagedObjectBase(IntPtr classHandle) : base(classHandle)
    {
        WriteManagedSelf();
    }

    protected static bool RegisterManagedMembers(IntPtr delegateClass)
    {
        int result;

#if DEBUG && FALSE
        result = Libobjc.class_addMethod(delegateClass, Libobjc.sel_getUid("dealloc"), s_dealloc, "v@:");
        Debug.Assert(result == 1);

        result = Libobjc.class_addMethod(delegateClass, Libobjc.sel_getUid("retain"), s_retain, "@@:");
        Debug.Assert(result == 1);

        result = Libobjc.class_addMethod(delegateClass, Libobjc.sel_getUid("release"), s_release, "v@:");
        Debug.Assert(result == 1);
#endif

        result = Libobjc.class_addIvar(delegateClass, "_managedSelf", new IntPtr(sizeof(IntPtr)), 0, "@");
        return result == 1;
    }

    private void WriteManagedSelf()
    {
        _managedHandle = GCHandle.Alloc(this, GCHandleType.Weak);
        _ = SetIvarValue("_managedSelf", GCHandle.ToIntPtr(_managedHandle));
    }

    protected static TSelf? ReadManagedSelf<TSelf>(IntPtr ptr)
        where TSelf : NSManagedObjectBase
    {
        return ReadManagedSelf(ptr) as TSelf;
    }

    protected static NSObject? ReadManagedSelf(IntPtr ptr)
    {
        var managedHandle = GetIvarValue(ptr, "_managedSelf");
        return managedHandle == default ? null : GCHandle.FromIntPtr(managedHandle).Target as NSObject;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            if (_managedHandle.IsAllocated)
                _managedHandle.Free();
        }
    }

#if DEBUG
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private protected static void DeallocCallback(IntPtr self, IntPtr sel)
    {
        var managedSelf = ReadManagedSelf(self);
        if (managedSelf is null)
            return;
        Libobjc.void_objc_msgSendSuper(managedSelf.GetSuperRef(), sel);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private protected static IntPtr RetainCallback(IntPtr self, IntPtr sel)
    {
        var managedSelf = ReadManagedSelf(self);
        if (managedSelf is null)
            return self;
        return Libobjc.intptr_objc_msgSendSuper(managedSelf.GetSuperRef(), sel);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private protected static void ReleaseCallback(IntPtr self, IntPtr sel)
    {
        var managedSelf = ReadManagedSelf(self);
        if (managedSelf is null)
            return;
        Libobjc.void_objc_msgSendSuper(managedSelf.GetSuperRef(), sel);
    }
#endif
}
