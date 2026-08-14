using System;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Avalonia.Input;

namespace Avalonia.Controls.Win;

[SupportedOSPlatform("windows6.1")]
internal static class WindowsUtility
{
    public static void MakeHwndTransparent(IntPtr hwnd)
    {
        var p = new HWND(hwnd);
        var exStyle = PInvoke.GetWindowLong(p, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        // Add WS_EX_TRANSPARENT (0x00000020L) to first intermediate window while removing WS_EX_LAYERED (0x00080000L)
        // This combination ensures:
        // 1. WS_EX_TRANSPARENT: Makes the window visually transparent but still blocks content from behind the application
        // 2. Removing WS_EX_LAYERED: Prevents the window from creating its own compositing surface with default black background
        PInvoke.SetWindowLong(p, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE,
            (exStyle | (int)0x00000020L) & ~(int)0x00080000L);
    }

    internal static StandardCursorType MapCursor(uint systemCursorId)
    {
        return systemCursorId switch
        {
            32512 => StandardCursorType.Arrow, // OCR_NORMAL
            32513 => StandardCursorType.Ibeam, // OCR_IBEAM
            32514 => StandardCursorType.Wait, // OCR_WAIT
            32515 => StandardCursorType.Cross, // OCR_CROSS
            32516 => StandardCursorType.UpArrow, // OCR_UP
            32642 => StandardCursorType.SizeAll, // OCR_SIZENWSE
            32643 => StandardCursorType.SizeAll, // OCR_SIZENESW,
            32644 => StandardCursorType.SizeWestEast, // OCR_SIZEWE
            32645 => StandardCursorType.SizeNorthSouth, // OCR_SIZENS
            32646 => StandardCursorType.SizeAll, // OCR_SIZEALL
            32648 => StandardCursorType.No, // OCR_NO
            32649 => StandardCursorType.Hand, // OCR_HAND
            32650 => StandardCursorType.AppStarting, // OCR_APPSTARTING
            32651 => StandardCursorType.Help, // OCR_HELP
            _ => StandardCursorType.Arrow,
        };
    }
}
