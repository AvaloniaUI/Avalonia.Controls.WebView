using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

public class HeadlessAdapterTests : HeadlessTestsBase
{
    [AvaloniaFact]
    public async Task Should_Initialize_As_Headless()
    {
        var window = new Window();
        var webView = new NativeWebView();
        window.Content = webView;
        window.Show();

        await WaitForAdapterCreation(webView);

        Assert.Equal("HeadlessWebViewAdapter", webView.TryGetPlatformHandle()?.HandleDescriptor);
    }

    [AvaloniaFact]
    public async Task Should_Delay_Adapter_Creation()
    {
        var window = new Window();
        var webView = new NativeWebView();
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var adapterCreated = false;
        webView.EnvironmentRequested += async (_, args) =>
        {
            if (args is HeadlessWebViewEnvironmentRequestedEventArgs headless)
            {
                headless.InitializeAsync = () => tcs.Task;
            }

            using var deferral = args.GetDeferral();
            await Task.Delay(10);
        };
        webView.AdapterCreated += (_, _) => adapterCreated = true;
        window.Content = webView;
        window.Show();

        Assert.False(adapterCreated);
        tcs.SetResult();

        await WaitForAdapterCreation(webView);

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

        await WaitForAdapterCreation(webView);

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

        await WaitForAdapterCreation(webView);

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

    [AvaloniaFact]
    public async Task NativeWebView_Reattach_Does_Not_Create_Duplicate_Host()
    {
        var window = new Window();
        var panel = new StackPanel();
        window.Content = panel;
        var webView = new NativeWebView();
        webView.EnvironmentRequested += (_, _) => { };
        panel.Children.Add(webView);
        window.Show();

        await WaitForAdapterCreation(webView);
        var initialVisualChildren = webView.GetVisualChildren().Count();
        Assert.Equal(1, initialVisualChildren);

        // Detach and reattach
        panel.Children.Remove(webView);
        await DoDelay();
        panel.Children.Add(webView);
        await DoDelay();

        // NativeWebView should not add a second control host to the visual tree.
        Assert.Equal(initialVisualChildren, webView.GetVisualChildren().Count());
    }

    [AvaloniaFact]
    public async Task NativeWebView_Multiple_Detach_Reattach_No_Duplicate_Hosts()
    {
        var window = new Window();
        var panel = new StackPanel();
        window.Content = panel;
        var webView = new NativeWebView();
        webView.EnvironmentRequested += (_, _) => { };
        panel.Children.Add(webView);
        window.Show();

        await WaitForAdapterCreation(webView);
        var initialVisualChildren = webView.GetVisualChildren().Count();

        for (int i = 0; i < 5; i++)
        {
            panel.Children.Remove(webView);
            await DoDelay();
            panel.Children.Add(webView);
            await DoDelay();
        }

        // No duplicate children accumulated in the visual tree
        Assert.Equal(initialVisualChildren, webView.GetVisualChildren().Count());
    }

    [AvaloniaFact]
    public async Task NativeWebView_Reattach_Navigates_To_Source_Set_While_Attached()
    {
        var (window, panel, webView) = CreateReattachableWebView();
        window.Show();

        var first = new Uri("https://first.com/");
        var second = new Uri("https://second.com/");

        webView.Source = first;
        await DoDelay();
        Assert.Equal(first, webView.Source);

        // Source is changed while the adapter is still alive, so it is applied on the adapter directly.
        webView.Source = second;
        await DoDelay();
        Assert.Equal(second, webView.Source);

        await ReattachAsync(panel, webView);

        // Recreated adapter should navigate to the last source and not to the very first one.
        Assert.Equal(second, webView.Source);
    }

    [AvaloniaFact]
    public async Task NativeWebView_Reattach_Navigates_To_Source_Set_While_Detached()
    {
        var (window, panel, webView) = CreateReattachableWebView();
        window.Show();

        var first = new Uri("https://first.com/");
        var second = new Uri("https://second.com/");

        webView.Source = first;
        await DoDelay();

        panel.Children.Remove(webView);
        await DoDelay();

        webView.Source = second;

        var adapterCreated = WaitForAdapterCreation(webView);
        panel.Children.Add(webView);
        await adapterCreated;
        await DoDelay();

        Assert.Equal(second, webView.Source);
    }

    [AvaloniaFact]
    public async Task NativeWebView_Offscreen_Host_Is_Laid_Out_After_Adapter_Creation()
    {
        var window = new Window { Width = 400, Height = 300 };
        var webView = new NativeWebView();
        webView.EnvironmentRequested += (_, _) => { };
        window.Content = webView;
        window.Show();

        await WaitForAdapterCreation(webView);
        await DoDelay();

        var host = webView.GetVisualChildren().OfType<Control>().Single();
        Assert.True(host.Bounds.Width > 0 && host.Bounds.Height > 0,
            $"Host was not laid out: {host.Bounds}, web view: {webView.Bounds}");
    }

    [AvaloniaFact]
    public async Task NativeWebView_Reattach_Creates_A_New_Adapter()
    {
        var (window, panel, webView) = CreateReattachableWebView();
        window.Show();

        await WaitForAdapterCreation(webView);
        var first = webView.TryGetPlatformHandle();
        Assert.NotNull(first);

        var destroyed = false;
        webView.AdapterDestroyed += (_, _) => destroyed = true;

        await ReattachAsync(panel, webView);

        Assert.True(destroyed);
        var second = webView.TryGetPlatformHandle();
        Assert.NotNull(second);
        Assert.NotSame(first, second);
    }

    private static (Window window, StackPanel panel, NativeWebView webView) CreateReattachableWebView()
    {
        var panel = new StackPanel();
        var window = new Window { Content = panel };
        var webView = new NativeWebView();
        webView.EnvironmentRequested += (_, _) => { };
        panel.Children.Add(webView);
        return (window, panel, webView);
    }

    private async Task ReattachAsync(Panel panel, NativeWebView webView)
    {
        panel.Children.Remove(webView);
        await DoDelay();

        var adapterCreated = WaitForAdapterCreation(webView);
        panel.Children.Add(webView);
        await adapterCreated;
        await DoDelay();
    }

    [AvaloniaFact(Skip = "Flaky test")]
    public async Task NativeWebView_NavigateToString_With_BaseUri()
    {
        var window = new Window();
        var webView = new NativeWebView();
        bool navCompleted = false;
        webView.EnvironmentRequested += (_, _) => { };
        webView.NavigationCompleted += (_, _) => navCompleted = true;
        window.Content = webView;
        window.Show();

        var baseUri = new Uri("https://example.com/");
        webView.NavigateToString("<html>test</html>", baseUri);
        await DoDelay();

        Assert.True(navCompleted);
        Assert.Equal(baseUri, webView.Source);
    }
}
