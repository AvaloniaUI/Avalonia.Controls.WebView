using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaloniaUI.WebView.Avalonia.Samples;

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

        await ((NativeWebView)sender!).InvokeScript(""" invokeCSharpAction({'key': 10}) """);

        var result = await ((NativeWebView)sender!).InvokeScript("""var a = 1; var b = 2; a + b""");
        LogList.Text += "\r\nFrom javascript: 1 + 2 is " + result;
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
            var options = new WebAuthenticatorOptions(new Uri(RequestUri.Text!), new Uri(RedirectUri.Text!));

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
        var redirectUri = OperatingSystem.IsIOS() ?
            "com.googleusercontent.apps.457602913817-kd2547t40mrvqi63c4m7lphs5s6s5lt2://" :
            "http://localhost";
        var clientId = OperatingSystem.IsIOS() ?
            "457602913817-kd2547t40mrvqi63c4m7lphs5s6s5lt2.apps.googleusercontent.com" :
            "457602913817-2qhv0sr6d08gvs3amj3vjpnodt7hnfai.apps.googleusercontent.com";

        var requestUri = "https://accounts.google.com/o/oauth2/auth?response_type=code&access_type=offline&scope=openid";
        requestUri += "&client_id=" + clientId;
        requestUri += "&redirect_uri=" + redirectUri;
        return (requestUri, redirectUri);
    }
}

