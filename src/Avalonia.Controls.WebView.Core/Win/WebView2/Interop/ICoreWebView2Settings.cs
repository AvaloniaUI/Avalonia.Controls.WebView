using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("E562E4F0-D7FA-43AC-8D71-C05150499F00")]
internal partial  interface ICoreWebView2Settings
{
    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetIsScriptEnabled();
    void SetIsScriptEnabled([MarshalAs(UnmanagedType.Bool)] bool value);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool IsWebMessageEnabled();
    void SetIsWebMessageEnabled([MarshalAs(UnmanagedType.Bool)] bool value);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool AreDefaultScriptDialogsEnabled();
    void SetAreDefaultScriptDialogsEnabled([MarshalAs(UnmanagedType.Bool)] bool value);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool IsStatusBarEnabled();
    void SetIsStatusBarEnabled([MarshalAs(UnmanagedType.Bool)] bool value);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool AreDevToolsEnabled();
    void SetAreDevToolsEnabled([MarshalAs(UnmanagedType.Bool)] bool value);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool AreDefaultContextMenusEnabled();
    void SetAreDefaultContextMenusEnabled([MarshalAs(UnmanagedType.Bool)] bool value);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool AreHostObjectsAllowed();
    void SetAreHostObjectsAllowed([MarshalAs(UnmanagedType.Bool)] bool value);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool IsZoomControlEnabled();
    void SetIsZoomControlEnabled([MarshalAs(UnmanagedType.Bool)] bool value);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool IsBuiltInErrorPageEnabled();
    void SetIsBuiltInErrorPageEnabled([MarshalAs(UnmanagedType.Bool)] bool value);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("EE9A0F68-F46C-4E32-AC23-EF8CAC224D2A")]
internal partial interface ICoreWebView2Settings2 : ICoreWebView2Settings
{
    [return: MarshalAs(UnmanagedType.LPWStr)]
    string UserAgent();
    void SetUserAgent([MarshalAs(UnmanagedType.LPWStr)] string value);
}
