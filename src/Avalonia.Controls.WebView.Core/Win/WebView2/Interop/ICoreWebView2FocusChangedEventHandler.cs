using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("05EA24BD-6452-4926-9014-4B82B498135D")]
internal partial interface ICoreWebView2FocusChangedEventHandler
{
    void Invoke(ICoreWebView2Controller sender, IntPtr args);
}

