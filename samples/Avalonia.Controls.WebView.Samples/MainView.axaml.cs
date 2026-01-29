using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
#if AVALONIA
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Platform.Storage;
#elif WPF
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using Microsoft.Win32;
#endif
using AC = Avalonia.Controls;
using AP = Avalonia.Platform;

#if AVALONIA
namespace Avalonia.Controls.WebView.Samples;
#elif WPF
namespace Avalonia.Xpf.Controls.WebView.Samples;
#endif

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        var (requestUri, redirectUri) = GetGoogleAuth();
        RequestUri.Text = requestUri;
        RedirectUri.Text = redirectUri;
    }

    private async void NativeWebView_OnNavigationCompleted(object? sender, AC.WebViewNavigationCompletedEventArgs e)
    {
        LogList.Text += "\r\nNativeWebView_OnNavigationCompleted " + e.Request;

        var webView = (NativeWebView)sender!;
        await InvokeTestScript(webView, """ invokeCSharpAction({'key': 10}) """);

        await InvokeTestScript(webView, "1+1");
        await InvokeTestScript(webView, "'test'");
        await InvokeTestScript(webView, "var x = 123; x");
        await InvokeTestScript(webView, "var x = 'test'; x");
        await InvokeTestScript(webView, "'te\"st'");
        await InvokeTestScript(webView, "'te()st'");
        await InvokeTestScript(webView, "document.body.innerHTML");
        await InvokeTestScript(webView, "true");
        try
        {
            await InvokeTestScript(webView, "throw new Error('Hello there')");
        }
        catch (Exception ex)
        {
            LogList.Text += "\r\nTest Script Exception " + ex.Message;
        }

        if (webView.TryGetCookieManager() is { } manager)
        {
            manager.AddOrUpdateCookie(new Cookie("Hello", "There", "/", ".google.com") { HttpOnly = false });
            var cookies = await manager.GetCookiesAsync();
            foreach (var c in cookies)
            {
                LogList.Text += "\r\nCookie retrieved " + c;
                manager.DeleteCookie(c.Name, c.Domain, c.Path);
            }

            cookies = await manager.GetCookiesAsync();
            foreach (var c in cookies)
            {
                LogList.Text += "\r\nCookie retrieved after delete " + c;
            }
        }
    }

    private async Task InvokeTestScript(NativeWebView webView, string script)
    {
        try
        {
            var result = await webView.InvokeScript(script);
            LogList.Text += "\r\nTest Script " + script + ": " + (result?.Length > 100 ? result[..100] + "..." : result);;
        }
        catch (Exception ex)
        {
            LogList.Text += "\r\nTest Script " + script + " FAILED: " + ex.Message;
        }
    }

    private void NativeWebView_OnNavigationStarted(object? sender, AC.WebViewNavigationStartingEventArgs e)
    {
        LogList.Text += "\r\nNativeWebView_OnNavigationStarted " + e.Request;
    }

    private void NativeWebView_OnWebMessageReceived(object? sender, AC.WebMessageReceivedEventArgs e)
    {
        LogList.Text += "\r\nNativeWebView_OnWebMessageReceived " + e.Body;
    }

    private void NativeWebView_OnWebResourceRequested(object? sender, AC.WebResourceRequestedEventArgs e)
    {
        var requestFormatted = string.Join(Environment.NewLine, e.Request.ToString()
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Take(4)
            .Select(static e => e.Length > 100 ? e[..100] : e));
        LogList.Text += "\r\nNativeWebView_OnWebResourceRequested " + requestFormatted;
    }

    private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
    {
        LogList.Text += "\r\nInputElement_OnKeyDown " + e.Key + " " + GetKeyModifiers(e);
    }

    private async void View_OnKeyDown(object? sender, KeyEventArgs e)
    {
        var modifiers = GetKeyModifiers(e);
        LogList.Text += "\r\nWindow_OnKeyDown " + e.Key + " " + modifiers;

        if (e is { Key: Key.P }
#if AVALONIA
            && modifiers.HasFlag(KeyModifiers.Control))
#elif WPF
            && modifiers.HasFlag(ModifierKeys.Control))
#endif
        {
            try
            {
                var page = await WebView.PrintToPdfStreamAsync();

#if AVALONIA
                var storage = TopLevel.GetTopLevel(this)!.StorageProvider;
                var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    DefaultExtension = ".pdf",
                    SuggestedFileName = "page",
                    FileTypeChoices = [new FilePickerFileType("Pdf") { Patterns = [".pdf"] }]
                });
                if (file is not null)
                {
                    await using var writeStream = await file.OpenWriteAsync();
                    await page.CopyToAsync(writeStream);
                }
#elif WPF
                var saveFileDialog = new SaveFileDialog { DefaultExt = ".pdf", FileName = "page", Filter = "PDF files (*.pdf)|*.pdf" };
                if (saveFileDialog.ShowDialog() == true)
                {
                    await using var writeStream = saveFileDialog.OpenFile();
                    await page.CopyToAsync(writeStream);
                }
#endif
            }
            catch (Exception ex)
            {
                LogList.Text += "\r\nPrint failed " + ex;
            }
        }
        else if (e is { Key: Key.D }
#if AVALONIA
            && modifiers.HasFlag(KeyModifiers.Control))
#elif WPF
            && modifiers.HasFlag(ModifierKeys.Control))
#endif
        {
            try
            {
                WebView.ShowPrintUI();
            }
            catch (Exception ex)
            {
                LogList.Text += "\r\nPrintUI failed " + ex;
            }
        }
        else if (e is { Key: Key.R }
#if AVALONIA
                         && modifiers.HasFlag(KeyModifiers.Control))
#elif WPF
            && modifiers.HasFlag(ModifierKeys.Control))
#endif
        {
            using (WebView.BeginReparenting())
            {
                var currentTab = (TabItem)GridContainer.Parent!;
                currentTab.Content = null;
                var index = TabControl.Items.Add(new TabItem
                {
                    Header = "New Tab",
                    Content = GridContainer
                });
                TabControl.SelectedIndex = index;
            }
        }
    }

    private void InputElement_OnKeyUp(object? sender, KeyEventArgs e)
    {
        LogList.Text += "\r\nInputElement_OnKeyUp " + e.Key + " " + GetKeyModifiers(e);
    }

    private void View_OnKeyUp(object? sender, KeyEventArgs e)
    {
        LogList.Text += "\r\nWindow_OnKeyUp " + e.Key + " " + GetKeyModifiers(e);
    }

    private async void WebAuthenticationBrokerButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
#if AVALONIA
            var topLevel = TopLevel.GetTopLevel(this);
#elif WPF
            var topLevel = Window.GetWindow(this);
#endif
            var options = new WebAuthenticatorOptions(new Uri(RequestUri.Text!), new Uri(RedirectUri.Text!, UriKind.RelativeOrAbsolute));

            var result = await WebAuthenticationBroker.AuthenticateAsync(topLevel!, options);

            CallbackUri.Text = result.CallbackUri.ToString();
        }
        catch (Exception ex)
        {
            CallbackUri.Text = ex.Message;
        }
    }

    private static (string requestUri, string redirectUri) GetGoogleAuth()
    {
        var href = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault();

        var redirectUri = OperatingSystem.IsIOS() ?
            "com.googleusercontent.apps.457602913817-kd2547t40mrvqi63c4m7lphs5s6s5lt2://" :
            OperatingSystem.IsAndroid() ?
                "com.AvaloniaUI.WebView.Samples:/oauth2redirect" :
                OperatingSystem.IsBrowser() ?
                    href?.TrimEnd('/') + "/oauth2redirect" :
                    "http://localhost";
        var clientId = OperatingSystem.IsIOS() ?
            "457602913817-kd2547t40mrvqi63c4m7lphs5s6s5lt2.apps.googleusercontent.com" :
            OperatingSystem.IsAndroid() ?
                "457602913817-12s7l3shl5nipenm61cqdbsu7ehsm26b.apps.googleusercontent.com" :
                OperatingSystem.IsBrowser() ?
                    "457602913817-l99pga4o8j33ujb7af6hrc9icnk88ho1.apps.googleusercontent.com" :
                    "457602913817-2qhv0sr6d08gvs3amj3vjpnodt7hnfai.apps.googleusercontent.com";

        var requestUri = "https://accounts.google.com/o/oauth2/auth?response_type=code&access_type=offline&scope=openid";
        requestUri += "&client_id=" + clientId;
        requestUri += "&redirect_uri=" + redirectUri;
        return (requestUri, redirectUri);
    }

    private void NativeWebView_OnNewWindowRequested(object? sender, AC.WebViewNewWindowRequestedEventArgs e)
    {
        TabControl.Items.Add(new TabItem { Header = "New tab", Content = new NativeWebView { Source = e.Request! } });
        e.Handled = true;
    }

    private unsafe void NativeWebView_OnAdapterCreated(object? sender, AC.WebViewAdapterEventArgs e)
    {
        LogList.Text += "\r\nNativeWebView_OnAdapterCreated " + e.TryGetPlatformHandle()?.GetType().Name;

        // if (e.TryGetPlatformHandle() is IWindowsWebView2PlatformHandle webView2)
        // {
        //     DispatcherTimer.RunOnce(() =>
        //     {
        //         LogList.Text += "\r\nRunning ICoreWebView2.Refresh using native interop";
        //
        //         // Some testing code, just to make sure user can do the same in a better way (CsWin32 or so).
        //         const int refreshMethodOffset = 31;
        //         var vtable = Marshal.ReadIntPtr(webView2.CoreWebView2);
        //         var methodPtr = Marshal.ReadIntPtr(vtable, refreshMethodOffset * IntPtr.Size);
        //         var methodDelegate = (delegate* unmanaged[Stdcall]<IntPtr, int>)methodPtr;
        //         methodDelegate(webView2.CoreWebView2);
        //     }, TimeSpan.FromSeconds(4));
        // }
    }

    private void NativeWebView_OnAdapterDestroyed(object? sender, AC.WebViewAdapterEventArgs e)
    {
        LogList.Text += "\r\nNativeWebView_OnAdapterDestroyed " + e.TryGetPlatformHandle()?.GetType().Name;
    }

    private void NativeWebView_OnEnvironmentRequested(object? sender, AC.WebViewEnvironmentRequestedEventArgs e)
    {
        LogList.Text += "\r\nNativeWebView_OnEnvironmentRequested";
        e.EnableDevTools = true;
        if (e is AP.WindowsWebView2EnvironmentRequestedEventArgs webView2)
        {
            // webView2.UserAgent = "AvaloniaWebView";
            webView2.ProfileName = "AvaloniaUser";
            webView2.UserDataFolder = Path.Combine(AppContext.BaseDirectory, "webview");
        }
        else if (e is AP.AppleWKWebViewEnvironmentRequestedEventArgs wkWebView)
        {
            wkWebView.NonPersistentDataStore = true;
            wkWebView.ApplicationNameForUserAgent = "Avalonia WebView Sample";
            wkWebView.LimitsNavigationsToAppBoundDomains = true;
        }
    }

    private void Make_Transparent_Clicked(object? sender, RoutedEventArgs e)
    {
        TransparentWebView.Background = Brushes.Transparent;
    }

    private void Make_Solid_Background_Clicked(object? sender, RoutedEventArgs e)
    {
        TransparentWebView.Background = Brushes.Green;
    }

    private void Make_Default_Background_Clicked(object? sender, RoutedEventArgs e)
    {
        TransparentWebView.ClearValue(NativeWebView.BackgroundProperty);
    }

    private void Make_Gradient_Background_Clicked(object? sender, RoutedEventArgs e)
    {
        TransparentWebView.Background = new LinearGradientBrush
        {
#if AVALONIA
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
#elif WPF
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(1, 1),
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
#endif
            GradientStops =
            [
                new GradientStop(Colors.Green, 0),
                new GradientStop(Colors.Transparent, 1)
            ]
        };
    }

    private void TransparentWebView_OnLoaded(object? sender, RoutedEventArgs e)
    {
        TransparentWebView.NavigateToString(
            """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <title>WebView Transparency Demo</title>
                <style>
                    html, body {
                        height: 100%;
                        margin: 0;
                        padding: 0;
                        background: transparent;
                    }
                    body {
                        min-height: 100vh;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                    }
                    .demo-content {
                        font-family: sans-serif;
                        font-size: 2em;
                        font-weight: bold;
                        padding: 2em 3em;
                        border-radius: 16px;
                        /* Text is always readable: black with white shadow (for white bg), or white with black shadow (for dark bg) */
                        color: #111;
                        text-shadow: 0 2px 8px #fff, 0 0 2px #fff, 0 0 1px #fff;
                    }
                    body[style*="background-color: black"] .demo-content,
                    body[style*="background: black"] .demo-content {
                        color: #fff;
                        text-shadow: 0 2px 8px #000, 0 0 2px #000, 0 0 1px #000;
                    }
                </style>
            </head>
            <body>
                <div class="demo-content">
                    WebView Transparency Demo
                </div>
            </body>
            </html>
            """);
    }

    private void GridWebViewButton_OnClick(object? sender, RoutedEventArgs e)
    {
        GridWebView.Source = Uri.TryCreate(GridWebViewSource.Text, UriKind.Absolute, out var source) ?
            source :
            new Uri("about:blank");
    }

    private void GridWebViewDialogButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new NativeWebDialog()
        {
            Source = Uri.TryCreate(GridWebViewSource.Text, UriKind.Absolute, out var source) ?
                source :
                new Uri("about:blank"),
            Title = "Avalonia WebView Demo" 
        };
        dialog.Show();
    }

    private void Headers_OnWebResourceRequested(object? sender, AC.WebResourceRequestedEventArgs e)
    {
        e.Request.Headers.TryGetValue("User-Agent", out var userAgent);
        e.Request.Headers.TrySet("X-MyHeader", $"Time: {DateTime.Now:O}");
    }

    private void Offscreen_EnvironmentRequested(object? sender, AC.WebViewEnvironmentRequestedEventArgs e)
    {
        if (e is AP.WindowsWebView2EnvironmentRequestedEventArgs webView2)
        {
            webView2.ExperimentalOffscreen = true;
        }
        else if (e is AP.GtkWebViewEnvironmentRequestedEventArgs gtk)
        {
            gtk.ExperimentalOffscreen = true;
        }
        else
        {
            throw new NotSupportedException();
        }
    }

#if AVALONIA
    private KeyModifiers GetKeyModifiers(KeyEventArgs e) => e.KeyModifiers;
#elif WPF
    private ModifierKeys GetKeyModifiers(KeyEventArgs e) => Keyboard.Modifiers;
#endif
}
