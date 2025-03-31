# Avalonia WebView

## Components

### NativeWebView

Control that embeds native control host into a visual controls tree.

Current implementation only supports NativeControlHost, but API technically can be extended to offscreen implementations in the future.

### NativeWebDialog

Dialog-only version of the webview. Opens web page in the dedicated window, with limited API/control.
Mainly useful on Linux, where NativeControlHost is not possible.

Has two implementations:
- GTK window with GtkWebView for Linux
- And Avalonia window combined with NativeWebView

### WebAuthenticationBroker

A specific API usable for OAuth authentication. 
Gets oauth URL as an input, and returns redirected URL once authentication is completed.
More or less just a helper over OAuth specification.
Should be compatible with Google, Azure and custom OAuth implementations, based on redirect Urls.

## Projects

### Avalonia.Controls.WebView

Avalonia specific project

### Avalonia.Xpf.Controls.WebView

XPF/WPF specific project.

### Avalonia.Controls.WebView.Core

Shared code between Avalonia and XPF.
Note 1: this project is ILRepack merged into Avalonia/XPF ones. This project is not published on NuGet.
Note 2: some shared code is linked as files, like NativeWebView.cs, with `#if AVALONIA/XPF` conditions. 

## Build

This repo uses NukeBuild for build and packaging.
Main build steps:
1. Compile - builds all the `src` projects
2. IlMerge - merges `Core` project into main output assemblies
3. Obfuscate - as per name
4. CreateNugetPackages - creates output nuget packages. Note, `PackEnable.targets` is used to feed package information to the project.
5. CopyPackagesToNuGetCache - copies packages to the nuget cache. This way these packages can be used without nuget server, just specify `9999.0.0-localbuild` and you can use it locally from another solution.

For different shared props/targets see `build/Props` folder. 
Assembly signing is currently disabled. Some dependencies are not signed either.
