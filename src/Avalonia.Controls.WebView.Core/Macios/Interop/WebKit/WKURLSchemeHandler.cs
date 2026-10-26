using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Macios.Interop.WebKit;

internal unsafe class WKURLSchemeHandler : NSManagedObjectBase
{
    private static readonly IntPtr s_class;

    private static readonly delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, void>
        s_startURLSchemeTask = &OnStartURLSchemeTask;
    private static readonly delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, void>
        s_stopURLSchemeTask = &OnStopURLSchemeTask;

    static WKURLSchemeHandler()
    {
        var handlerClass = AllocateClassPair("ManagedWKURLSchemeHandler");

        var startURLSchemeTask = Libobjc.sel_getUid("webView:startURLSchemeTask:");
        var stopURLSchemeTask = Libobjc.sel_getUid("webView:stopURLSchemeTask:");

        AddProtocol(handlerClass, "WKURLSchemeHandler",
            new ProtocolMethod(startURLSchemeTask, "v@:@@", true),
            new ProtocolMethod(stopURLSchemeTask, "v@:@@", true));

        AddMethod(handlerClass, startURLSchemeTask, new IntPtr(s_startURLSchemeTask), "v@:@@");
        AddMethod(handlerClass, stopURLSchemeTask, new IntPtr(s_stopURLSchemeTask), "v@:@@");

        RegisterManagedMembers(handlerClass);

        Libobjc.objc_registerClassPair(handlerClass);
        s_class = handlerClass;
    }

    public WKURLSchemeHandler() : base(s_class)
    {
        Init();
    }

    public event EventHandler<URLSchemeTaskEventArgs>? StartURLSchemeTask;
    public event EventHandler<URLSchemeTaskEventArgs>? StopURLSchemeTask;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnStartURLSchemeTask(IntPtr self, IntPtr sel, IntPtr webView, IntPtr task)
    {
        var managed = ReadManagedSelf<WKURLSchemeHandler>(self);
        managed?.StartURLSchemeTask?.Invoke(managed, new URLSchemeTaskEventArgs { Task = task });
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnStopURLSchemeTask(IntPtr self, IntPtr sel, IntPtr webView, IntPtr task)
    {
        var managed = ReadManagedSelf<WKURLSchemeHandler>(self);
        managed?.StopURLSchemeTask?.Invoke(managed, new URLSchemeTaskEventArgs { Task = task });
    }

    public class URLSchemeTaskEventArgs : EventArgs
    {
        public IntPtr Task { get; init; }
    }
}

/// <summary>
/// Retains a WKURLSchemeTask while the response is produced.
/// </summary>
/// <remarks>
/// WebKit throws if a task is answered after webView:stopURLSchemeTask:, callers must stop using it from then on.
/// </remarks>
internal class WKURLSchemeTask(IntPtr handle) : NSObject(handle, true)
{
    private static readonly IntPtr s_request = Libobjc.sel_getUid("request");
    private static readonly IntPtr s_didReceiveResponse = Libobjc.sel_getUid("didReceiveResponse:");
    private static readonly IntPtr s_didReceiveData = Libobjc.sel_getUid("didReceiveData:");
    private static readonly IntPtr s_didFinish = Libobjc.sel_getUid("didFinish");
    private static readonly IntPtr s_didFailWithError = Libobjc.sel_getUid("didFailWithError:");

    public NSURLRequest Request => NSURLRequest.FromHandle(Libobjc.intptr_objc_msgSend(Handle, s_request));

    public void DidReceiveResponse(NSHTTPURLResponse response) =>
        Libobjc.void_objc_msgSend(Handle, s_didReceiveResponse, response.Handle);

    public void DidReceiveData(NSData data) => Libobjc.void_objc_msgSend(Handle, s_didReceiveData, data.Handle);

    public void DidFinish() => Libobjc.void_objc_msgSend(Handle, s_didFinish);

    public void DidFailWithError(IntPtr error) => Libobjc.void_objc_msgSend(Handle, s_didFailWithError, error);
}
