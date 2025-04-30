using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.Controls.Rendering;

internal class BitmapFrameChain : FrameChainBase<WriteableBitmap, PixelSize>
{
    protected override WriteableBitmap CreateFrame(PixelSize size)
    {
        return new WriteableBitmap(size, new Vector(96, 96), PixelFormats.Rgba8888);
    }

    protected override void FreeFrame(WriteableBitmap frame)
    {
        frame?.Dispose();
    }
}
