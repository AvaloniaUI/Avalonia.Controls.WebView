using System.Runtime.InteropServices;
using Avalonia.Controls.Rendering;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

public class BitmapFrameChainTests
{
    [AvaloniaFact]
    public void Frames_Are_Allocated_In_The_Requested_Format()
    {
        var chain = new BitmapFrameChain(PixelFormats.Rgba8888, AlphaFormat.Unpremul);

        using (chain.Producer.GetNextFrame(new PixelSize(4, 4), out var frame))
        {
            Assert.Equal(PixelFormats.Rgba8888, frame.Format);
            Assert.Equal(AlphaFormat.Unpremul, frame.AlphaFormat);
        }
    }

    [AvaloniaFact]
    public void Rgba_Frames_Keep_The_Channel_Order_Of_The_Source_Buffer()
    {
        var chain = new BitmapFrameChain(PixelFormats.Rgba8888, AlphaFormat.Unpremul);
        using var destination = new WriteableBitmap(
            new PixelSize(1, 1), new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Premul);

        using (chain.Producer.GetNextFrame(new PixelSize(1, 1), out var frame))
        {
            using (var frameBuffer = frame.Lock())
            {
                // #FFFF00 as GdkPixbuf stores it.
                Marshal.Copy(new byte[] { 0xFF, 0xFF, 0x00, 0xFF }, 0, frameBuffer.Address, 4);
            }

            using var destinationBuffer = destination.Lock();
            frame.CopyPixels(destinationBuffer);
        }

        var pixel = new byte[4];
        using (var destinationBuffer = destination.Lock())
        {
            Marshal.Copy(destinationBuffer.Address, pixel, 0, 4);
        }

        // #FFFF00 as Bgra8888 stores it.
        Assert.Equal([0x00, 0xFF, 0xFF, 0xFF], pixel);
    }
}
