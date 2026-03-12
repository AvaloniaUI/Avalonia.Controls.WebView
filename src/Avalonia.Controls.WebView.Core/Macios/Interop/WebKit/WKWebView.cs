using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace Avalonia.Controls.Macios.Interop.WebKit;

internal class WKWebView : AppleView
{
    private static readonly IntPtr s_webViewClass;
    private static readonly IntPtr s_initWithFrame = Libobjc.sel_getUid("initWithFrame:configuration:");
    private static readonly IntPtr s_setNavigationDelegate = Libobjc.sel_getUid("setNavigationDelegate:");
    private static readonly IntPtr s_loadRequest = Libobjc.sel_getUid("loadRequest:");
    private static readonly IntPtr s_loadHTMLString = Libobjc.sel_getUid("loadHTMLString:baseURL:");
    private static readonly IntPtr s_url = Libobjc.sel_getUid("URL");
    private static readonly IntPtr s_scrollView = Libobjc.sel_getUid("scrollView");

    private static readonly IntPtr s_canGoBack = Libobjc.sel_getUid("canGoBack");
    private static readonly IntPtr s_goBack = Libobjc.sel_getUid("goBack");
    private static readonly IntPtr s_canGoForward = Libobjc.sel_getUid("canGoForward");
    private static readonly IntPtr s_goForward = Libobjc.sel_getUid("goForward");

    private static readonly IntPtr s_printOperationWithPrintInfo = Libobjc.sel_getUid("printOperationWithPrintInfo:");
    private static readonly IntPtr s_createPDFWithConfiguration = Libobjc.sel_getUid("createPDFWithConfiguration:completionHandler:");

    private static readonly IntPtr s_reload = Libobjc.sel_getUid("reload");
    private static readonly IntPtr s_stopLoading = Libobjc.sel_getUid("stopLoading");

    private static readonly IntPtr s_customUserAgent = Libobjc.sel_getUid("customUserAgent");
    private static readonly IntPtr s_setCustomUserAgent = Libobjc.sel_getUid("setCustomUserAgent:");

    private static readonly IntPtr s_evaluateJavaScript = Libobjc.sel_getUid("evaluateJavaScript:completionHandler:");
    private static readonly IntPtr s_callAsyncJavaScript = Libobjc.sel_getUid("callAsyncJavaScript:arguments:inFrame:inContentWorld:completionHandler:");

    private static readonly unsafe IntPtr s_evaluateScriptCallback = new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)&EvaluateScriptCallback);
    private static readonly unsafe IntPtr s_callAsyncJavaScriptCallback = new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)&CallAsyncJavaScriptCallback);
    private static readonly unsafe IntPtr s_createPDFCallback = new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)&CreatePDFCallback);

    internal static NSString UserAgentKey { get; } = NSString.Create("userAgent");

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

    [SupportedOSPlatform("ios")]
    public AppleView? ScrollView => Libobjc.intptr_objc_msgSend(Handle, s_scrollView) is var val && val != IntPtr.Zero ?
        new AppleView(val, false) :
        null;

    public NSUrl? Url => Libobjc.intptr_objc_msgSend(Handle, s_url) is var handle && handle != default ?
        new(handle, false) :
        null;

    public bool CanGoBack => Libobjc.int_objc_msgSend(Handle, s_canGoBack) == 1;
    public bool CanGoForward => Libobjc.int_objc_msgSend(Handle, s_canGoForward) == 1;

    public IntPtr GoBack() => Libobjc.intptr_objc_msgSend(Handle, s_goBack);
    public IntPtr GoForward() => Libobjc.intptr_objc_msgSend(Handle, s_goForward);
    public IntPtr Reload() => Libobjc.intptr_objc_msgSend(Handle, s_reload);
    public void StopLoading() => Libobjc.void_objc_msgSend(Handle, s_stopLoading);

    public NSString? CustomUserAgent
    {
        get
        {
            var handle = Libobjc.intptr_objc_msgSend(Handle, s_customUserAgent);
            return handle != IntPtr.Zero ? NSString.FromHandle(handle) : null;
        }
        set => Libobjc.void_objc_msgSend(Handle, s_setCustomUserAgent, value?.Handle ?? IntPtr.Zero);
    }

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

    public async Task<string?> EvaluateJavaScriptAsync(string script)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var state = new JSCallState(Handle, tcs);
        var stateHandle = GCHandle.Alloc(state);
        try
        {
            var scriptStr = NSString.Create(script);
            GC.SuppressFinalize(scriptStr);
            var block = BlockLiteral.GetBlockForFunctionPointer(s_evaluateScriptCallback, GCHandle.ToIntPtr(stateHandle));
            Libobjc.void_objc_msgSend(Handle, s_evaluateJavaScript, scriptStr.Handle, block);
            return await tcs.Task;
        }
        finally
        {
            stateHandle.Free();
        }
    }

    public NSPrintOperation? PrintOperationWithPrintInto(NSPrintInfo printInfo)
    {
        var operation = Libobjc.intptr_objc_msgSend(Handle, s_printOperationWithPrintInfo, printInfo.Handle);
        return operation != IntPtr.Zero ? new NSPrintOperation(operation, false) : null;
    }

    public async Task<MemoryStream> CreatePdf(WKPDFConfiguration? configuration)
    {
        var tcs = new TaskCompletionSource<MemoryStream>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stateHandle = GCHandle.Alloc(tcs);
        try
        {
            var block = BlockLiteral.GetBlockForFunctionPointer(s_createPDFCallback, GCHandle.ToIntPtr(stateHandle));
            Libobjc.void_objc_msgSend(Handle,
                s_createPDFWithConfiguration,
                configuration?.Handle ?? IntPtr.Zero,
                block);
            return await tcs.Task;
        }
        finally
        {
            stateHandle.Free();
        }
    }

    internal static async Task PtrResultToString(IntPtr result, JSCallState state)
    {
        if (result == default)
        {
            _ = state.CompletionSource.TrySetResult(null);
            return;
        }

        try
        {
            if (NSString.TryGetString(result) is { } str)
            {
                _ = state.CompletionSource.TrySetResult(str);
                return;
            }

            using var argNameStr = NSString.Create("arg");
            using var args = NSDictionary.WithObjects(
                [result],
                [argNameStr.Handle],
                1);
            using var functionBodyStr = NSString.Create("return JSON.stringify(arg);");

            var stateHandle = GCHandle.Alloc(state);
            try
            {
                var block = BlockLiteral.GetBlockForFunctionPointer(s_callAsyncJavaScriptCallback,
                    GCHandle.ToIntPtr(stateHandle));
                Libobjc.void_objc_msgSend(state.WebView, s_callAsyncJavaScript,
                    functionBodyStr.Handle, args.Handle, default, WKContentWorld.DefaultClientWorld, block);
                _ = await state.CompletionSource.Task;
            }
            finally
            {
                stateHandle.Free();
            }
        }
        catch (Exception ex)
        {
            _ = state.CompletionSource.TrySetException(ex);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void EvaluateScriptCallback(IntPtr block, IntPtr value, IntPtr nsError)
    {
        var statePtr = BlockLiteral.TryGetBlockState(block);
        if (GCHandle.FromIntPtr(statePtr).Target is not JSCallState state)
            return;

        if (nsError != default)
        {
            _ = state.CompletionSource.TrySetException(NSError.ToException(nsError));
        }
        else
        {
            _ = PtrResultToString(value, state);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void CallAsyncJavaScriptCallback(IntPtr block, IntPtr value, IntPtr nsError)
    {
        var statePtr = BlockLiteral.TryGetBlockState(block);
        if (GCHandle.FromIntPtr(statePtr).Target is not JSCallState state)
            return;

        if (nsError != default)
        {
            _ = state.CompletionSource.TrySetException(NSError.ToException(nsError));
        }
        else
        {
            _ = state.CompletionSource.TrySetResult(NSString.TryGetString(value));
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void CreatePDFCallback(IntPtr block, IntPtr data, IntPtr nsError)
    {
        var statePtr = BlockLiteral.TryGetBlockState(block);
        if (GCHandle.FromIntPtr(statePtr).Target is not TaskCompletionSource<MemoryStream> state)
            return;

        if (nsError != default)
        {
            _ = state.TrySetException(NSError.ToException(nsError));
        }
        else
        {
            var dataLength = (int)CFDataGetLength(data);
            var dataBytes = new byte[dataLength];
            fixed (byte* dataPtr = dataBytes)
                CFDataGetBytes(data, new(0, dataLength), dataPtr);
            state.SetResult(new MemoryStream(dataBytes, 0, dataBytes.Length, true, true));
        }
    }

    private const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    [DllImport(CoreFoundationLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr CFDataGetLength(IntPtr cfData);
    [DllImport(CoreFoundationLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void CFDataGetBytes(IntPtr cfData, CFRange range, byte* buffer);
    private struct CFRange(nint location, nint lentgh)
    {
        public nint Location = location;
        public nint Length = lentgh;
    }

    internal record JSCallState(IntPtr WebView, TaskCompletionSource<string?> CompletionSource);
}
