using System;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

public class WebAuthenticationBrokerTests : HeadlessTestsBase
{
    [AvaloniaFact(Timeout = 10_000)]
    public async Task Should_Complete_Auth_Workflow()
    {
        var window = new Window();
        window.Show();

#pragma warning disable CA1416
        var options = CreateDialogOptions() with { Mode = WebAuthenticatorMode.NativeWebDialog };
#pragma warning restore CA1416

        var result = await WebAuthenticationBroker.AuthenticateAsync(window, options);
        Assert.Equal(ExpectedCallbackUri, result.CallbackUri);
    }

    // The completion path of WebAuthenticatorMode.Browser is covered by
    // SystemBrowserWebAuthenticationBrokerTests: the broker takes its launcher from TopLevel.Launcher,
    // which cannot be substituted, so only the paths that reject before launching are testable here.

    [AvaloniaTheory(Timeout = 10_000)]
    [InlineData("https://127.0.0.1:5000/callback")]
    [InlineData("http://example.com/callback")]
    [InlineData("myapp://callback")]
    public async Task Should_Reject_Non_Loopback_Http_Redirect_Uri(string redirectUri)
    {
        var window = new Window();
        window.Show();

        var options = new WebAuthenticatorOptions(new Uri("http://input.com"), new Uri(redirectUri))
        {
            Mode = WebAuthenticatorMode.Browser,
            BrowserOptions = new BrowserOptions
            {
                ResponseHandler = (_, _) => throw new InvalidOperationException("Callback should not be received.")
            }
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => WebAuthenticationBroker.AuthenticateAsync(window, options));
        Assert.Equal("redirectUri", exception.ParamName);
    }

    [AvaloniaFact(Timeout = 30_000)]
    public async Task Should_Throw_When_The_Browser_Cannot_Be_Launched()
    {
        var window = new Window();
        window.Show();

        // The headless platform exposes no ILauncher, so LaunchUriAsync reports failure.
        var options = new WebAuthenticatorOptions(
            new Uri("http://input.com"), new Uri($"http://127.0.0.1:{GetFreePort()}/callback"))
        {
            Mode = WebAuthenticatorMode.Browser,
            BrowserOptions = new BrowserOptions { Timeout = TimeSpan.FromSeconds(5) }
        };

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => WebAuthenticationBroker.AuthenticateAsync(window, options));
    }

    private static readonly Uri s_inputUri = new("http://input.com");
    private static readonly Uri s_middleUri = new("http://middle.com");
    private static readonly Uri s_outputUri = new("http://localhost");
    private static readonly Uri s_extraArgs = new("/?code=123", UriKind.Relative);

    private static Uri ExpectedCallbackUri => new(s_outputUri, s_extraArgs);

    private static WebAuthenticatorOptions CreateDialogOptions() =>
        new(s_inputUri, s_outputUri)
        {
            NativeWebDialogFactory = () =>
            {
                var dialog = new NativeWebDialog();
                dialog.EnvironmentRequested += (_, args) =>
                {
                    if (args is HeadlessWebViewEnvironmentRequestedEventArgs headless)
                    {
                        // Mock HttpHandler to simulate chain of redirections.
                        // Importantly, last redirect should include extra callback parameters.
                        headless.HttpHandler = async uri =>
                        {
                            await Task.Delay(10);
                            if (uri == s_inputUri)
                                return new HeadlessWebViewEnvironmentRequestedEventArgs.HttpResult(
                                    true, RedirectUri: s_middleUri);
                            if (uri == s_middleUri)
                                return new HeadlessWebViewEnvironmentRequestedEventArgs.HttpResult(
                                    true, RedirectUri: new Uri(s_outputUri, s_extraArgs));
                            if (uri.ToString().StartsWith(s_outputUri.ToString()))
                                Assert.Fail("Final localhost request should be canceled.");
                            return new HeadlessWebViewEnvironmentRequestedEventArgs.HttpResult(false);
                        };
                    }
                };
                return dialog;
            }
        };

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
