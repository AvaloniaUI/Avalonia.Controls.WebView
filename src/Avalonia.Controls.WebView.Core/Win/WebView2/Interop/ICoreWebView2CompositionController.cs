using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct tagPOINT
{
    public int x;
    public int y;
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("3DF9B733-B9AE-4A15-86B4-EB9EE9826469")]
internal partial interface ICoreWebView2CompositionController
{
    IntPtr GetRootVisualTarget();
    
    void SetRootVisualTarget(IntPtr value);

    void SendMouseInput(int eventKind, int virtualKeys, uint mouseData, tagPOINT point);

    void SendPointerInput(int eventKind, [MarshalAs(UnmanagedType.Interface)] IntPtr pointerInfo);

    IntPtr GetCursor();

    uint GetSystemCursorId();

    void add_CursorChanged([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);

    void remove_CursorChanged(EventRegistrationToken token);
}
