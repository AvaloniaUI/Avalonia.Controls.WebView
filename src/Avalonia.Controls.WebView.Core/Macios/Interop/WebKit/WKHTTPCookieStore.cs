using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Avalonia.Controls.Macios.Interop.WebKit;

internal class WKHTTPCookieStore(IntPtr handle, bool owns) : NSObject(handle, owns)
{
    private static readonly IntPtr s_getAllCookies = Libobjc.sel_getUid("getAllCookies:");
    private static readonly IntPtr s_setCookie = Libobjc.sel_getUid("setCookie:completionHandler:");
    private static readonly IntPtr s_deleteCookie = Libobjc.sel_getUid("deleteCookie:completionHandler:");
    private static readonly unsafe IntPtr s_getAllCookiesCallback = new((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)&GetAllCookiesCallback);
    private static readonly unsafe IntPtr s_setDeleteCallback = new((delegate* unmanaged[Cdecl]<IntPtr, void>)&SetDeleteCallback);

    public void SetCookie(Cookie cookie)
    {
        using var nsCookie = NSHTTPCookie.Create(cookie);
        var block = BlockLiteral.GetBlockForFunctionPointer(s_setDeleteCallback, default);
        Libobjc.void_objc_msgSend(Handle, s_setCookie, nsCookie.Handle, block);
    }

    public void DeleteCookie(Cookie cookie)
    {
        using var nsCookie = NSHTTPCookie.Create(cookie);
        var block = BlockLiteral.GetBlockForFunctionPointer(s_setDeleteCallback, default);
        Libobjc.void_objc_msgSend(Handle, s_deleteCookie, nsCookie.Handle, block);
    }

    public async Task<IReadOnlyList<Cookie>> GetAllCookies()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<Cookie>>();
        var stateHandle = GCHandle.Alloc(tcs);
        try
        {
            var block = BlockLiteral.GetBlockForFunctionPointer(s_getAllCookiesCallback, GCHandle.ToIntPtr(stateHandle));
            Libobjc.void_objc_msgSend(Handle, s_getAllCookies, block);
            return await tcs.Task;
        }
        finally
        {
            stateHandle.Free();
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void GetAllCookiesCallback(IntPtr block, IntPtr arrayOfCookies)
    {
        var statePtr = BlockLiteral.TryGetBlockState(block);
        if (GCHandle.FromIntPtr(statePtr).Target is not TaskCompletionSource<IReadOnlyList<Cookie>> tcs)
            return;

        if (arrayOfCookies != default)
        {
            try
            {
                using var array = new NSArray(arrayOfCookies, false);
                var arrayCount = array.Count;
                var cookies = stackalloc IntPtr[(int)arrayCount];
                array.GetObjects(new IntPtr(cookies), 0, arrayCount);

                var converter = new List<Cookie>((int)arrayCount);
                for (var i = 0; i < (int)arrayCount; i++)
                {
                    var cookie = new NSHTTPCookie(cookies[i], false);
                    if (cookie.ToSystemCookie() is { } systemCookie)
                    {
                        converter.Add(systemCookie);
                    }
                }
                _ = tcs.TrySetResult(converter);
            }
            catch (Exception ex)
            {
                _ = tcs.TrySetException(ex);
            }
        }
        else
        {
            _ = tcs.TrySetResult([]);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void SetDeleteCallback(IntPtr block) { }
}
