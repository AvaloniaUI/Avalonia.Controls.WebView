using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

#if COM_SOURCE_GEN
[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
#else
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("76ECEACB-0462-4D94-AC83-423A6793775E")]
internal partial interface ICoreWebView2
{
    [return: MarshalAs(UnmanagedType.Interface)]
    IntPtr GetSettings();

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetSource();

    void Navigate([MarshalAs(UnmanagedType.LPWStr)] string uri);

    void NavigateToString([MarshalAs(UnmanagedType.LPWStr)] string htmlContent);

    void add_NavigationStarting([MarshalAs(UnmanagedType.Interface)] ICoreWebView2NavigationStartingEventHandler eventHandler, out EventRegistrationToken token);
    void remove_NavigationStarting(EventRegistrationToken token);

    void add_ContentLoading([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);
    void remove_ContentLoading(EventRegistrationToken token);

    void add_SourceChanged([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);
    void remove_SourceChanged(EventRegistrationToken token);

    void add_HistoryChanged([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);
    void remove_HistoryChanged(EventRegistrationToken token);

    void add_NavigationCompleted([MarshalAs(UnmanagedType.Interface)] ICoreWebView2NavigationCompletedEventHandler eventHandler, out EventRegistrationToken token);
    void remove_NavigationCompleted(EventRegistrationToken token);

    void add_FrameNavigationStarting([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);
    void remove_FrameNavigationStarting(EventRegistrationToken token);

    void add_FrameNavigationCompleted([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);
    void remove_FrameNavigationCompleted(EventRegistrationToken token);

    void add_ScriptDialogOpening([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);
    void remove_ScriptDialogOpening(EventRegistrationToken token);

    void add_PermissionRequested([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);
    void remove_PermissionRequested(EventRegistrationToken token);

    void add_ProcessFailed([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);
    void remove_ProcessFailed(EventRegistrationToken token);

    void AddScriptToExecuteOnDocumentCreated([MarshalAs(UnmanagedType.LPWStr)] string javaScript, [MarshalAs(UnmanagedType.Interface)] ICoreWebView2AddScriptToExecuteOnDocumentCreatedCompletedHandler handler);
    void RemoveScriptToExecuteOnDocumentCreated([MarshalAs(UnmanagedType.LPWStr)] string id);

    void ExecuteScript([MarshalAs(UnmanagedType.LPWStr)] string javaScript, [MarshalAs(UnmanagedType.Interface)] ICoreWebView2ExecuteScriptCompletedHandler handler);

    void CapturePreview(int imageFormat, [MarshalAs(UnmanagedType.Interface)] IStream imageStream, [MarshalAs(UnmanagedType.Interface)] IntPtr handler);

    void Reload();

    void PostWebMessageAsJson([MarshalAs(UnmanagedType.LPWStr)] string webMessageAsJson);

    void PostWebMessageAsString([MarshalAs(UnmanagedType.LPWStr)] string webMessageAsString);

    void add_WebMessageReceived([MarshalAs(UnmanagedType.Interface)] ICoreWebView2WebMessageReceivedEventHandler handler, out EventRegistrationToken token);
    void remove_WebMessageReceived(EventRegistrationToken token);

    void CallDevToolsProtocolMethod([MarshalAs(UnmanagedType.LPWStr)] string methodName, [MarshalAs(UnmanagedType.LPWStr)] string parametersAsJson, [MarshalAs(UnmanagedType.Interface)] IntPtr handler);

    uint GetBrowserProcessId();

    int GetCanGoBack();

    int GetCanGoForward();

    void GoBack();

    void GoForward();

    [return: MarshalAs(UnmanagedType.Interface)]
    IntPtr GetDevToolsProtocolEventReceiver([MarshalAs(UnmanagedType.LPWStr)] string eventName);

    void Stop();

    void add_NewWindowRequested([MarshalAs(UnmanagedType.Interface)] ICoreWebView2NewWindowRequestedEventHandler eventHandler, out EventRegistrationToken token);
    void remove_NewWindowRequested(EventRegistrationToken token);

    void add_DocumentTitleChanged([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);
    void remove_DocumentTitleChanged(EventRegistrationToken token);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetDocumentTitle();

    void AddHostObjectToScript([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr @object);
    void RemoveHostObjectFromScript([MarshalAs(UnmanagedType.LPWStr)] string name);

    void OpenDevToolsWindow();

    void add_ContainsFullScreenElementChanged([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);
    void remove_ContainsFullScreenElementChanged(EventRegistrationToken token);

    int GetContainsFullScreenElement();

    void add_WebResourceRequested([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);
    void remove_WebResourceRequested(EventRegistrationToken token);

    void AddWebResourceRequestedFilter([MarshalAs(UnmanagedType.LPWStr)] string uri, int ResourceContext);
    void RemoveWebResourceRequestedFilter([MarshalAs(UnmanagedType.LPWStr)] string uri, int ResourceContext);

    void add_WindowCloseRequested([MarshalAs(UnmanagedType.Interface)] IntPtr eventHandler, out EventRegistrationToken token);
    void remove_WindowCloseRequested(EventRegistrationToken token);
}
