using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;
using Avalonia.Controls.Win.Interop;

namespace Avalonia.Controls.Win.WebView1.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("AF40329B-B544-469B-86B9-AC3D466FEA36")]
internal partial interface IHttpRequestHeaderCollection : IInspectable
{
    IntPtr Accept();
    IntPtr AcceptEncoding();
    IntPtr AcceptLanguage();
    IntPtr Authorization();
    void Authorization(IntPtr value);
    IntPtr CacheControl();
    IntPtr Connection();
    IntPtr Cookie();
    IntPtr Date();
    void Date(IntPtr value);
    IntPtr Expect();
    IntPtr From();
    void From(IntPtr value);
    IntPtr Host();
    void Host(IntPtr value);
    IntPtr IfModifiedSince();
    void IfModifiedSince(IntPtr value);
    IntPtr IfUnmodifiedSince();
    void IfUnmodifiedSince(IntPtr value);
    IntPtr MaxForwards();
    void MaxForwards(IntPtr value);
    IntPtr ProxyAuthorization();
    void ProxyAuthorization(IntPtr value);
    IntPtr Referer();
    void Referer(IntPtr value);
    IntPtr TransferEncoding();
    IntPtr UserAgent();
    void Append(IntPtr name, IntPtr value);

    // returns "winrt.boolean*", I don't need that though
    IntPtr TryAppendWithoutValidation(IntPtr name, IntPtr value);
}
