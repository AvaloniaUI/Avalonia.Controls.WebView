using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Logging;

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
    private static readonly IntPtr s_setValueForKeySel = Libobjc.sel_getUid("setValue:forKey:");
    private static readonly IntPtr s_valueForKeySel = Libobjc.sel_getUid("valueForKey:");

    private bool _owns;
    private IntPtr? _class;
    private unsafe ObjcSuper* _superRef;

    protected NSObject(IntPtr handle, bool owns)
    {
        if (handle == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(handle));
        }

        Handle = handle;
        if (owns)
        {
            // This object is owned (and usually created) by C# code, and we want to keep it retained while C# reference is alive.
            // Native object is released in Dispose method. 
            // When "owns" is false, we do not retain nor release any objects.
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
    {
        if (superclass == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(superclass));
        }

        if (string.IsNullOrEmpty(className))
        {
            throw new ArgumentNullException(nameof(className));
        }

        var result = Libobjc.objc_allocateClassPair(superclass, className, 0);
        if (result == 0)
        {
            throw new ObjectDisposedException(className, "objc_allocateClassPair returned null object");
        }

        return result;
    }

    protected static void AddProtocol(IntPtr classHandle, IntPtr protocol)
    {
        if (classHandle == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(classHandle));
        }

        if (protocol == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(protocol));
        }

        if (Libobjc.class_addProtocol(classHandle, protocol) == 0)
        {
            throw new ObjectDisposedException("objc_addProtocol returned an error");
        }
    }

    /// <summary>
    /// Adds the protocol to the class.
    /// If the runtime doesn't know the protocol yet, registers one with the same name and the given methods.
    /// </summary>
    /// <remarks>
    /// objc_getProtocol only finds protocols that some loaded image references.
    /// On iOS 26, WebKit protocols are not always registered, and WebKit rejects handlers that don't conform.
    /// The runtime checks conformance by protocol name, so a protocol with the same name is enough for conformsToProtocol:.
    /// Method descriptions are still added, so code that inspects the protocol sees the same methods as in the original one.
    /// </remarks>
    protected static void AddProtocol(IntPtr classHandle, string protocolName, params ReadOnlySpan<ProtocolMethod> methods)
    {
        var protocol = Libobjc.objc_getProtocol(protocolName);
        if (protocol == IntPtr.Zero)
        {
            protocol = Libobjc.objc_allocateProtocol(protocolName);
            if (protocol == IntPtr.Zero)
            {
                // Another thread or image registered it in the meantime.
                protocol = Libobjc.objc_getProtocol(protocolName);
            }
            else
            {
                if (Libobjc.objc_getProtocol("NSObject") is var nsObjectProtocol and not 0)
                {
                    Libobjc.protocol_addProtocol(protocol, nsObjectProtocol);
                }
                foreach (var method in methods)
                {
                    Libobjc.protocol_addMethodDescription(protocol, method.Selector, method.Types,
                        method.Required ? (byte)1 : (byte)0, 1);
                }
                Libobjc.objc_registerProtocol(protocol);
            }
        }

        if (protocol == IntPtr.Zero)
        {
            Logger.TryGet(LogEventLevel.Warning, "WebView")?.Log(null, "Failed to resolve {Protocol} protocol", protocolName);
            return;
        }

        AddProtocol(classHandle, protocol);
    }

    protected static void AddMethod(IntPtr classHandle, IntPtr selector, IntPtr methodHandler, string types)
    {
        if (classHandle == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(classHandle));
        }

        if (selector == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        if (methodHandler == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(methodHandler));
        }

        if (Libobjc.class_addMethod(classHandle, selector, methodHandler, types) == 0)
        {
            throw new ObjectDisposedException("objc_addMethod returned an error");
        }
    }

    public IntPtr Retain() => Libobjc.intptr_objc_msgSend(Handle, s_retainSel);
    public void Release() => Libobjc.void_objc_msgSend(Handle, s_releaseSel);
    public nuint RetainCount() => Libobjc.nuint_objc_msgSend(Handle, s_retainCountSel);

    protected void Init()
    {
        _ = Libobjc.intptr_objc_msgSend(Handle, s_initSel);
    }

    public static bool ConformsToProtocol(IntPtr handle, IntPtr protocolHandle)
    {
        return Libobjc.byte_objc_msgSend(handle, s_conformsToProtocol, protocolHandle) != 0;
    }

    public static bool RespondsToSelector(IntPtr handle, IntPtr selectorHandle)
    {
        return Libobjc.byte_objc_msgSend(handle, s_respondsToSelector, selectorHandle) != 0;
    }

    public static string? GetDescription(IntPtr handle) =>
        NSString.GetString(Libobjc.intptr_objc_msgSend(handle, s_description));

    public void SetValueForKey(IntPtr value, NSString key) => Libobjc.void_objc_msgSend(
        Handle,
        s_setValueForKeySel,
        value,
        key.Handle);

    public IntPtr ValueForKey(NSString key) => Libobjc.intptr_objc_msgSend(
        Handle,
        s_valueForKeySel,
        key.Handle);

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
        Debug.WriteLine($"Disposing (finalize: {!disposing}, owns: {_owns}): {GetType()}");
#endif

        // if (_superRef != default)
        // {
        //     Marshal.Release(new IntPtr(_superRef));
        // }

        if (_owns)
        {
            Libobjc.void_objc_msgSend(Handle, s_releaseSel);
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

    protected readonly record struct ProtocolMethod(IntPtr Selector, string Types, bool Required);

    [StructLayout (LayoutKind.Sequential)]
    private struct ObjcSuper {
        public IntPtr Handle;
        public IntPtr ClassHandle;
    }
}
