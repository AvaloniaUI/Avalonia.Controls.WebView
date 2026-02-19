using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("9da43ccc-26e1-4dad-b56c-d8961c94c571")]
internal partial interface ICoreWebView2CursorChangedEventHandler
{
    void Invoke(ICoreWebView2CompositionController sender, IntPtr args);
}
