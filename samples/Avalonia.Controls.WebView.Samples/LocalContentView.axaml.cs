using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Avalonia.Platform;

namespace Avalonia.Controls.WebView.Samples;

/// <summary>
/// Serves the LocalContent folder, embedded into the app, without an HTTP server.
/// </summary>
public partial class LocalContentView : UserControl
{
    // Android WebView can't register schemes, so it gets an https virtual host instead.
    private static readonly Uri s_origin = OperatingSystem.IsAndroid() ?
        new Uri("https://appassets.androidplatform.net/") :
        new Uri("app://localhost/");

    public LocalContentView()
    {
        InitializeComponent();
        WebView.Source = new Uri(s_origin, "index.html");
    }

    private void WebView_OnEnvironmentRequested(object? sender, WebViewEnvironmentRequestedEventArgs e)
    {
        e.EnableDevTools = true;
        if (s_origin.Scheme != Uri.UriSchemeHttps)
            e.CustomSchemes.Add(s_origin.Scheme);

        // WebView2 needs the same schemes on every web view of a user data folder, and the other tabs have none.
        if (e is WindowsWebView2EnvironmentRequestedEventArgs webView2)
            webView2.UserDataFolder = Path.Combine(AppContext.BaseDirectory, "webview-local-content");
    }

    private void WebView_OnWebResourceRequested(object? sender, WebResourceRequestedEventArgs e)
    {
        // Everything else loads normally.
        if (e.Request.Uri.Scheme == s_origin.Scheme && e.Request.Uri.Authority == s_origin.Authority)
            e.Response = LocalContent.GetResponse(e.Request.Uri.AbsolutePath);
    }
}

internal static class LocalContent
{
    private const string Folder = "LocalContent/";
    private static readonly Assembly s_assembly = typeof(LocalContent).Assembly;

    // "data/info.json" -> manifest resource name
    private static readonly Dictionary<string, string> s_files = GetFiles();

    public static WebViewWebResourceResponse GetResponse(string path)
    {
        path = Uri.UnescapeDataString(path).TrimStart('/');
        if (path.Length == 0 || path.EndsWith('/'))
            path += "index.html";

        if (!s_files.TryGetValue(path, out var resourceName))
            return new WebViewWebResourceResponse { StatusCode = 404 };

        return new WebViewWebResourceResponse
        {
            Headers =
            {
                ["Content-Type"] = GetContentType(path),
                ["Cache-Control"] = "no-cache"
            },
            Content = s_assembly.GetManifestResourceStream(resourceName)
        };
    }

    private static Dictionary<string, string> GetFiles()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in s_assembly.GetManifestResourceNames())
        {
            // RecursiveDir uses the build machine's separator.
            var path = name.Replace('\\', '/');
            if (path.StartsWith(Folder, StringComparison.Ordinal))
                files[path[Folder.Length..]] = name;
        }
        return files;
    }

    private static string GetContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".html" => "text/html; charset=utf-8",
        ".js" or ".mjs" => "text/javascript; charset=utf-8",
        ".css" => "text/css; charset=utf-8",
        ".json" => "application/json; charset=utf-8",
        ".svg" => "image/svg+xml",
        ".png" => "image/png",
        ".woff2" => "font/woff2",
        _ => "application/octet-stream"
    };
}
