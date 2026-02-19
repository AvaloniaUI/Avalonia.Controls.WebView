using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;
using Avalonia.Controls.Win.WebView1.Interop;

namespace Avalonia.Controls.Win.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("B403CA50-7F8C-4E83-985F-CC45060036D8")]
internal partial interface ICompositor : IInspectable
{
    IntPtr CreateColorKeyFrameAnimation();
    IntPtr CreateColorBrush();
    IntPtr CreateColorBrushWithColor(winrtColor color);
    IContainerVisual CreateContainerVisual();
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("48EA31AD-7FCD-4076-A79C-90CC4B852C9B")]
internal partial interface ICompositor5 : IInspectable
{
    IntPtr GetComment();
    void SetComment(IntPtr value);
    float GetGlobalPlaybackRate();
    void SetGlobalPlaybackRate(float value);
    IntPtr CreateBounceScalarAnimation();
    IntPtr CreateBounceVector2Animation();
    IntPtr CreateBounceVector3Animation();
    IntPtr CreateContainerShape();
    IntPtr CreateEllipseGeometry();
    IntPtr CreateLineGeometry();
    IntPtr CreatePathGeometry();
    IntPtr CreatePathGeometryWithPath(IntPtr path);
    IntPtr CreatePathKeyFrameAnimation();
    IntPtr CreateRectangleGeometry();
    IntPtr CreateRoundedRectangleGeometry();
    IntPtr CreateShapeVisual();
    IntPtr CreateSpriteShape();
    IntPtr CreateSpriteShapeWithGeometry(IntPtr geometry);
    IntPtr CreateViewBox();

    IAsyncAction RequestCommitAsync();
}
