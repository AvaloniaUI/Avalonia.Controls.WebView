using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls.Macios.Interop;

namespace Avalonia.Controls.Macios.Interop.WebKit;

/// <summary>
/// Managed WKURLSchemeHandler that raises events for custom-scheme requests.
/// </summary>
internal unsafe class WKURLSchemeHandler : NSManagedObjectBase
{
    private static readonly IntPtr s_class;
    private static readonly IntPtr s_taskRequest = Libobjc.sel_getUid("request");
    private static readonly IntPtr s_didReceiveResponse = Libobjc.sel_getUid("didReceiveResponse:");
    private static readonly IntPtr s_didReceiveData = Libobjc.sel_getUid("didReceiveData:");
    private static readonly IntPtr s_didFinish = Libobjc.sel_getUid("didFinish");
    private static readonly IntPtr s_didFailWithError = Libobjc.sel_getUid("didFailWithError:");

    private static readonly delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, void>
        s_startURLSchemeTask = &OnStartURLSchemeTask;
    private static readonly delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, void>
        s_stopURLSchemeTask = &OnStopURLSchemeTask;

    static WKURLSchemeHandler()
    {
        var delegateClass = AllocateClassPair("ManagedWKURLSchemeHandler");

        if (Libobjc.objc_getProtocol("WKURLSchemeHandler") is var protocol and > 0)
            AddProtocol(delegateClass, protocol);

        AddMethod(delegateClass, Libobjc.sel_getUid("webView:startURLSchemeTask:"),
            new IntPtr(s_startURLSchemeTask), "v@:@@");
        AddMethod(delegateClass, Libobjc.sel_getUid("webView:stopURLSchemeTask:"),
            new IntPtr(s_stopURLSchemeTask), "v@:@@");

        RegisterManagedMembers(delegateClass);
        Libobjc.objc_registerClassPair(delegateClass);
        s_class = delegateClass;
    }

    public WKURLSchemeHandler() : base(s_class)
    {
        Init();
    }

    public event EventHandler<URLSchemeTaskEventArgs>? StartURLSchemeTask;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnStartURLSchemeTask(IntPtr self, IntPtr sel, IntPtr webView, IntPtr task)
    {
        var managed = ReadManagedSelf<WKURLSchemeHandler>(self);
        if (managed is null)
            return;

        var requestPtr = Libobjc.intptr_objc_msgSend(task, s_taskRequest);
        managed.StartURLSchemeTask?.Invoke(managed, new URLSchemeTaskEventArgs(task, requestPtr));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnStopURLSchemeTask(IntPtr self, IntPtr sel, IntPtr webView, IntPtr task)
    {
        // No-op: callers finish synchronously or via managed deferral on Start.
    }

    public static void CompleteTask(IntPtr task, NSHTTPURLResponse response, NSData data)
    {
        Libobjc.void_objc_msgSend(task, s_didReceiveResponse, response.Handle);
        Libobjc.void_objc_msgSend(task, s_didReceiveData, data.Handle);
        Libobjc.void_objc_msgSend(task, s_didFinish);
    }

    public static void FailTask(IntPtr task, IntPtr error)
    {
        Libobjc.void_objc_msgSend(task, s_didFailWithError, error);
    }

    public sealed class URLSchemeTaskEventArgs(IntPtr task, IntPtr request) : EventArgs
    {
        public IntPtr Task { get; } = task;
        public IntPtr Request { get; } = request;
    }
}
