namespace Avalonia.Controls.Macios.Interop;

// CGFloat is double on 64-bit Apple platforms (macOS and iOS are 64-bit only).
// Using float here causes ABI corruption: float struct fields go in s-registers,
// but ObjC expects doubles in d-registers on arm64.
internal record struct CGRect(double X, double Y, double Width, double Height);
internal record struct CGSize(double Width, double Height);
