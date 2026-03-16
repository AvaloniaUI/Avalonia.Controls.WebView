# Avalonia Controls WebView

## Components

### NativeWebView

Control that embeds native control host into a visual controls tree.

Current implementation supports NativeControlHost for native handles and composition host for offscreen buffers. 

### NativeWebDialog

Dialog-only version of the webview. Opens web page in the dedicated window, with limited API/control.

### WebAuthenticationBroker

A specific API usable for OAuth authentication. 
Gets oauth URL as an input, and returns redirected URL once authentication is completed.
More or less just a helper over OAuth specification.
Compatible with Google, Azure and custom OAuth implementations, based on redirect Urls.

See Accelerate `avalonia-docs` for actual documentation on each component. 

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
3. CreateNugetPackages - creates output nuget packages. Note, `PackEnable.targets` is used to feed package information to the project.
4. CopyPackagesToNuGetCache - copies packages to the nuget cache. This way these packages can be used without nuget server, just specify `9999.0.0-localbuild` and you can use it locally from another solution.
