using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("80A22AE3-BE7C-4CE2-AFE1-5A50056CDEEB")]
internal partial interface ICoreWebView2Environment3 : ICoreWebView2Environment2
{
    void CreateCoreWebView2CompositionController(IntPtr ParentWindow, [MarshalAs(UnmanagedType.Interface)] ICoreWebView2CreateCoreWebView2CompositionControllerCompletedHandler handler);

    IntPtr CreateCoreWebView2PointerInfo();
}
