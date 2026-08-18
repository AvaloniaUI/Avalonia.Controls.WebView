# Avalonia WebView

The Avalonia WebView component provides native web browser functionality for your Avalonia applications. Unlike embedded WebView solutions that require bundling Chromium, this implementation leverages the platform's native web rendering capabilities, resulting in smaller application size and better performance.

## Features

- **Platform-Native Engines**: WebView2 (Windows), WebKit (macOS), WebKitGTK (Linux)
- **Lightweight**: No embedded browser engine required - smaller application footprint
- **AOT Compatible**: Compatible with Ahead-of-Time compilation and trimming
- **Platform Configuration**: Supports WebView2 profiles, persistent storage paths, and many other platform-specific options
- **Web APIs**: JavaScript execution, bidirectional messaging, cookie management, HTTP header interception
- **Local content serving**: Fulfill requests with app-local bytes via `WebResourceRequestedEventArgs.SetResponse` and custom URI schemes (`CustomSchemes`)
- **Authentication**: Web authentication broker for OAuth and web-based authentication
- **Printing**: Print web content directly from the WebView

## Local content / Hybrid

To serve offline SPA or Blazor Hybrid content:

1. Register a custom scheme (non-Windows) in `EnvironmentRequested`:

```csharp
webView.EnvironmentRequested += (_, e) => e.CustomSchemes.Add("app");
```

2. Fulfill matching requests:

```csharp
webView.WebResourceRequested += (_, e) =>
{
    if (e.Request.Uri.Scheme != "app") return;
    e.SetResponse(
        content: File.OpenRead("wwwroot/index.html"),
        statusCode: 200,
        reasonPhrase: "OK",
        contentType: "text/html");
};
webView.Navigate(new Uri("app://0.0.0.0/"));
```

On Windows, Hybrid apps typically use `http://0.0.0.0/` with `SetResponse` instead of a custom scheme.

For Blazor Hybrid, use the separate [`Avalonia.Controls.BlazorWebView`](../Avalonia.Controls.BlazorWebView/README.md) package.

## Quick Start

Get started quickly with the WebView component:
https://docs.avaloniaui.net/accelerate/components/webview/quickstart

## Components

### NativeWebView Control

The main control for embedding web content in your app.

```xaml
<NativeWebView x:Name="WebView" 
               Source="https://avaloniaui.net" />
```

**Documentation**: https://docs.avaloniaui.net/accelerate/components/webview/nativewebview

### NativeWebDialog

Native web dialog that provides a way to display web content in a separate window, particularly useful for platforms like Linux where embedded WebView controls might not be available

**Documentation**: https://docs.avaloniaui.net/accelerate/components/webview/nativewebdialog

### WebAuthenticationBroker

WebAuthenticationBroker is a utility class that facilitates OAuth and other web-based authentication flows by providing a secure way to handle web authentication in desktop applications.
Set `WebAuthenticatorMode.Browser` to run the flow in the user's default browser and capture the callback on a local loopback HTTP listener, which is required by providers that reject embedded webviews.

That listener accepts connections from any process on the machine, so the result is untrusted input. `WebAuthenticationResult` parses the callback query into `Code`, `State`, `Error` and `ErrorDescription` (and the full `Parameters` set); check `State` against the value you sent, and use PKCE.

**Documentation**: https://docs.avaloniaui.net/accelerate/components/webview/webauthenticationbroker

### OAuth 2.0 authorization code flow with PKCE

`AuthorizationCodePkceSession` runs the flow against an OAuth 2.0 authorization server: it discovers the endpoints from the issuer, builds the authorization request with PKCE (RFC 7636), validates the callback, and exchanges the code for tokens.

```csharp
var session = await AuthorizationCodePkceSession.CreateAsync(
    "https://id.example.com", clientId, "http://127.0.0.1:5000/callback", "openid profile");

var options = new WebAuthenticatorOptions(session.AuthorizationUri, session.RedirectUri)
{
    Mode = WebAuthenticatorMode.Browser,
    BrowserOptions = new BrowserOptions { CallbackFilter = session.IsCallbackFor },
};

var result = await WebAuthenticationBroker.AuthenticateAsync(topLevel, options);
var token = await session.ExchangeCodeAsync(result);
```

Discovery follows RFC 8414 (`/.well-known/oauth-authorization-server`) and falls back to OpenID Connect discovery. The issuer and every endpoint taken from the metadata document must be `https`, and the document must declare the issuer it was requested for. Use `AuthorizationCodePkceSession.Create` instead when the server publishes no metadata and you know the endpoints.

The `id_token` in the response is returned as received and is not validated.

**Sample**: `samples/Avalonia.Controls.WebView.Samples.Oidc`
