using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.Interop;

namespace Avalonia.Controls.Win.WebView1.Interop;

[StructLayout(LayoutKind.Sequential)]
struct winrtColor
{
    public byte A;
    public byte R;
    public byte G;
    public byte B;
};

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("3F921316-BC70-4BDA-9136-C94370899FAB")]
internal partial interface IWebViewControl : IInspectable
{    IUriRuntimeClass get_Source();

    void put_Source(IUriRuntimeClass source);

    IntPtr get_DocumentTitle();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool get_CanGoBack();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool get_CanGoForward();

    void put_DefaultBackgroundColor(winrtColor value);

    winrtColor get_DefaultBackgroundColor();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool get_ContainsFullScreenElement();

    IWebViewControlSettings get_Settings();

    IntPtr get_DeferredPermissionRequests();

    void GoForward();

    void GoBack();

    void Refresh();

    void Stop();

    void Navigate(IntPtr source);

    void NavigateToString(IntPtr text);

    void NavigateToLocalStreamUri(IntPtr source, IntPtr streamResolver);

    void NavigateWithHttpRequestMessage(IntPtr requestMessage);

    IAsyncOperation_HString InvokeScriptAsync(IntPtr scriptName, IIterable arguments);

    void CapturePreviewToStreamAsync(IntPtr stream, out IntPtr operation);

    void CaptureSelectedContentToDataPackageAsync(out IntPtr operation);

    IntPtr BuildLocalStreamUri(IntPtr contentIdentifier, IntPtr relativePath);

    void GetDeferredPermissionRequestById(uint id, out IntPtr result);

    void add_NavigationStarting(IWebViewControlNavigationStartingHandler handler, out EventRegistrationToken token);

    void remove_NavigationStarting(EventRegistrationToken token);

    void add_ContentLoading(IntPtr handler, out EventRegistrationToken token);

    void remove_ContentLoading(EventRegistrationToken token);

    void add_DOMContentLoaded(IntPtr handler, out EventRegistrationToken token);

    void remove_DOMContentLoaded(EventRegistrationToken token);

    void add_NavigationCompleted(IWebViewControlNavigationCompletedHandler handler, out EventRegistrationToken token);

    void remove_NavigationCompleted(EventRegistrationToken token);

    void add_FrameNavigationStarting(IntPtr handler, out EventRegistrationToken token);

    void remove_FrameNavigationStarting(EventRegistrationToken token);

    void add_FrameContentLoading(IntPtr handler, out EventRegistrationToken token);

    void remove_FrameContentLoading(EventRegistrationToken token);

    void add_FrameDOMContentLoaded(IntPtr handler, out EventRegistrationToken token);

    void remove_FrameDOMContentLoaded(EventRegistrationToken token);

    void add_FrameNavigationCompleted(IntPtr handler, out EventRegistrationToken token);

    void remove_FrameNavigationCompleted(EventRegistrationToken token);

    void add_ScriptNotify(IWebViewControlScriptNotifyHandler handler, out EventRegistrationToken token);

    void remove_ScriptNotify(EventRegistrationToken token);

    void add_LongRunningScriptDetected(IntPtr handler, out EventRegistrationToken token);

    void remove_LongRunningScriptDetected(EventRegistrationToken token);

    void add_UnsafeContentWarningDisplaying(IntPtr handler, out EventRegistrationToken token);

    void remove_UnsafeContentWarningDisplaying(EventRegistrationToken token);

    void add_UnviewableContentIdentified(IntPtr handler, out EventRegistrationToken token);

    void remove_UnviewableContentIdentified(EventRegistrationToken token);

    void add_PermissionRequested(IntPtr handler, out EventRegistrationToken token);

    void remove_PermissionRequested(EventRegistrationToken token);

    void add_UnsupportedUriSchemeIdentified(IntPtr handler, out EventRegistrationToken token);

    void remove_UnsupportedUriSchemeIdentified(EventRegistrationToken token);

    void add_NewWindowRequested(IWebViewControlNewWindowRequestedHandler handler, out EventRegistrationToken token);

    void remove_NewWindowRequested(EventRegistrationToken token);

    void add_ContainsFullScreenElementChanged(IntPtr handler, out EventRegistrationToken token);

    void remove_ContainsFullScreenElementChanged(EventRegistrationToken token);

    void add_WebResourceRequested(IWebViewControlWebResourceRequestedHandler handler, out EventRegistrationToken token);

    void remove_WebResourceRequested(EventRegistrationToken token);
}
