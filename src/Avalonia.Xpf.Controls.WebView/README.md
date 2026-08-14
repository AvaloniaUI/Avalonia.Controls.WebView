# Avalonia Xpf WebView

The Avalonia WebView component provides native web browser functionality for your Avalonia applications. Unlike embedded WebView solutions that require bundling Chromium, this implementation leverages the platform's native web rendering capabilities, resulting in smaller application size and better performance.

## Requirements

This package requires Avalonia XPF license:
https://avaloniaui.net/xpf

## Features

- **Platform-Native Engines**: WebView2 (Windows), WebKit (macOS), WebKitGTK (Linux)
- **Lightweight**: No embedded browser engine required - smaller application footprint
- **AOT Compatible**: Compatible with Ahead-of-Time compilation and trimming
- **Platform Configuration**: Supports WebView2 profiles, persistent storage paths, and many other platform-specific options
- **Web APIs**: JavaScript execution, bidirectional messaging, cookie management, HTTP header interception
- **Authentication**: Web authentication broker for OAuth and web-based authentication

## Quick Start

Get started quickly with the WebView component:
https://docs.avaloniaui.net/xpf/embedding/web-view

## Components

### NativeWebView Control

The main control for embedding web content in your app.

```xaml
<!-- Add to your XAML -->
<wb:NativeWebView x:Name="WebView" 
                  Source="https://avaloniaui.net" />
```

**Documentation**: https://docs.avaloniaui.net/accelerate/components/webview/nativewebview

### NativeWebDialog

Native web dialog that provides a way to display web content in a separate window, particularly useful for platforms like Linux where embedded WebView controls might not be available

**Documentation**: https://docs.avaloniaui.net/accelerate/components/webview/nativewebdialog

### WebAuthenticationBroker

WebAuthenticationBroker is a utility class that facilitates OAuth and other web-based authentication flows by providing a secure way to handle web authentication in desktop applications.
Set `WebAuthenticatorMode.Browser` to run the flow in the user's default browser and capture the callback on a local loopback HTTP listener, which is required by providers that reject embedded webviews.

That listener accepts connections from any process on the machine, so the returned `CallbackUri` is untrusted input: validate its `code`, `state` and `error` parameters, and use PKCE.

**Documentation**: https://docs.avaloniaui.net/accelerate/components/webview/webauthenticationbroker
