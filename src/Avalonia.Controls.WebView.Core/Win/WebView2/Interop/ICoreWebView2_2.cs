using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("9E8F0CF8-E670-4B5E-B2BC-73E061E3184C")]
internal partial interface ICoreWebView2_2 : ICoreWebView2
{
    void add_WebResourceResponseReceived(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_WebResourceResponseReceived(EventRegistrationToken token);

    void NavigateWithWebResourceRequest(IntPtr Request);

    void add_DOMContentLoaded(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_DOMContentLoaded(EventRegistrationToken token);

    ICoreWebView2CookieManager GetCookieManager();

    ICoreWebView2Environment Environment();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("A0D6DF20-3B92-416D-AA0C-437A9C727857")]
internal partial interface ICoreWebView2_3 : ICoreWebView2_2
{
    void TrySuspend(IntPtr handler);
    void Resume();
    
    int get_IsSuspended();

    void SetVirtualHostNameToFolderMapping([MarshalAs(UnmanagedType.LPWStr)] string hostName, [MarshalAs(UnmanagedType.LPWStr)] string folderPath, int accessKind);
    void ClearVirtualHostNameToFolderMapping([MarshalAs(UnmanagedType.LPWStr)] string hostName);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("20D02D59-6DF2-42DC-BD06-F98A694B1302")]
internal partial interface ICoreWebView2_4 : ICoreWebView2_3
{
    void add_FrameCreated(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_FrameCreated(EventRegistrationToken token);

    void add_DownloadStarting(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_DownloadStarting(EventRegistrationToken token);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("BEDB11B8-D63C-11EB-B8BC-0242AC130003")]
internal partial interface ICoreWebView2_5 : ICoreWebView2_4
{
    void add_ClientCertificateRequested(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_ClientCertificateRequested(EventRegistrationToken token);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("499AADAC-D92C-4589-8A75-111BFC167795")]
internal partial interface ICoreWebView2_6 : ICoreWebView2_5
{
    void OpenTaskManagerWindow();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("79C24D83-09A3-45AE-9418-487F32A58740")]
internal partial interface ICoreWebView2_7 : ICoreWebView2_6
{
    void PrintToPdf([MarshalAs(UnmanagedType.LPWStr)] string ResultFilePath, IntPtr printSettings, IntPtr handler);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("E9632730-6E1E-43AB-B7B8-7B2C9E62E094")]
internal partial interface ICoreWebView2_8 : ICoreWebView2_7
{
    void add_IsMutedChanged(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_IsMutedChanged(EventRegistrationToken token);

    int get_IsMuted();
    void put_IsMuted(int value);

    void add_IsDocumentPlayingAudioChanged(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_IsDocumentPlayingAudioChanged(EventRegistrationToken token);

    int get_IsDocumentPlayingAudio();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("4D7B2EAB-9FDC-468D-B998-A9260B5ED651")]
internal partial interface ICoreWebView2_9 : ICoreWebView2_8
{
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

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("B1690564-6F5A-4983-8E48-31D1143FECDB")]
internal partial interface ICoreWebView2_10 : ICoreWebView2_9
{
    void add_BasicAuthenticationRequested(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_BasicAuthenticationRequested(EventRegistrationToken token);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("0BE78E56-C193-4051-B943-23B460C08BDB")]
internal partial interface ICoreWebView2_11 : ICoreWebView2_10
{
    void CallDevToolsProtocolMethodForSession([MarshalAs(UnmanagedType.LPWStr)] string sessionId, [MarshalAs(UnmanagedType.LPWStr)] string methodName, [MarshalAs(UnmanagedType.LPWStr)] string parametersAsJson, IntPtr handler);

    void add_ContextMenuRequested(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_ContextMenuRequested(EventRegistrationToken token);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("35D69927-BCFA-4566-9349-6B3E0D154CAC")]
internal partial interface ICoreWebView2_12 : ICoreWebView2_11
{
    void add_StatusBarTextChanged(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_StatusBarTextChanged(EventRegistrationToken token);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string get_StatusBarText();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("F75F09A8-667E-4983-88D6-C8773F315E84")]
internal partial interface ICoreWebView2_13 : ICoreWebView2_12
{
    [return: MarshalAs(UnmanagedType.Interface)]
    IntPtr get_Profile();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("6DAA4F10-4A90-4753-8898-77C5DF534165")]
internal partial interface ICoreWebView2_14 : ICoreWebView2_13
{
    void add_ServerCertificateErrorDetected(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_ServerCertificateErrorDetected(EventRegistrationToken token);

    void ClearServerCertificateErrorActions(IntPtr handler);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("517B2D1D-7DAE-4A66-A4F4-10352FFB9518")]
internal partial interface ICoreWebView2_15 : ICoreWebView2_14
{
    void add_FaviconChanged(IntPtr eventHandler, out EventRegistrationToken token);
    void remove_FaviconChanged(EventRegistrationToken token);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string get_FaviconUri();

    void GetFavicon(int format, IntPtr completedHandler);
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("0EB34DC9-9F91-41E1-8639-95CD5943906B")]
internal partial interface ICoreWebView2_16 : ICoreWebView2_15
{
    void Print(IntPtr printSettings, IntPtr handler);
    void ShowPrintUI(int printDialogKind);
    void PrintToPdfStream(ICoreWebView2PrintSettings? printSettings, ICoreWebView2PrintToPdfStreamCompletedHandler handler);
}
