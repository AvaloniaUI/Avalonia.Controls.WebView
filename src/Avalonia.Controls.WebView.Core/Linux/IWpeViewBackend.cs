using System;
using Avalonia.Controls.Linux.Interop;
using Avalonia.Controls.Rendering;
using Avalonia.Input;
using Avalonia.Media.Imaging;

namespace Avalonia.Controls.Linux;

internal interface IWpeViewBackend : IDisposable
{
    string WebViewPropertyName { get; }
    GValue WebViewPropertyValue { get; }

    IntPtr WpeViewBackend { get; }
    IntPtr WpeView { get; }

    Func<bool>? FrameReady { get; set; }

    void Attach(IntPtr webView, float scale);
    void ReleaseFrames();
    void CopyFrame(FrameChainBase<WriteableBitmap, PixelSize>.IProducer producer);

    void SetSize(uint logicalWidth, uint logicalHeight);
    void SetScale(float scale);
    void SetVisible(bool visible);

    uint UnicodeToKeysym(uint codepoint);
    void KeyInput(bool press, uint keysym, uint hardwareKeycode, uint modifiers);
    void PointerInput(PointerPoint point, int clickCount, double scale, uint modifiers);
    bool PointerLeaveInput(PointerPoint point, double scale, uint modifiers);
    void PointerWheelInput(Vector delta, PointerPoint point, double scale, uint modifiers);
}
