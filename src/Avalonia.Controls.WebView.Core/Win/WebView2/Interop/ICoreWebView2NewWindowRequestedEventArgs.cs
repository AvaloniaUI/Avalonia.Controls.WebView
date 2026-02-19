using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("34ACB11C-FC37-4418-9132-F9C21D1EAFB9")]
internal partial interface ICoreWebView2NewWindowRequestedEventArgs
{
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetUri();

    void SetNewWindow(ICoreWebView2 newWindow);
    ICoreWebView2 GetNewWindow();

    void SetHandled(int handled);
    int GetHandled();

    int GetIsUserInitiated();

    IntPtr GetDeferral();

    IntPtr GetWindowFeatures();
}
