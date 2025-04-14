using System;

namespace Avalonia.Controls.Macios.Interop.WebKit;

internal class WKPreferences(IntPtr handle) : NSObject(handle, false)
{
    private static readonly NSString s_developerExtrasEnabledKey = NSString.Create("developerExtrasEnabled");
    private static readonly NSString s_mediaDevicesEnabledKey = NSString.Create("mediaDevicesEnabled");

    public bool DeveloperExtrasEnabled
    {
        get => ValueForKey(s_developerExtrasEnabledKey) == NSNumber.Yes.Handle;
        set => SetValueForKey(value ? NSNumber.Yes.Handle : NSNumber.No.Handle, s_developerExtrasEnabledKey);
    }

    public bool MediaDevicesEnabled
    {
        get => ValueForKey(s_mediaDevicesEnabledKey) == NSNumber.Yes.Handle;
        set => SetValueForKey(value ? NSNumber.Yes.Handle : NSNumber.No.Handle, s_mediaDevicesEnabledKey);
    }
}
