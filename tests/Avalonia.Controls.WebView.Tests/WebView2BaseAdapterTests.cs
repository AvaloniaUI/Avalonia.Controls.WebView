#pragma warning disable CA1416 // Platform compatibility — guarded by runtime OS check inside each test

using System;
using System.Runtime.InteropServices;
using Avalonia.Controls.Win;
using Avalonia.Controls.Win.WebView2;
using Avalonia.Controls.Win.WebView2.Interop;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

public class WebView2BaseAdapterTests
{
    // Reproduces https://github.com/AvaloniaUI/Avalonia.Controls.WebView/issues/27
    // The controller exists but rejects MoveFocus with E_INVALIDARG during a
    // window-activation race (WM_ACTIVATE fires before async init completes).
    // This tests that the adapter's Focus method doesn't throw in this scenario, which would cause a crash at runtime.
    // NOTE: This is a regression test for a specific issue. We test the implementation by faking the behavior
    // of the controller, rather than using a real WebView2 instance, to avoid the complexity of reproducing the exact race
    // condition in a reliable way.
    [Fact]
    public void FocusShouldNotThrowWhenControllerThrowsEInvalidArg()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip("WebView2 adapter is Windows-only");
        }

        var adapter = new TestableAdapter(new ThrowingFakeController());
        
        adapter.Focus();
    }

    private sealed class TestableAdapter(ICoreWebView2Controller controller)
        : WebView2BaseAdapter(controller)
    {
        public override IntPtr Handle => IntPtr.Zero;
        public override string? HandleDescriptor => null;
    }

    private sealed class ThrowingFakeController : ICoreWebView2Controller
    {
        void ICoreWebView2Controller.MoveFocus(COREWEBVIEW2_MOVE_FOCUS_REASON reason)
        {
            // Marshal.GetExceptionForHR produces the exact ArgumentException with HResult
            // E_INVALIDARG that the source-generated COM interop layer throws at runtime.
            throw (ArgumentException)Marshal.GetExceptionForHR(unchecked((int)0x80070057))!;
        }

        void ICoreWebView2Controller.Close() { }

        int ICoreWebView2Controller.GetIsVisible() => throw new NotImplementedException();
        void ICoreWebView2Controller.SetIsVisible(int value) => throw new NotImplementedException();
        tagRECT ICoreWebView2Controller.GetBounds() => throw new NotImplementedException();
        void ICoreWebView2Controller.SetBounds(tagRECT value) => throw new NotImplementedException();
        double ICoreWebView2Controller.GetZoomFactor() => throw new NotImplementedException();
        void ICoreWebView2Controller.SetZoomFactor(double value) => throw new NotImplementedException();
        void ICoreWebView2Controller.add_ZoomFactorChanged(IntPtr eventHandler, out EventRegistrationToken token) => throw new NotImplementedException();
        void ICoreWebView2Controller.remove_ZoomFactorChanged(EventRegistrationToken token) => throw new NotImplementedException();
        void ICoreWebView2Controller.SetBoundsAndZoomFactor(tagRECT bounds, double zoomFactor) => throw new NotImplementedException();
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
        ICoreWebView2 ICoreWebView2Controller.GetCoreWebView2() => throw new NotImplementedException();
    }
}
