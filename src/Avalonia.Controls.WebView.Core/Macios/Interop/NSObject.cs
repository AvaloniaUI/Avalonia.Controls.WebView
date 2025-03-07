using System;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Macios.Interop;

internal abstract class NSObject : IDisposable, IEquatable<NSObject>
{
    private static readonly IntPtr s_class = Libobjc.objc_getClass("NSObject");
    private static readonly IntPtr s_allocSel = Libobjc.sel_getUid("alloc");
    private static readonly IntPtr s_initSel = Libobjc.sel_getUid("init");
    private static readonly IntPtr s_retainSel = Libobjc.sel_getUid("retain");
    private static readonly IntPtr s_releaseSel = Libobjc.sel_getUid("release");
    private static readonly IntPtr s_deallocSel = Libobjc.sel_getUid("dealloc");
    private static readonly IntPtr s_autoreleaseSel = Libobjc.sel_getUid("autorelease");
    private static readonly IntPtr s_retainCountSel = Libobjc.sel_getUid("retainCount");
    private static readonly IntPtr s_conformsToProtocol = Libobjc.sel_getUid("conformsToProtocol:");
    private static readonly IntPtr s_respondsToSelector = Libobjc.sel_getUid("respondsToSelector:");
    private static readonly IntPtr s_description = Libobjc.sel_getUid("description");

    private bool _owns;
    private IntPtr? _class;
    private unsafe ObjcSuper* _superRef;

    private const bool RetainIfNotOwned = false;

    protected NSObject(IntPtr handle, bool owns)
    {
        if (handle == default)
            throw new ArgumentNullException(nameof(handle));

        Handle = handle;
        if (!owns && RetainIfNotOwned)
        {
            owns = true;
            Retain();
        }
        _owns = owns;
    }

    protected NSObject(IntPtr classHandle) : this(Libobjc.intptr_objc_msgSend(classHandle, s_allocSel), true)
    {
        _class = classHandle;
    }

    public IntPtr Class => _class ??= Libobjc.object_getClass(Handle);
    public IntPtr Handle { get; }

    public static IntPtr AllocateClassPair(string className)
        => AllocateClassPair(s_class, className);
    public static IntPtr AllocateClassPair(IntPtr superclass, string className)
        => Libobjc.objc_allocateClassPair(superclass, className, 0);

    public IntPtr Retain() => Libobjc.intptr_objc_msgSend(Handle, s_retainSel);
    public int RetainCount() => Libobjc.int_objc_msgSend(Handle, s_retainCountSel);

    protected void Init()
    {
        _ = Libobjc.intptr_objc_msgSend(Handle, s_initSel);
    }

    public static bool ConformsToProtocol(IntPtr handle, IntPtr protocolHandle)
    {
        return Libobjc.int_objc_msgSend(handle, s_conformsToProtocol, protocolHandle) == 1;
    }

    public static bool RespondsToSelector(IntPtr handle, IntPtr selectorHandle)
    {
        return Libobjc.int_objc_msgSend(handle, s_respondsToSelector, selectorHandle) == 1;
    }

    public static string? GetDescription(IntPtr handle) =>
        NSString.GetString(Libobjc.intptr_objc_msgSend(handle, s_description));

    protected unsafe bool SetIvarValue(string varName, IntPtr value)
    {
        var ivarPtr = GetIvarPointer(Handle, varName);
        if (ivarPtr == default)
            return false;
        *(IntPtr*)ivarPtr = value;
        return true;
    }

    protected IntPtr GetIvarValue(string varName)
    {
        return GetIvarValue(Handle, varName);
    }

    protected static unsafe IntPtr GetIvarValue(IntPtr handle, string varName)
    {
        var ivarPtr = GetIvarPointer(handle, varName);
        if (ivarPtr == default)
            return default;
        return *(IntPtr*)ivarPtr;
    }

    private static unsafe IntPtr GetIvarPointer(IntPtr baseHandle, string varName)
    {
        var ivar = Libobjc.class_getInstanceVariable(Libobjc.object_getClass(baseHandle), varName);
        if (ivar == default)
            return default;
        return new IntPtr((long)baseHandle + (long)Libobjc.ivar_getOffset(ivar));
    }

    internal void UnsafeDisown()
    {
        _owns = false;
    }

    private unsafe void ReleaseUnmanagedResources(bool disposing)
    {
        if (!_owns && _superRef == default)
            return;

#if DEBUG
        Console.WriteLine($"Disposing ({disposing}): {GetType()}");
#endif

        // if (_superRef != default)
        // {
        //     Marshal.Release(new IntPtr(_superRef));
        // }

        if (_owns)
        {
            //Libobjc.void_objc_msgSend(Handle, s_releaseSel);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources(disposing);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // Finalizer is dangerous here, as we don't know well if ObjC side still uses object or not. Be careful.
    ~NSObject()
    {
        Dispose(false);
    }

    public override string? ToString()
    {
        return GetDescription(Handle);
    }

    public bool Equals(NSObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Handle == other.Handle;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((NSObject)obj);
    }

    public override int GetHashCode()
    {
        return Handle.GetHashCode();
    }

    public static bool operator ==(NSObject? left, NSObject? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(NSObject? left, NSObject? right)
    {
        return !Equals(left, right);
    }

    protected internal unsafe IntPtr GetSuperRef()
    {
        if (_superRef == default)
        {
            _superRef = (ObjcSuper*)Marshal.AllocHGlobal(sizeof(ObjcSuper));
            _superRef->ClassHandle = Libobjc.class_getSuperclass(Class);
            _superRef->Handle = Handle;
        }
        return new IntPtr(_superRef);
    }

    [StructLayout (LayoutKind.Sequential)]
    private struct ObjcSuper {
        public IntPtr Handle;
        public IntPtr ClassHandle;
    }
}
