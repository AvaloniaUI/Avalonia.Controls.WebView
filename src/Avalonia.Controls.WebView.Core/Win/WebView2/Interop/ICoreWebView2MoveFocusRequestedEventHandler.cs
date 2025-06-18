using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

internal enum COREWEBVIEW2_MOVE_FOCUS_REASON
{
    COREWEBVIEW2_MOVE_FOCUS_REASON_PROGRAMMATIC,
    COREWEBVIEW2_MOVE_FOCUS_REASON_NEXT,
    COREWEBVIEW2_MOVE_FOCUS_REASON_PREVIOUS
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("69035451-6DC7-4CB8-9BCE-B2BD70AD289F")]
internal partial interface ICoreWebView2MoveFocusRequestedEventHandler
{
    void Invoke(ICoreWebView2Controller sender, ICoreWebView2MoveFocusRequestedEventArgs args);
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("2D6AA13B-3839-4A15-92FC-D88B3C0D9C9D")]
internal partial interface ICoreWebView2MoveFocusRequestedEventArgs
{
    COREWEBVIEW2_MOVE_FOCUS_REASON GetReason();
    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetHandled();
    void SetHandled([MarshalAs(UnmanagedType.Bool)] bool handled);
}
