# 12.0.0-preview2

- Compatibility with Avalonia 12.0, while keeping 11.1 support
- Dropped .NET Standard and .NET Framework support
- NativeWebView and NativeWebDialog now support Browser backend, but with sandboxing limitations

# 11.3.16

- New WebViewAdapterInfo API to retrieve WebView engine and availability information
- Implemented Android printing functions
- Implemented `PrintToPdfStreamAsync(WebViewPrintSettings)` overload
- Fixed Windows 7 support for WebView2
- Experimental WebView2 offscreen rendering, can be enabled via WindowsWebView2EnvironmentRequestedEventArgs

# 11.3.15

- Fixed threading issue on XPF

# 11.3.14

- Fixed crash when using `NativeWebView.Background` on iOS

# 11.3.13

- Improves Mono AOT compatibility, removing warnings
- Fixes GTK NativeWebDialog.Canceling event not being raised 

# 11.3.11

- First public NuGet release!
- Adds `NativeWebView.ShowPrintUI` and `NativeWebView.PrintToPdfStreamAsync` methods for printing web content, supported on Windows, macOS and Linux GTK (with NativeWebDialog)

# 11.3.10

- Fixes Android WebAuthenticationBroker not working with major OAuth providers
- Adds `WindowsWebView2EnvironmentRequestedEventArgs.AllowSingleSignOnUsingOSPrimaryAccount` parameter
- Adds `EnvironmentRequestedEventArgs.GetDeferrer()` for async initialization of the webview environment, if necessary

# 11.3.9

- Implements Android `WebResourceRequested` header interception
- Fixes Android `WebResourceRequested` not being executed on the UI thread

# 11.3.8

- Fixes Gtk `NativeWebDialog` depending on the libsoap3.0, even if it's not installed
- Fixes Gtk `NativeWebDialog` relying on `g_timeout_add_once` on older platforms

# 11.3.7

- Implements Android `NativeWebDialog`
- Implements Android `AndroidWebViewEnvironmentRequestedEventArgs`
- Fixes Gtk `NativeWebDialog` crashing on some .NET runtime versions
- Fixes Gtk AOT compilation

# 11.3.6

- Fixes GTK `NativeWebDialog` on XPF
- Adds WebView2 STA thread check

# 11.3.5

- Fixes WebView2 on ARM64 processors

# 11.3.4

- Imroves automatic sizing of `NativeWebView`
- Imrpoves NuGet packaging, readme, description

# 11.3.3

- Makes `NativeWebView` compatible with XPF

# 11.3.2

- Extended focus handling support for WebView2
- Adds strong naming to the assembly
- Implements `NativeWebView.Background`, with support for transparent background on some backends

# 11.3.1

- Implements `NativeWebView.EnvironmentRequested` and `NativeWebDialog.EnvironmentRequested`
- Enhanced platform interop with typed `NativeWebView.TryGetPlatformHandle`
- Added `AdapterInitialized` and `AdapterDestroyed`
- Added `WebResourceRequested`
- Added `WebAuthenticationBroker` options for `NonPersistent` and `NativeWebDialogFactory`

# 11.3.0

- Initial release
