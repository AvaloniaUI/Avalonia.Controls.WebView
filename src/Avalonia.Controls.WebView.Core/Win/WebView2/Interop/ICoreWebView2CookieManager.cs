using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("177CD9E7-B6F5-451A-94A0-5D7A3A4C4141")]
internal partial interface ICoreWebView2CookieManager
{
	[return: MarshalAs(UnmanagedType.Interface)]
	ICoreWebView2Cookie CreateCookie([MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.LPWStr)] string value, [MarshalAs(UnmanagedType.LPWStr)] string Domain, [MarshalAs(UnmanagedType.LPWStr)] string Path);

	[return: MarshalAs(UnmanagedType.Interface)]
	ICoreWebView2Cookie CopyCookie([MarshalAs(UnmanagedType.Interface)] ICoreWebView2Cookie cookieParam);

	void GetCookies([MarshalAs(UnmanagedType.LPWStr)] string? uri, [MarshalAs(UnmanagedType.Interface)] ICoreWebView2GetCookiesCompletedHandler handler);

	void AddOrUpdateCookie([MarshalAs(UnmanagedType.Interface)] ICoreWebView2Cookie cookie);

	void DeleteCookie([MarshalAs(UnmanagedType.Interface)] ICoreWebView2Cookie cookie);

	void DeleteCookies([MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.LPWStr)] string uri);

	void DeleteCookiesWithDomainAndPath([MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.LPWStr)] string Domain, [MarshalAs(UnmanagedType.LPWStr)] string Path);

	void DeleteAllCookies();
}

