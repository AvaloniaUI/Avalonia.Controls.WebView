using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.Interop;

namespace Avalonia.Controls.Win.WebView1.Interop;

internal enum WebViewControlProcessCapabilityState
{
    Default  = 0,
    Disabled = 1,
    Enabled  = 2
};

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("1CCA72A7-3BD6-4826-8261-6C8189505D89")]
internal partial interface IWebViewControlProcessOptions : IInspectable
{    void put_EnterpriseId(IntPtr value);

    IntPtr get_EnterpriseId();
    
    void put_PrivateNetworkClientServerCapability(WebViewControlProcessCapabilityState value);
    
    WebViewControlProcessCapabilityState get_PrivateNetworkClientServerCapability();
}
