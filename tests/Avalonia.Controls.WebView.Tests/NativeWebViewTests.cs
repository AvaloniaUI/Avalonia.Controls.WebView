using Avalonia.Headless.XUnit;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

public class NativeWebViewTests : HeadlessTestsBase
{
    [AvaloniaFact]
    public void Should_Set_And_Reflect_Source_Property()
    {
        var window = new Window();
        var webView = new NativeWebView();
        window.Content = webView;
        window.Show();

        var uri = new Uri("https://avaloniaui.net");
        webView.Source = uri;
        Assert.Equal(uri, webView.Source);
    }

    [AvaloniaFact]
    public void Should_Expose_Command_Manager()
    {
        var window = new Window();
        var webView = new NativeWebView();
        window.Content = webView;
        window.Show();

        Assert.NotNull(webView.TryGetCommandManager());
    }

    [AvaloniaFact]
    public async Task Should_Navigate_And_NavigateToString()
    {
        var window = new Window();
        var webView = new NativeWebView();
        window.Content = webView;
        window.Show();

        var uri = new Uri("https://avaloniaui.net");
        webView.Navigate(uri);
        await DoDelay();
        Assert.Equal(uri, webView.Source);

        webView.NavigateToString("<html>abc</html>");
        await DoDelay();
        Assert.Equal(WebViewHelper.EmptyPage, webView.Source);
    }

    [AvaloniaFact]
    public void Should_Raise_AdapterInitialized_And_AdapterDestroyed()
    {
        var window = new Window();
        var webView = new NativeWebView();
        bool initialized = false, destroyed = false;
        webView.AdapterInitialized += (_, _) => initialized = true;
        webView.AdapterDestroyed += (_, _) => destroyed = true;
        window.Content = webView;
        window.Show();

        Assert.True(initialized);

        window.Close();
        Assert.True(destroyed);
    }

    [AvaloniaFact]
    public async Task Should_Navigate_If_Navigate_Called_Before_Window_Show()
    {
        var window = new Window();
        var webView = new NativeWebView();
        var uri = new Uri("https://avaloniaui.net");
        bool navCompleted = false;
        webView.NavigationCompleted += (_, e) =>
        {
            if (e.Request == uri)
                navCompleted = true;
        };

        webView.Navigate(uri); // before adding to window
        window.Content = webView;
        window.Show();
        await DoDelay();

        Assert.Equal(uri, webView.Source);
        Assert.True(navCompleted);
    }

    [AvaloniaFact]
    public async Task Should_NavigateToString_If_Called_Before_Window_Show()
    {
        var window = new Window();
        var webView = new NativeWebView();
        bool navCompleted = false;
        webView.NavigationCompleted += (_, e) =>
        {
            if (e.Request == WebViewHelper.EmptyPage)
                navCompleted = true;
        };

        webView.NavigateToString("<html>abc</html>"); // before adding to window
        window.Content = webView;
        window.Show();
        await DoDelay();

        Assert.Equal(WebViewHelper.EmptyPage, webView.Source);
        Assert.True(navCompleted);
    }

    [AvaloniaFact(Skip = "Headless platform doesn't support NativeControlHost attachment, so we can't test this now.")]
    public void Should_BeginReparenting_Return_Disposable()
    {
        var owner1 = new Panel();
        var owner2 = new Panel();
        var window = new Window { Content = new Panel { Children = { owner1, owner2 } } };

        var webView = new NativeWebView();
        owner1.Children.Add(webView);
        window.Show();

        var wasRecreated = false;
        var wasDestroyed = false;
        webView.AdapterInitialized += (_, _) => wasRecreated = true;
        webView.AdapterDestroyed += (_, _) => wasDestroyed = true;

        using (webView.BeginReparenting())
        {
            owner1.Children.Remove(webView);
            owner2.Children.Add(webView);
        }

        Assert.False(wasRecreated);
        Assert.False(wasDestroyed);
        Assert.NotNull(webView.TryGetPlatformHandle());
    }
}
