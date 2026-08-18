using Microsoft.Extensions.FileProviders;

namespace Avalonia.Controls.BlazorWebView.Samples;

/// <summary>
/// Serves <c>wwwroot</c> from embedded resources so publish output does not need a physical wwwroot folder.
/// </summary>
public sealed class EmbeddedBlazorWebView : BlazorWebView
{
    protected override IFileProvider CreateFileProvider(string contentRootDir)
    {
        // Resource root matches the wwwroot folder embedded via GenerateEmbeddedFilesManifest.
        var embedded = new ManifestEmbeddedFileProvider(typeof(Program).Assembly, "wwwroot");
        var physical = base.CreateFileProvider(contentRootDir);
        return new CompositeFileProvider(embedded, physical);
    }
}
