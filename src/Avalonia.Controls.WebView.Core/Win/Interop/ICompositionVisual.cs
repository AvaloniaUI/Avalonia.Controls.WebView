using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct winrtVector2
{
    public float X;
    public float Y;
}
[StructLayout(LayoutKind.Sequential)]
internal struct winrtVector3
{
    public float X;
    public float Y;
    public float Z;
}

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("117E202D-A859-4C89-873B-C2AA566788E3")]
internal partial interface ICompositionVisual : IInspectable
{
    winrtVector2 GetAnchorPoint();
    void SetAnchorPoint(winrtVector2 value);

    int GetBackfaceVisibility();
    void SetBackfaceVisibility(int value);

    int GetBorderMode();
    void SetBorderMode(int value);

    winrtVector3 GetCenterPoint();
    void SetCenterPoint(winrtVector3 value);

    IntPtr GetClip();
    void SetClip( /* CompositionClip */ IntPtr value);

    int GetCompositeMode();
    void SetCompositeMode( /* CompositionCompositeMode */ int value);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetIsVisible();

    void SetIsVisible([MarshalAs(UnmanagedType.Bool)] bool value);

    winrtVector3 GetOffset();
    void SetOffset(winrtVector3 value);

    float GetOpacity();
    void SetOpacity(float value);

    IntPtr GetOrientation();
    void SetOrientation( /* Quaternion */ IntPtr value);

    IContainerVisual GetParent();

    float GetRotationAngle();
    void SetRotationAngle(float value);

    float GetRotationAngleInDegrees();
    void SetRotationAngleInDegrees(float value);

    winrtVector3 GetRotationAxis();
    void SetRotationAxis(winrtVector3 value);

    winrtVector3 GetScale();
    void SetScale(winrtVector3 value);

    winrtVector2 GetSize();
    void SetSize(winrtVector2 value);

    IntPtr GetTransformMatrix();
    void SetTransformMatrix( /* Matrix4x4 */ IntPtr value);
}
