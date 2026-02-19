using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("EE0EB9DF-6F12-46CE-B53F-3F47B9C928E0")]
internal partial interface ICoreWebView2Environment10 : ICoreWebView2Environment9
{
    ICoreWebView2ControllerOptions CreateCoreWebView2ControllerOptions();
    void CreateCoreWebView2ControllerWithOptions(IntPtr ParentWindow, ICoreWebView2ControllerOptions options, ICoreWebView2CreateCoreWebView2ControllerCompletedHandler handler);
    void CreateCoreWebView2CompositionControllerWithOptions(IntPtr ParentWindow, ICoreWebView2ControllerOptions options, ICoreWebView2CreateCoreWebView2CompositionControllerCompletedHandler handler);
}

////// Thank you Microsoft for so many environments //////
// GeneratedComInterface doesn't support `_VtblGap1_17`, so we have to actually add these interfaces to be inherited by ICoreWebView2Environment10.

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("F06F41BF-4B5A-49D8-B9F6-FA16CD29F274")]
internal partial interface ICoreWebView2Environment9 : ICoreWebView2Environment8
{
    IntPtr CreateContextMenuItem([MarshalAs(UnmanagedType.LPWStr)] string Label, IntPtr iconStream, int Kind);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("D6EB91DD-C3D2-45E5-BD29-6DC2BC4DE9CF")]
internal partial interface ICoreWebView2Environment8 : ICoreWebView2Environment7
{
    void add_ProcessInfosChanged(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_ProcessInfosChanged(EventRegistrationToken token);
    IntPtr GetProcessInfos();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("43C22296-3BBD-43A4-9C00-5C0DF6DD29A2")]
internal partial interface ICoreWebView2Environment7 : ICoreWebView2Environment6
{
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetUserDataFolder();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("E59EE362-ACBD-4857-9A8E-D3644D9459A9")]
internal partial interface ICoreWebView2Environment6 : ICoreWebView2Environment5
{
    ICoreWebView2PrintSettings CreatePrintSettings();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("319E423D-E0D7-4B8D-9254-AE9475DE9B17")]
internal partial interface ICoreWebView2Environment5 : ICoreWebView2Environment4
{
    void add_BrowserProcessExited(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_BrowserProcessExited(EventRegistrationToken token);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("20944379-6DCF-41D6-A0A0-ABC0FC50DE0D")]
internal partial interface ICoreWebView2Environment4 : ICoreWebView2Environment3
{
    IntPtr GetAutomationProviderForWindow(IntPtr hwnd);
}
