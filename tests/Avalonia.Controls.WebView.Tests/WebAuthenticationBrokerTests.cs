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

        var inputUri = new Uri("http://input.com");
        var middleUri = new Uri("http://middle.com");
        var outputUri = new Uri("http://localhost");
        var extraArgs = new Uri("/?code=123", UriKind.Relative);
        var options = new WebAuthenticatorOptions(inputUri, outputUri)
        {
            PreferNativeWebDialog = true,
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
                            if (uri == inputUri)
                                return new HeadlessWebViewEnvironmentRequestedEventArgs.HttpResult(
                                    true, RedirectUri: middleUri);
                            if (uri == middleUri)
                                return new HeadlessWebViewEnvironmentRequestedEventArgs.HttpResult(
                                    true, RedirectUri: new Uri(outputUri, extraArgs));
                            if (uri.ToString().StartsWith(outputUri.ToString()))
                                Assert.Fail("Final localhost request should be canceled.");
                            return new HeadlessWebViewEnvironmentRequestedEventArgs.HttpResult(false);
                        };
                    }
                };
                return dialog;
            }
        };

        var result = await WebAuthenticationBroker.AuthenticateAsync(window, options);
        Assert.Equal(new Uri(outputUri, extraArgs), result.CallbackUri);
    }
}
