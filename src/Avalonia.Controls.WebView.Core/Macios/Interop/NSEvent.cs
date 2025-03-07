using System;

namespace Avalonia.Controls.Macios.Interop;

internal class NSEvent : NSObject
{
    private static readonly IntPtr s_charactersIgnoringModifiers = Libobjc.sel_getUid("charactersIgnoringModifiers");
    private static readonly IntPtr s_keyCode = Libobjc.sel_getUid("keyCode");
    private static readonly IntPtr s_modifierFlags = Libobjc.sel_getUid("modifierFlags");
    private static readonly IntPtr s_type = Libobjc.sel_getUid("type");

    public NSEvent(IntPtr handle, bool owns) : base(handle, owns)
    {
    }

    public string? CharactersIgnoringModifiers =>
        NSString.GetString(Libobjc.intptr_objc_msgSend(Handle, s_charactersIgnoringModifiers));
    public int KeyCode => Libobjc.int_objc_msgSend(Handle, s_keyCode); // ushort?
    public NSEventModifierMask ModifierFlags => (NSEventModifierMask)Libobjc.int_objc_msgSend(Handle, s_modifierFlags);
    public int Type => Libobjc.int_objc_msgSend(Handle, s_type);

    [Flags]
    public enum NSEventModifierMask : uint
    {
        AlphaShiftKeyMask = 65536,
        AlternateKeyMask = 524288,
        CommandKeyMask = 1048576,
        ControlKeyMask = 262144,
        DeviceIndependentModifierFlagsMask = 4294901760,
        FunctionKeyMask = 8388608,
        HelpKeyMask = 4194304,
        NumericPadKeyMask = 2097152,
        ShiftKeyMask = 131072,
    }
}
