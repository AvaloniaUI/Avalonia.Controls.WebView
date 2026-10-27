#pragma warning disable CA1416 // Platform compatibility — the tested types are managed-only, no native calls happen in these tests

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Controls.Win.WebView2;
using Avalonia.Controls.Win.WebView2.Interop;
using Avalonia.Controls.Linux;
using Avalonia.Controls.Win;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

public class ProcessTerminatedTests : HeadlessTestsBase
{
    [Theory]
    [InlineData((int)COREWEBVIEW2_PROCESS_FAILED_KIND.COREWEBVIEW2_PROCESS_FAILED_KIND_BROWSER_PROCESS_EXITED, "BrowserProcessExited")]
    [InlineData((int)COREWEBVIEW2_PROCESS_FAILED_KIND.COREWEBVIEW2_PROCESS_FAILED_KIND_RENDER_PROCESS_EXITED, "RenderProcessExited")]
    public void WebView2Adapter_Raises_ProcessTerminated_With_Reason(int kind, string expectedReason)
    {
        var adapter = new TestableAdapter(new FakeController());
        var received = new List<WebViewProcessTerminatedEventArgs>();
        adapter.ProcessTerminated += (_, args) => received.Add(args);

        RaiseProcessFailed(adapter, (COREWEBVIEW2_PROCESS_FAILED_KIND)kind);

        var args = Assert.Single(received);
        Assert.Equal(expectedReason, args.Reason);
    }

    [Theory]
    [InlineData((int)COREWEBVIEW2_PROCESS_FAILED_KIND.COREWEBVIEW2_PROCESS_FAILED_KIND_FRAME_RENDER_PROCESS_EXITED)]
    [InlineData((int)COREWEBVIEW2_PROCESS_FAILED_KIND.COREWEBVIEW2_PROCESS_FAILED_KIND_RENDER_PROCESS_UNRESPONSIVE)]
    [InlineData((int)COREWEBVIEW2_PROCESS_FAILED_KIND.COREWEBVIEW2_PROCESS_FAILED_KIND_GPU_PROCESS_EXITED)]
    public void WebView2Adapter_Does_Not_Raise_ProcessTerminated_For_Frame_Only_Or_Recoverable_Failures(int kind)
    {
        var adapter = new TestableAdapter(new FakeController());
        var raised = false;
        adapter.ProcessTerminated += (_, _) => raised = true;

        RaiseProcessFailed(adapter, (COREWEBVIEW2_PROCESS_FAILED_KIND)kind);

        Assert.False(raised);
    }

    [Theory]
    [InlineData(0, "Crashed")]
    [InlineData(1, "ExceededMemoryLimit")]
    [InlineData(2, "TerminatedByApi")]
    [InlineData(42, "Unknown(42)")]
    public void WebKit_Process_Termination_Maps_Reason_Names(int reason, string expectedName)
    {
        Assert.Equal(expectedName, WebKitWebProcessTermination.GetReasonName(reason));
    }

    [Fact]
    public void WpeAdapter_Exposes_Process_Termination_Capability()
    {
        Assert.Contains(typeof(IWebViewAdapterWithProcessTermination), typeof(WpeWebViewAdapter).GetInterfaces());
    }

    [AvaloniaFact]
    public async System.Threading.Tasks.Task NativeWebView_ProcessTerminated_Can_Be_Subscribed_Before_Adapter_Creation()
    {
        var window = new Window();
        var webView = new NativeWebView();
        var raised = false;
        webView.ProcessTerminated += (_, _) => raised = true;
        window.Content = webView;
        window.Show();

        await WaitForAdapterCreation(webView);

        // The headless adapter doesn't terminate any process, so the event must stay silent.
        Assert.False(raised);
    }

    private static void RaiseProcessFailed(WebView2BaseAdapter adapter, COREWEBVIEW2_PROCESS_FAILED_KIND kind)
    {
        var callbacks = new WebViewCallbacks(new WeakReference<WebView2BaseAdapter>(adapter));
        ((ICoreWebView2ProcessFailedEventHandler)callbacks).Invoke(null!, new FakeProcessFailedEventArgs(kind));
    }

    private sealed class TestableAdapter(ICoreWebView2Controller controller) : WebView2BaseAdapter(controller)
    {
        public override IntPtr Handle => IntPtr.Zero;
        public override string? HandleDescriptor => null;
    }

    private sealed class FakeProcessFailedEventArgs(COREWEBVIEW2_PROCESS_FAILED_KIND kind) : ICoreWebView2ProcessFailedEventArgs
    {
        public COREWEBVIEW2_PROCESS_FAILED_KIND GetProcessFailedKind() => kind;
    }

    private sealed class FakeController : ICoreWebView2Controller
    {
        int ICoreWebView2Controller.GetIsVisible() => throw new NotImplementedException();
        void ICoreWebView2Controller.SetIsVisible(int value) => throw new NotImplementedException();
        tagRECT ICoreWebView2Controller.GetBounds() => throw new NotImplementedException();
        void ICoreWebView2Controller.SetBounds(tagRECT value) => throw new NotImplementedException();
        double ICoreWebView2Controller.GetZoomFactor() => throw new NotImplementedException();
        void ICoreWebView2Controller.SetZoomFactor(double value) => throw new NotImplementedException();
        void ICoreWebView2Controller.add_ZoomFactorChanged(IntPtr eventHandler, out EventRegistrationToken token) => throw new NotImplementedException();
        void ICoreWebView2Controller.remove_ZoomFactorChanged(EventRegistrationToken token) => throw new NotImplementedException();
        void ICoreWebView2Controller.SetBoundsAndZoomFactor(tagRECT bounds, double zoomFactor) => throw new NotImplementedException();
        void ICoreWebView2Controller.MoveFocus(COREWEBVIEW2_MOVE_FOCUS_REASON reason) => throw new NotImplementedException();
        void ICoreWebView2Controller.add_MoveFocusRequested(ICoreWebView2MoveFocusRequestedEventHandler eventHandler, out EventRegistrationToken token) => throw new NotImplementedException();
        void ICoreWebView2Controller.remove_MoveFocusRequested(EventRegistrationToken token) => throw new NotImplementedException();
        void ICoreWebView2Controller.add_GotFocus(ICoreWebView2FocusChangedEventHandler eventHandler, out EventRegistrationToken token) => throw new NotImplementedException();
        void ICoreWebView2Controller.remove_GotFocus(EventRegistrationToken token) => throw new NotImplementedException();
        void ICoreWebView2Controller.add_LostFocus(ICoreWebView2FocusChangedEventHandler eventHandler, out EventRegistrationToken token) => throw new NotImplementedException();
        void ICoreWebView2Controller.remove_LostFocus(EventRegistrationToken token) => throw new NotImplementedException();
        void ICoreWebView2Controller.add_AcceleratorKeyPressed(IntPtr eventHandler, out EventRegistrationToken token) => throw new NotImplementedException();
        void ICoreWebView2Controller.remove_AcceleratorKeyPressed(EventRegistrationToken token) => throw new NotImplementedException();
        IntPtr ICoreWebView2Controller.GetParentWindow() => throw new NotImplementedException();
        void ICoreWebView2Controller.SetParentWindow(IntPtr value) => throw new NotImplementedException();
        void ICoreWebView2Controller.NotifyParentWindowPositionChanged() => throw new NotImplementedException();
        void ICoreWebView2Controller.Close() { }
        ICoreWebView2 ICoreWebView2Controller.GetCoreWebView2() => throw new NotImplementedException();
    }
}
