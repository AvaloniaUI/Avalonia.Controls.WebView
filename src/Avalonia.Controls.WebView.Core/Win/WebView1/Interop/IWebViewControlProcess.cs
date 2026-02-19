using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.Interop;

namespace Avalonia.Controls.Win.WebView1.Interop;

[StructLayout(LayoutKind.Sequential)]
struct winrtRect
{
    public float X;
    public float Y;
    public float Width;
    public float Height;
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("02C723EC-98D6-424A-B63E-C6136C36A0F2")]
internal partial interface IWebViewControlProcess : IInspectable
{    uint GetProcessId();

    IntPtr GetEnterpriseId();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetIsPrivateNetworkClientServerCapabilityEnabled();

    IAsyncOperation_WebViewControl CreateWebViewControl(long hostWindowHandle, winrtRect bounds);

    void GetWebViewControls(out IntPtr result);

    [PreserveSig]
    int Terminate();

    void add_ProcessExited(IntPtr handler, out EventRegistrationToken token);

    void remove_ProcessExited(EventRegistrationToken token);
}
