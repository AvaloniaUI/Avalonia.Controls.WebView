using System;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

public class HeadlessAdapterTests : HeadlessTestsBase
{
    [AvaloniaFact]
    public void Should_Initialize_As_Headless()
    {
        var window = new Window();
        var webView = new NativeWebView();
        window.Content = webView;
        window.Show();

        Assert.Equal("HeadlessWebViewAdapter", webView.TryGetPlatformHandle()?.HandleDescriptor);
    }

    [AvaloniaFact]
    public void Should_Delay_Adapter_Creation()
    {
        var window = new Window();
        var webView = new NativeWebView();
        var tcs = new TaskCompletionSource();
        var adapterCreated = false;
        webView.EnvironmentRequested += (_, args) =>
        {
            if (args is HeadlessWebViewEnvironmentRequestedEventArgs headless)
            {
                headless.InitializeAsync = () => tcs.Task;
            }
        };
        webView.AdapterInitialized += (_, _) => adapterCreated = true;
        window.Content = webView;
        window.Show();

        Assert.False(adapterCreated);
        tcs.SetResult();
        Assert.True(adapterCreated);
    }

    [AvaloniaFact]
    public async Task NativeWebView_Navigation_And_Events()
    {
        var window = new Window();
        var webView = new NativeWebView();
        bool navStarted = false, navCompleted = false, resourceRequested = false;
        webView.EnvironmentRequested += (_, _) => { };
        webView.NavigationStarted += (_, _) => navStarted = true;
        webView.NavigationCompleted += (_, _) => navCompleted = true;
        webView.WebResourceRequested += (_, _) => resourceRequested = true;
        window.Content = webView;
        window.Show();

        var uri = new Uri("https://example.com");
        webView.Source = uri;
        await DoDelay();

        Assert.True(navStarted);
        Assert.True(navCompleted);
        Assert.True(resourceRequested);
        Assert.Equal(uri, webView.Source);
        Assert.False(webView.CanGoBack);
        Assert.False(webView.CanGoForward);
    }

    [AvaloniaFact]
    public async Task NativeWebView_NavigateToString_And_Refresh()
    {
        var window = new Window();
        var webView = new NativeWebView();
        bool navCompleted = false;
        webView.EnvironmentRequested += (_, _) => { };
        webView.NavigationCompleted += (_, _) => navCompleted = true;
        window.Content = webView;
        window.Show();

        webView.NavigateToString("<html>test</html>");
        await DoDelay();

        Assert.True(navCompleted);
        Assert.Equal(WebViewHelper.EmptyPage, webView.Source);

        navCompleted = false;
        Assert.True(webView.Refresh());
        await DoDelay();
        Assert.True(navCompleted);
    }

    [AvaloniaFact]
    public async Task NativeWebView_History_GoBack_Forward()
    {
        var window = new Window();
        var webView = new NativeWebView();
        webView.EnvironmentRequested += (_, _) => { };
        window.Content = webView;
        window.Show();

        var uri1 = new Uri("https://a.com");
        var uri2 = new Uri("https://b.com");
        webView.Source = uri1;
        await DoDelay();
        webView.Source = uri2;
        await DoDelay();

        Assert.True(webView.CanGoBack);
        Assert.False(webView.CanGoForward);

        Assert.True(webView.GoBack());
        await DoDelay();
        Assert.False(webView.CanGoBack);
        Assert.True(webView.CanGoForward);

        Assert.True(webView.GoForward());
        await DoDelay();
        Assert.True(webView.CanGoBack);
        Assert.False(webView.CanGoForward);
    }

    [AvaloniaFact]
    public async Task NativeWebView_Stop_Cancels_Navigation()
    {
        var window = new Window();
        var webView = new NativeWebView();
        webView.EnvironmentRequested += (_, args) =>
        {
            if (args is HeadlessWebViewEnvironmentRequestedEventArgs headless)
            {
                headless.HttpHandler = async _ =>
                {
                    await Task.Delay(100);
                    return new HeadlessWebViewEnvironmentRequestedEventArgs.HttpResult(true, "ok");
                };
            }
        };
        bool navCompleted = false;
        webView.NavigationCompleted += (_, _) => navCompleted = true;
        window.Content = webView;
        window.Show();

        webView.Source = new Uri("https://slow.com");
        Assert.True(webView.Stop());
        await Task.Delay(120);

        Assert.False(navCompleted);
    }

    [AvaloniaFact]
    public async Task NativeWebView_InvokeScript_And_Events()
    {
        var window = new Window();
        var webView = new NativeWebView();
        string? message = null;
        Uri? newWindowUri = null;
        webView.EnvironmentRequested += (_, _) => { };
        webView.WebMessageReceived += (_, e) => message = e.Body;
        webView.NewWindowRequested += (_, e) => newWindowUri = e.Request;
        window.Content = webView;
        window.Show();

        // Simulate invokeCSharpAction
        await webView.InvokeScript("window.external.invokeCSharpAction('msg')");
        Assert.Equal("msg", message);

        // Simulate open new window
        await webView.InvokeScript("window.open('https://new.com')");
        await DoDelay();
        Assert.Equal(new Uri("https://new.com"), newWindowUri);

        // Simulate open link
        await webView.InvokeScript("window.location.replace('https://nav.com')");
        await DoDelay();
        Assert.Equal(new Uri("https://nav.com"), webView.Source);

        // Simulate getHTMLContent
        webView.NavigateToString("<html>abc</html>");
        await DoDelay();
        var html = await webView.InvokeScript("getHTMLContent()");
        Assert.Equal("<html>abc</html>", html);

        // Simulate unknown script
        var result = await webView.InvokeScript("unknown()");
        Assert.Contains("Executed script", result);
    }

    [AvaloniaFact]
    public async Task NativeWebView_Dispose_Throws_On_Use()
    {
        var window = new Window();
        var webView = new NativeWebView();
        webView.EnvironmentRequested += (_, _) => { };
        window.Content = webView;
        window.Show();
        window.Close();

        await Assert.ThrowsAsync<InvalidOperationException>(() => webView.InvokeScript("test"));
    }

    [AvaloniaFact]
    public async Task NativeWebView_Navigation_Can_Be_Cancelled()
    {
        var window = new Window();
        var webView = new NativeWebView();
        webView.EnvironmentRequested += (_, _) => { };
        webView.NavigationStarted += (_, e) => e.Cancel = true;
        bool navCompleted = false;
        webView.NavigationCompleted += (_, _) => navCompleted = true;
        window.Content = webView;
        window.Show();

        webView.Source = new Uri("https://cancel.com");
        await DoDelay();

        Assert.True(navCompleted);
    }
}
