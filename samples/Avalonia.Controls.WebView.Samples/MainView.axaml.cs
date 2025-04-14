using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls.WebView.Samples;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        var (requestUri, redirectUri) = GetGoogleAuth();
        RequestUri.Text = requestUri;
        RedirectUri.Text = redirectUri;
    }

    private async void NativeWebView_OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        LogList.Text += "\r\nNativeWebView_OnNavigationCompleted " + e.Request;

        var webView = (NativeWebView)sender!;
        _ = await webView.InvokeScript(""" invokeCSharpAction({'key': 10}) """);

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
            manager.AddOrUpdateCookie(new Cookie("Hello", "There", "/", ".google.com")
            {
                HttpOnly = false
            });
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
        var result = await webView.InvokeScript(script);
        LogList.Text += "\r\nTest Script " + script + ": " + result;
    }

    private void NativeWebView_OnNavigationStarted(object? sender, WebViewNavigationStartingEventArgs e)
    {
        LogList.Text += "\r\nNativeWebView_OnNavigationStarted " + e.Request;
    }

    private void NativeWebView_OnWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        LogList.Text += "\r\nNativeWebView_OnWebMessageReceived " + e.Body;
    }

    private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
    {
        LogList.Text += "\r\nInputElement_OnKeyDown " + e.Key + " " + e.KeyModifiers;
    }

    private void Window_OnKeyDown(object? sender, KeyEventArgs e)
    {
        LogList.Text += "\r\nWindow_OnKeyDown " + e.Key + " " + e.KeyModifiers;

        if (e.Key == Key.P)
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
        LogList.Text += "\r\nInputElement_OnKeyUp " + e.Key + " " + e.KeyModifiers;
    }

    private void Window_OnKeyUp(object? sender, KeyEventArgs e)
    {
        LogList.Text += "\r\nWindow_OnKeyUp " + e.Key + " " + e.KeyModifiers;
    }

    private async void WebAuthenticationBrokerButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
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
        Console.WriteLine("HRef " + href);

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

    private void NativeWebView_OnNewWindowRequested(object? sender, WebViewNewWindowRequestedEventArgs e)
    {
        TabControl.Items.Add(new TabItem { Header = "New tab", Content = new NativeWebView { Source = e.Request! } });
        e.Handled = true;
    }
}

