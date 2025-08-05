using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("9E8F0CF8-E670-4B5E-B2BC-73E061E3184C")]
internal partial interface ICoreWebView2_2 : ICoreWebView2
{
#if !COM_SOURCE_GEN
    void _VtblGap1_58();
#endif

    void add_WebResourceResponseReceived(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_WebResourceResponseReceived(EventRegistrationToken token);

    void NavigateWithWebResourceRequest(IntPtr Request);

    void add_DOMContentLoaded(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_DOMContentLoaded(EventRegistrationToken token);

    ICoreWebView2CookieManager GetCookieManager();

    ICoreWebView2Environment Environment();
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("A0D6DF20-3B92-416D-AA0C-437A9C727857")]
internal partial interface ICoreWebView2_3 : ICoreWebView2_2
{
#if !COM_SOURCE_GEN
    void _VtblGap1_65();
#endif

    void TrySuspend(IntPtr handler);
    void Resume();
    
    int get_IsSuspended();

    void SetVirtualHostNameToFolderMapping([MarshalAs(UnmanagedType.LPWStr)] string hostName, [MarshalAs(UnmanagedType.LPWStr)] string folderPath, int accessKind);
    void ClearVirtualHostNameToFolderMapping([MarshalAs(UnmanagedType.LPWStr)] string hostName);
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("20D02D59-6DF2-42DC-BD06-F98A694B1302")]
internal partial interface ICoreWebView2_4 : ICoreWebView2_3
{
#if !COM_SOURCE_GEN
    void _VtblGap1_70();
#endif

    void add_FrameCreated(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_FrameCreated(EventRegistrationToken token);

    void add_DownloadStarting(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_DownloadStarting(EventRegistrationToken token);
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("BEDB11B8-D63C-11EB-B8BC-0242AC130003")]
internal partial interface ICoreWebView2_5 : ICoreWebView2_4
{
#if !COM_SOURCE_GEN
    void _VtblGap1_74();
#endif

    void add_ClientCertificateRequested(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_ClientCertificateRequested(EventRegistrationToken token);
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("499AADAC-D92C-4589-8A75-111BFC167795")]
internal partial interface ICoreWebView2_6 : ICoreWebView2_5
{
#if !COM_SOURCE_GEN
    void _VtblGap1_76();
#endif

    void OpenTaskManagerWindow();
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("79C24D83-09A3-45AE-9418-487F32A58740")]
internal partial interface ICoreWebView2_7 : ICoreWebView2_6
{
#if !COM_SOURCE_GEN
    void _VtblGap1_77();
#endif

    void PrintToPdf([MarshalAs(UnmanagedType.LPWStr)] string ResultFilePath, IntPtr printSettings, IntPtr handler);
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("E9632730-6E1E-43AB-B7B8-7B2C9E62E094")]
internal partial interface ICoreWebView2_8 : ICoreWebView2_7
{
#if !COM_SOURCE_GEN
    void _VtblGap1_78();
#endif

    void add_IsMutedChanged(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_IsMutedChanged(EventRegistrationToken token);

    int get_IsMuted();
    void put_IsMuted(int value);

    void add_IsDocumentPlayingAudioChanged(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_IsDocumentPlayingAudioChanged(EventRegistrationToken token);

    int get_IsDocumentPlayingAudio();
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("4D7B2EAB-9FDC-468D-B998-A9260B5ED651")]
internal partial interface ICoreWebView2_9 : ICoreWebView2_8
{
#if !COM_SOURCE_GEN
    void _VtblGap1_85();
#endif

    void add_IsDefaultDownloadDialogOpenChanged(IntPtr handler, out EventRegistrationToken token);
    void remove_IsDefaultDownloadDialogOpenChanged(EventRegistrationToken token);

    int get_IsDefaultDownloadDialogOpen();

    void OpenDefaultDownloadDialog();
    void CloseDefaultDownloadDialog();

    int get_DefaultDownloadDialogCornerAlignment();
    void put_DefaultDownloadDialogCornerAlignment(int value);

    tagPOINT get_DefaultDownloadDialogMargin();
    void put_DefaultDownloadDialogMargin(tagPOINT value);
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("B1690564-6F5A-4983-8E48-31D1143FECDB")]
internal partial interface ICoreWebView2_10 : ICoreWebView2_9
{
#if !COM_SOURCE_GEN
    void _VtblGap1_94();
#endif

    void add_BasicAuthenticationRequested(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_BasicAuthenticationRequested(EventRegistrationToken token);
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("0BE78E56-C193-4051-B943-23B460C08BDB")]
internal partial interface ICoreWebView2_11 : ICoreWebView2_10
{
#if !COM_SOURCE_GEN
    void _VtblGap1_96();
#endif

    void CallDevToolsProtocolMethodForSession([MarshalAs(UnmanagedType.LPWStr)] string sessionId, [MarshalAs(UnmanagedType.LPWStr)] string methodName, [MarshalAs(UnmanagedType.LPWStr)] string parametersAsJson, IntPtr handler);

    void add_ContextMenuRequested(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_ContextMenuRequested(EventRegistrationToken token);
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("35D69927-BCFA-4566-9349-6B3E0D154CAC")]
internal partial interface ICoreWebView2_12 : ICoreWebView2_11
{
#if !COM_SOURCE_GEN
    void _VtblGap1_99();
#endif

    void add_StatusBarTextChanged(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_StatusBarTextChanged(EventRegistrationToken token);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string get_StatusBarText();
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("F75F09A8-667E-4983-88D6-C8773F315E84")]
internal partial interface ICoreWebView2_13 : ICoreWebView2_12
{
#if !COM_SOURCE_GEN
    void _VtblGap1_102();
#endif

    [return: MarshalAs(UnmanagedType.Interface)]
    IntPtr get_Profile();
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("6DAA4F10-4A90-4753-8898-77C5DF534165")]
internal partial interface ICoreWebView2_14 : ICoreWebView2_13
{
#if !COM_SOURCE_GEN
    void _VtblGap1_103();
#endif

    void add_ServerCertificateErrorDetected(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_ServerCertificateErrorDetected(EventRegistrationToken token);

    void ClearServerCertificateErrorActions(IntPtr handler);
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("517B2D1D-7DAE-4A66-A4F4-10352FFB9518")]
internal partial interface ICoreWebView2_15 : ICoreWebView2_14
{
#if !COM_SOURCE_GEN
    void _VtblGap1_106();
#endif

    void add_FaviconChanged(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_FaviconChanged(EventRegistrationToken token);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string get_FaviconUri();

    void GetFavicon(int format, IntPtr completedHandler);
}

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("0EB34DC9-9F91-41E1-8639-95CD5943906B")]
internal partial interface ICoreWebView2_16 : ICoreWebView2_15
{
#if !COM_SOURCE_GEN
    void _VtblGap1_110();
#endif

    void Print(IntPtr printSettings, IntPtr handler);
    void ShowPrintUI(int printDialogKind);
    void PrintToPdfStream(ICoreWebView2PrintSettings? printSettings, ICoreWebView2PrintToPdfStreamCompletedHandler handler);
}
