using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.Interop;

// ReSharper disable InconsistentNaming

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
    ICompositionVisual? GetRootVisualTarget();
    void SetRootVisualTarget(ICompositionVisual? value);

    void SendMouseInput(COREWEBVIEW2_MOUSE_EVENT_KIND eventKind, COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS virtualKeys, int mouseData, tagPOINT point);

    void SendPointerInput(int eventKind, [MarshalAs(UnmanagedType.Interface)] IntPtr pointerInfo);

    IntPtr GetCursor();

    uint GetSystemCursorId();

    void add_CursorChanged(ICoreWebView2CursorChangedEventHandler eventHandler, out EventRegistrationToken token);

    void remove_CursorChanged(EventRegistrationToken token);
}

public enum COREWEBVIEW2_MOUSE_EVENT_KIND
{
    COREWEBVIEW2_MOUSE_EVENT_KIND_MOVE = 512, // 0x00000200
    COREWEBVIEW2_MOUSE_EVENT_KIND_LEFT_BUTTON_DOWN = 513, // 0x00000201
    COREWEBVIEW2_MOUSE_EVENT_KIND_LEFT_BUTTON_UP = 514, // 0x00000202
    COREWEBVIEW2_MOUSE_EVENT_KIND_LEFT_BUTTON_DOUBLE_CLICK = 515, // 0x00000203
    COREWEBVIEW2_MOUSE_EVENT_KIND_RIGHT_BUTTON_DOWN = 516, // 0x00000204
    COREWEBVIEW2_MOUSE_EVENT_KIND_RIGHT_BUTTON_UP = 517, // 0x00000205
    COREWEBVIEW2_MOUSE_EVENT_KIND_RIGHT_BUTTON_DOUBLE_CLICK = 518, // 0x00000206
    COREWEBVIEW2_MOUSE_EVENT_KIND_MIDDLE_BUTTON_DOWN = 519, // 0x00000207
    COREWEBVIEW2_MOUSE_EVENT_KIND_MIDDLE_BUTTON_UP = 520, // 0x00000208
    COREWEBVIEW2_MOUSE_EVENT_KIND_MIDDLE_BUTTON_DOUBLE_CLICK = 521, // 0x00000209
    COREWEBVIEW2_MOUSE_EVENT_KIND_WHEEL = 522, // 0x0000020A
    COREWEBVIEW2_MOUSE_EVENT_KIND_X_BUTTON_DOWN = 523, // 0x0000020B
    COREWEBVIEW2_MOUSE_EVENT_KIND_X_BUTTON_UP = 524, // 0x0000020C
    COREWEBVIEW2_MOUSE_EVENT_KIND_X_BUTTON_DOUBLE_CLICK = 525, // 0x0000020D
    COREWEBVIEW2_MOUSE_EVENT_KIND_HORIZONTAL_WHEEL = 526, // 0x0000020E
    COREWEBVIEW2_MOUSE_EVENT_KIND_LEAVE = 675, // 0x000002A3
}

[Flags]
public enum COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS
{
    COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS_NONE = 0,
    COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS_LEFT_BUTTON = 1,
    COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS_RIGHT_BUTTON = 2,
    COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS_SHIFT = 4,
    COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS_CONTROL = 8,
    COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS_MIDDLE_BUTTON = 16, // 0x00000010
    COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS_X_BUTTON1 = 32, // 0x00000020
    COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS_X_BUTTON2 = 64, // 0x00000040
}
