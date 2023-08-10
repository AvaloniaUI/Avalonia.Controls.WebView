using System.IO;

namespace AvaloniaUI.WebView.TinyMCE.Core;

internal static class HtmlPageBuilder
{
    static HtmlPageBuilder()
    {
        var assembly = typeof(HtmlPageBuilder).Assembly;

        using (var htmlStream = assembly.GetManifestResourceStream("AvaloniaUI.WebView.TinyMCE.Core.tiny_mce4.tiny_mce.html")!)
        using (var htmlStreamReader = new StreamReader(htmlStream))
        using (var scriptStream = assembly.GetManifestResourceStream("AvaloniaUI.WebView.TinyMCE.Core.tiny_mce4.tiny_mce.min.js")!)
        using (var scriptStreamReader = new StreamReader(scriptStream))
        {
            HtmlBaseContent = htmlStreamReader.ReadToEnd()
                .Replace("/*${tinyMceScript}*/", scriptStreamReader.ReadToEnd());
        }

        using (var stream = assembly.GetManifestResourceStream("AvaloniaUI.WebView.TinyMCE.Core.tiny_mce4.tiny_mce.lightgray.css")!)
        using (var reader = new StreamReader(stream))
        {
            LightGrayStyle = reader.ReadToEnd();
        }
        
        using (var stream = assembly.GetManifestResourceStream("AvaloniaUI.WebView.TinyMCE.Core.tiny_mce4.tiny_mce.charcoal.css")!)
        using (var reader = new StreamReader(stream))
        {
            CharcoalStyle = reader.ReadToEnd();
        }
        
        using (var stream = assembly.GetManifestResourceStream("AvaloniaUI.WebView.TinyMCE.Core.tiny_mce4.tiny_mce.content.dark.css")!)
        using (var reader = new StreamReader(stream))
        {
            ContentDarkStyle = reader.ReadToEnd();
        }
        
        using (var stream = assembly.GetManifestResourceStream("AvaloniaUI.WebView.TinyMCE.Core.tiny_mce4.tiny_mce.content.light.css")!)
        using (var reader = new StreamReader(stream))
        {
            ContentLightStyle = reader.ReadToEnd();
        }
    }

    private static string HtmlBaseContent { get; }
    internal static string LightGrayStyle { get; }
    internal static string CharcoalStyle { get; } 
    internal static string ContentLightStyle { get; }
    internal static string ContentDarkStyle { get; } 

    public static string Build(
        string style,
        string contentStyle,
        string? fontName,
        int fontSize,
        string? background,
        string? foreground,
        string toolbarOptions,
        string plugins)
    {
        return HtmlBaseContent
            .Replace("/*${tinyMceStyle}*/", style)
            .Replace("/*${tinyMceContentStyle}*/", contentStyle)
            .Replace("/*${rootStyles}*/", $"margin: 0; background: {background}; color: {foreground};")
            .Replace("/*${toolbar}*/", toolbarOptions)
            .Replace("/*${plugins}*/", plugins)
            .Replace("/*${fontName}*/", fontName ?? "Arial")
            .Replace("/*${fontSize}*/", fontSize.ToString());
    }
}
