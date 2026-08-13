using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.Controls.Rendering;

internal class BitmapFrameChain(PixelFormat? pixelFormat, AlphaFormat? alphaFormat)
    : FrameChainBase<WriteableBitmap, PixelSize>
{
    protected override WriteableBitmap CreateFrame(PixelSize size)
    {
        return new WriteableBitmap(size, new Vector(96, 96), pixelFormat, alphaFormat);
    }

    protected override void FreeFrame(WriteableBitmap frame)
    {
        frame?.Dispose();
    }
}
