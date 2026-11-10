using System;
using System.Runtime.CompilerServices;
using Avalonia.Controls.Macios.Interop;
using Avalonia.Controls.Macios.Interop.WebKit;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

public class MaciosInteropTests
{
    [Fact]
    public void CG_Structs_Match_Native_Layout()
    {
        Assert.Equal(32, Unsafe.SizeOf<CGRect>());
        Assert.Equal(16, Unsafe.SizeOf<CGSize>());
    }

    [Fact]
    public void CGRect_Round_Trips_Through_Objective_C()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOSVersionAtLeast(11), "Requires macOS 11+ (WKPDFConfiguration).");
        Assert.True(Macios.Interop.WebKit.WebKit.PreloadWebKit());

        var expected = new CGRect(1.5, 2.25, 640.125, 480.75);

        using var configuration = new WKPDFConfiguration();
        configuration.Rect = expected;

        Assert.Equal(expected, configuration.Rect);
    }
}
