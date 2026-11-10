namespace Avalonia.Controls.Macios.Interop;

// CGFloat is double on 64-bit Apple platforms.
internal record struct CGRect(double X, double Y, double Width, double Height);
internal record struct CGSize(double Width, double Height);
