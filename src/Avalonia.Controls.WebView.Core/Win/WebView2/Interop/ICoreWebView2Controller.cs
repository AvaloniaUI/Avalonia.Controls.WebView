using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
struct tagRECT
{
    public int left;
    public int top;
    public int right;
    public int bottom;
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("4D00C0D1-9434-4EB6-8078-8697A560334F")]
internal partial interface ICoreWebView2Controller
{
    int GetIsVisible();
    void SetIsVisible(int value);

    tagRECT GetBounds();
    void SetBounds(tagRECT value);

    double GetZoomFactor();
    void SetZoomFactor(double value);

    void add_ZoomFactorChanged([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);
    void remove_ZoomFactorChanged(EventRegistrationToken token);

    void SetBoundsAndZoomFactor(tagRECT Bounds, double ZoomFactor);
    void MoveFocus(COREWEBVIEW2_MOVE_FOCUS_REASON reason);

    void add_MoveFocusRequested([MarshalAs(UnmanagedType.Interface)] ICoreWebView2MoveFocusRequestedEventHandler eventHandler, out EventRegistrationToken token);
    void remove_MoveFocusRequested(EventRegistrationToken token);

    void add_GotFocus([MarshalAs(UnmanagedType.Interface)] ICoreWebView2FocusChangedEventHandler eventHandler, out EventRegistrationToken token);
    void remove_GotFocus(EventRegistrationToken token);

    void add_LostFocus([MarshalAs(UnmanagedType.Interface)] ICoreWebView2FocusChangedEventHandler eventHandler, out EventRegistrationToken token);
    void remove_LostFocus(EventRegistrationToken token);

    void add_AcceleratorKeyPressed([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);
    void remove_AcceleratorKeyPressed(EventRegistrationToken token);

    IntPtr GetParentWindow();
    void SetParentWindow(IntPtr value);

    void NotifyParentWindowPositionChanged();
    void Close();

    ICoreWebView2 GetCoreWebView2();
}

