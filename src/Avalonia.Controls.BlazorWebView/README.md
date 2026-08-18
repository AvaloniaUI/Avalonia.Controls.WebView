# Avalonia.Controls.BlazorWebView

Blazor Hybrid hosting for Avalonia applications using [`Avalonia.Controls.WebView`](https://www.nuget.org/packages/Avalonia.Controls.WebView) (`NativeWebView`).

## Platforms

| Platform | Origin | Notes |
|----------|--------|--------|
| Windows | `http://0.0.0.0/` | WebView2 response synthesis |
| macOS / iOS | `app://0.0.0.0/` | Custom URI scheme |
| Linux (GTK / WPE) | `app://0.0.0.0/` | Custom URI scheme |
| Android | `app://0.0.0.0/` | `ShouldInterceptRequest` |

Browser (WASM) iframe hosting is not supported.

## Quick start

```csharp
var services = new ServiceCollection();
services.AddAvaloniaBlazorWebView();
var provider = services.BuildServiceProvider();

var blazor = new BlazorWebView
{
    Services = provider,
    HostPage = "wwwroot/index.html",
};
blazor.RootComponents.Add(new RootComponent
{
    Selector = "#app",
    ComponentType = typeof(Main),
});
```

Host page (`wwwroot/index.html`) should reference Blazor WebView scripts:

```html
<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <title>Avalonia Blazor Hybrid</title>
</head>
<body>
  <div id="app">Loading...</div>
  <script src="_framework/blazor.webview.js"></script>
</body>
</html>
```

A `chrome.webview` polyfill is injected automatically when HTML is served so Blazor IPC works with Avalonia's `invokeCSharpAction` bridge.

## Prerequisites

Same native WebView prerequisites as `Avalonia.Controls.WebView` (WebView2 Runtime, WebKitGTK, etc.).

## Embedded `wwwroot` (no folder next to the exe)

By default, static files are served from disk (`PhysicalFileProvider`). To ship without a physical `wwwroot` folder, override `CreateFileProvider` and embed the files:

```xml
<PropertyGroup>
  <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
  <StaticWebAssetsEnabled>false</StaticWebAssetsEnabled>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.FileProviders.Embedded" Version="8.0.11" />
  <Content Remove="wwwroot\**" />
  <EmbeddedResource Include="wwwroot\**" />
</ItemGroup>
```

```csharp
protected override IFileProvider CreateFileProvider(string contentRootDir)
{
    var embedded = new ManifestEmbeddedFileProvider(typeof(Program).Assembly, "wwwroot");
    return new CompositeFileProvider(embedded, base.CreateFileProvider(contentRootDir));
}
```

Keep `HostPage` as `wwwroot/index.html` (logical path). `_framework/blazor.webview.js` is already embedded in `Microsoft.AspNetCore.Components.WebView` and does not need to be in your `wwwroot`.

## AOT / trimming

NativeAOT and aggressive trimming are not guaranteed with `Microsoft.AspNetCore.Components.WebView` in v1.
