using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace Avalonia.Controls.Macios.Interop.AuthenticationServices;

[SupportedOSPlatform("macos10.15")]
[SupportedOSPlatform("ios13.0")]
internal unsafe class ASWebAuthenticationSession : NSManagedObjectBase
{
    private readonly ASWebAuthenticationSessionCallback? _callback;
    private static readonly IntPtr s_class = AuthenticationServices.objc_getClass("ASWebAuthenticationSession");
    private static readonly IntPtr s_initWithURL = Libobjc.sel_getUid("initWithURL:callback:completionHandler:");
    private static readonly IntPtr s_initWithURLOld = Libobjc.sel_getUid("initWithURL:callbackURLScheme:completionHandler:");
    private static readonly IntPtr s_start = Libobjc.sel_getUid("start");
    private static readonly IntPtr s_prefersEphemeralWebBrowserSession = Libobjc.sel_getUid("prefersEphemeralWebBrowserSession");
    private static readonly IntPtr s_setPrefersEphemeralWebBrowserSession = Libobjc.sel_getUid("setPrefersEphemeralWebBrowserSession:");
    private static readonly IntPtr s_setPresentationContextProvider = Libobjc.sel_getUid("setPresentationContextProvider:");

    private static readonly IntPtr s_completionHandler = new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)&CompletionHandler);

    private ASWebAuthenticationSession(ASWebAuthenticationSessionCallback? callback) : base(s_class)
    {
        _callback = callback;
    }

    public bool PrefersEphemeralWebBrowserSession
    {
        get => Libobjc.int_objc_msgSend(Handle, s_prefersEphemeralWebBrowserSession) == 1;
        set => Libobjc.void_objc_msgSend(Handle, s_setPrefersEphemeralWebBrowserSession, value ? 1 : 0);
    }

    public ASWebAuthenticationPresentationContextProviding? PresentationContextProvider
    {
        set => Libobjc.void_objc_msgSend(Handle, s_setPresentationContextProvider, value?.Handle ?? default);
    }

    public void Start() => Libobjc.void_objc_msgSend(Handle, s_start);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _callback?.Dispose();
        }
        base.Dispose(disposing);
    }

    [UnsupportedOSPlatform("macos14.4")]
    [UnsupportedOSPlatform("ios17.4")]
    public static ASWebAuthenticationSession InitWithURL(
        NSUrl url,
        NSString callbackUrlScheme,
        TaskCompletionSource<Uri> completionHandler)
    {
        var session = new ASWebAuthenticationSession(null);
        var completionHandlerState = GCHandle.Alloc(completionHandler);
        var block = BlockLiteral.GetBlockForFunctionPointer(s_completionHandler, GCHandle.ToIntPtr(completionHandlerState));
        _ = Libobjc.intptr_objc_msgSend(session.Handle, s_initWithURLOld, url.Handle, callbackUrlScheme.Handle, block);
        return session;
    }

    [SupportedOSPlatform("macos14.4")]
    [SupportedOSPlatform("ios17.4")]
    public static ASWebAuthenticationSession InitWithURL(
        NSUrl url,
        ASWebAuthenticationSessionCallback callback,
        TaskCompletionSource<Uri> completionHandler)
    {
        var session = new ASWebAuthenticationSession(callback);
        var completionHandlerState = GCHandle.Alloc(completionHandler);
        var block = BlockLiteral.GetBlockForFunctionPointer(s_completionHandler, GCHandle.ToIntPtr(completionHandlerState));
        _ = Libobjc.intptr_objc_msgSend(session.Handle, s_initWithURL, url.Handle, callback.Handle, block);
        return session;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void CompletionHandler(IntPtr block, IntPtr nsUrl, IntPtr nsError)
    {
        var state = BlockLiteral.TryGetBlockState(block);
        var handle = GCHandle.FromIntPtr(state);
        try
        {
            if (handle.Target is not TaskCompletionSource<Uri> tcs)
                return;

            if (nsError != default)
            {
                var error = NSError.ToException(nsError);
                if (error is { Domain: "com.apple.AuthenticationServices.WebAuthenticationSession", Code: 1 })
                {
                    _ = tcs.TrySetCanceled();
                }
                else
                {
                    _ = tcs.TrySetException(error);
                }
            }
            else
            {
                var result = new NSUrl(nsUrl, false).AbsoluteString!;
                _ = tcs.TrySetResult(new Uri(result));
            }
        }
        finally
        {
            handle.Free();
        }
    }
}
