using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("AC52D13F-0D38-475A-9DCA-876580D6793E")]
internal partial interface ICoreWebView2EnvironmentOptions4
{
    /// <remarks>
    /// The callee allocates the array with CoTaskMemAlloc and adds a reference to every registration.
    /// </remarks>
    void GetCustomSchemeRegistrations(out uint count, out IntPtr schemeRegistrations);

    void SetCustomSchemeRegistrations(uint count, IntPtr schemeRegistrations);
}
