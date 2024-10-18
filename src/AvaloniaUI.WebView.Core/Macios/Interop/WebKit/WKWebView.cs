using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AppleInterop.WebKit;

internal class WKWebView : AppleView
{
    private static readonly IntPtr s_webViewClass;
    private static readonly IntPtr s_initWithFrame = Libobjc.sel_getUid("initWithFrame:configuration:");
    private static readonly IntPtr s_setNavigationDelegate = Libobjc.sel_getUid("setNavigationDelegate:");
    private static readonly IntPtr s_loadRequest = Libobjc.sel_getUid("loadRequest:");
    private static readonly IntPtr s_loadHTMLString = Libobjc.sel_getUid("loadHTMLString:baseURL:");
    private static readonly IntPtr s_url = Libobjc.sel_getUid("URL");

    private static readonly IntPtr s_canGoBack = Libobjc.sel_getUid("canGoBack");
    private static readonly IntPtr s_goBack = Libobjc.sel_getUid("goBack");
    private static readonly IntPtr s_canGoForward = Libobjc.sel_getUid("canGoForward");
    private static readonly IntPtr s_goForward = Libobjc.sel_getUid("goForward");

    private static readonly IntPtr s_reload = Libobjc.sel_getUid("reload");
    private static readonly IntPtr s_stopLoading = Libobjc.sel_getUid("stopLoading");

    private static readonly IntPtr s_evaluateJavaScript = Libobjc.sel_getUid("evaluateJavaScript:completionHandler:");
    private static readonly IntPtr s_callAsyncJavaScript = Libobjc.sel_getUid("callAsyncJavaScript:arguments:inFrame:inContentWorld:completionHandler:");

    private static readonly unsafe IntPtr s_evaluateScriptCallback = new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)&EvaluateScriptCallback);

    static unsafe WKWebView()
    {
        var superclass = WebKit.objc_getClass("WKWebView");
        var webViewClass = AllocateClassPair(superclass, "ManagedWKWebView");

        RegisterMethods(webViewClass);

        var result = RegisterManagedMembers(webViewClass);
        Debug.Assert(result);

        Libobjc.objc_registerClassPair(webViewClass);
        s_webViewClass = webViewClass;
    }

    public WKWebView(WKWebViewConfiguration configuration) : base(s_webViewClass)
    {
        _ = Libobjc.intptr_objc_msgSend(Handle, s_initWithFrame, new CGRect(), configuration.Handle);
    }

    public WKNavigationDelegate? NavigationDelegate
    {
        set
        {
            Libobjc.void_objc_msgSend(Handle, s_setNavigationDelegate, value?.Handle ?? default);
        }
    }

    public NSUrl? Url => Libobjc.intptr_objc_msgSend(Handle, s_url) is var handle && handle != default ?
        new(handle, false) :
        null;

    public bool CanGoBack => Libobjc.int_objc_msgSend(Handle, s_canGoBack) == 1;
    public bool CanGoForward => Libobjc.int_objc_msgSend(Handle, s_canGoForward) == 1;

    public IntPtr GoBack() => Libobjc.intptr_objc_msgSend(Handle, s_goBack);
    public IntPtr GoForward() => Libobjc.intptr_objc_msgSend(Handle, s_goForward);
    public IntPtr Reload() => Libobjc.intptr_objc_msgSend(Handle, s_reload);
    public void StopLoading() => Libobjc.void_objc_msgSend(Handle, s_stopLoading);

    public IntPtr LoadRequest(NSURLRequest? request)
    {
        var result = Libobjc.intptr_objc_msgSend(Handle, s_loadRequest, request?.Handle ?? default);
        return result;
    }

    public IntPtr LoadHtmlString(NSString htmlString, NSUrl baseUrl)
    {
        var result = Libobjc.intptr_objc_msgSend(Handle, s_loadHTMLString, htmlString.Handle, baseUrl.Handle);
        return result;
    }

    public async Task<IntPtr> EvaluateJavaScriptAsync(string script)
    {
        var tcs = new TaskCompletionSource<IntPtr>();
        var tcsHandle = GCHandle.Alloc(tcs);
        try
        {
            var scriptStr = NSString.Create(script);
            GC.SuppressFinalize(scriptStr);
            var block = BlockLiteral.GetBlockForFunctionPointer(s_evaluateScriptCallback, GCHandle.ToIntPtr(tcsHandle));
            Libobjc.void_objc_msgSend(Handle, s_evaluateJavaScript, scriptStr.Handle, block);
            return await tcs.Task;
        }
        finally
        {
            tcsHandle.Free();
        }
    }

    public async Task<IntPtr> CallAsyncJavaScriptAsync(
        string functionBody,
        NSDictionary arguments,
        IntPtr frame,
        IntPtr contentWorld)
    {
        var tcs = new TaskCompletionSource<IntPtr>();
        var tcsHandle = GCHandle.Alloc(tcs);
        try
        {
            var functionBodyStr = NSString.Create(functionBody);
            GC.SuppressFinalize(functionBodyStr);
            var block = BlockLiteral.GetBlockForFunctionPointer(s_evaluateScriptCallback, GCHandle.ToIntPtr(tcsHandle));
            Libobjc.void_objc_msgSend(Handle, s_callAsyncJavaScript,
                functionBodyStr.Handle, arguments.Handle, frame, contentWorld, block);
            return await tcs.Task;
        }
        finally
        {
            tcsHandle.Free();
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void EvaluateScriptCallback(IntPtr block, IntPtr value, IntPtr nsError)
    {
        var state = BlockLiteral.TryGetBlockState(block);
        var tcs = GCHandle.FromIntPtr(state).Target as TaskCompletionSource<IntPtr>;

        if (nsError != default)
        {
            _ = tcs?.TrySetException(NSError.ToException(nsError));
        }
        else
        {
            _ = tcs?.TrySetResult(value);
        }
    }
}
