using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Controls.Authentication;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

/// <summary>
/// Covers the loopback browser flow directly, because <see cref="WebAuthenticationBroker"/> resolves the
/// browser launcher from <c>TopLevel.Launcher</c> and therefore cannot be driven end to end from a test.
/// </summary>
public class SystemBrowserWebAuthenticationBrokerTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    private static readonly Uri s_defaultRedirectUri = new("http://127.0.0.1/callback");

    [Fact]
    public async Task Should_Complete_Flow_And_Preserve_The_Other_Request_Parameters()
    {
        var requestUri = new Uri(
            "http://input.com/authorize?client_id=abc&scope=openid&state=xyz&redirect_uri=" +
            Uri.EscapeDataString(s_defaultRedirectUri.AbsoluteUri));

        Uri? launchedUri = null;

        var callbackUri = await SystemBrowserWebAuthenticationBroker.AuthenticateAsync(
            requestUri,
            s_defaultRedirectUri,
            s_timeout,
            async uri =>
            {
                launchedUri = uri;
                await GetAsync(RedirectUriOf(uri) + "?code=123&state=xyz");
                return true;
            },
            null,
            TestContext.Current.CancellationToken);

        Assert.NotNull(launchedUri);

        // The dynamically allocated port must be pushed into the request's redirect_uri...
        var actualRedirectUri = new Uri(RedirectUriOf(launchedUri));
        Assert.Equal("127.0.0.1", actualRedirectUri.Host);
        Assert.Equal("/callback", actualRedirectUri.AbsolutePath);
        Assert.NotEqual(80, actualRedirectUri.Port);

        // ...without dropping the rest of the authorization request.
        var query = HttpUtility.ParseQueryString(launchedUri.Query);
        Assert.Equal("abc", query["client_id"]);
        Assert.Equal("openid", query["scope"]);
        Assert.Equal("xyz", query["state"]);
        Assert.Equal("input.com", launchedUri.Host);
        Assert.Equal("/authorize", launchedUri.AbsolutePath);

        Assert.Equal("/callback", callbackUri.AbsolutePath);
        Assert.Equal("?code=123&state=xyz", callbackUri.Query);
        Assert.Equal(actualRedirectUri.Port, callbackUri.Port);
    }

    [Fact]
    public async Task Should_Bind_The_Explicit_Port_Of_The_Redirect_Uri()
    {
        var port = GetFreePort();
        var redirectUri = new Uri($"http://127.0.0.1:{port}/callback");
        var requestUri = new Uri("http://input.com/authorize?client_id=abc");

        Uri? launchedUri = null;

        var callbackUri = await SystemBrowserWebAuthenticationBroker.AuthenticateAsync(
            requestUri,
            redirectUri,
            s_timeout,
            async uri =>
            {
                launchedUri = uri;
                await GetAsync($"{redirectUri}?code=123");
                return true;
            },
            null,
            TestContext.Current.CancellationToken);

        // An explicit port needs no coercion, so the request uri is passed through untouched.
        Assert.Equal(requestUri, launchedUri);
        Assert.Equal(port, callbackUri.Port);
        Assert.Equal("?code=123", callbackUri.Query);
    }

    [Fact]
    public async Task Should_Send_The_Response_Produced_By_The_Response_Handler()
    {
        Uri? handlerUri = null;
        string? body = null;

        var callbackUri = await SystemBrowserWebAuthenticationBroker.AuthenticateAsync(
            new Uri("http://input.com/authorize?redirect_uri=" +
                    Uri.EscapeDataString(s_defaultRedirectUri.AbsoluteUri)),
            s_defaultRedirectUri,
            s_timeout,
            async uri =>
            {
                body = await GetAsync(RedirectUriOf(uri) + "?code=123");
                return true;
            },
            async (uri, response) =>
            {
                handlerUri = uri;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("<html>done</html>"));
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("<html>done</html>", body);
        Assert.Equal(callbackUri, handlerUri);
    }

    [Theory]
    [InlineData("https://127.0.0.1:5000/callback")]
    [InlineData("http://example.com/callback")]
    [InlineData("myapp://callback")]
    public async Task Should_Reject_Non_Loopback_Http_Redirect_Uri(string redirectUri)
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => SystemBrowserWebAuthenticationBroker.AuthenticateAsync(
                new Uri("http://input.com"),
                new Uri(redirectUri),
                s_timeout,
                _ => throw new InvalidOperationException("Browser should not be launched."),
                null,
                TestContext.Current.CancellationToken));

        Assert.Equal("redirectUri", exception.ParamName);
    }

    [Fact]
    public async Task Should_Throw_When_The_Request_Uri_Has_No_Redirect_Uri_To_Coerce()
    {
        // The redirect uri has no explicit port, so the request's redirect_uri has to be rewritten
        // with the dynamically allocated one - and there is nothing to rewrite here.
        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SystemBrowserWebAuthenticationBroker.AuthenticateAsync(
                new Uri("http://input.com/authorize?client_id=abc"),
                s_defaultRedirectUri,
                s_timeout,
                _ => throw new InvalidOperationException("Browser should not be launched."),
                null,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Should_Throw_When_The_Browser_Cannot_Be_Launched()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SystemBrowserWebAuthenticationBroker.AuthenticateAsync(
                new Uri("http://input.com/authorize?client_id=abc"),
                new Uri($"http://127.0.0.1:{GetFreePort()}/callback"),
                s_timeout,
                _ => Task.FromResult(false),
                null,
                TestContext.Current.CancellationToken));

        Assert.Contains("system browser", exception.Message);
    }

    [Fact]
    public async Task Should_Wrap_The_Exception_Thrown_By_The_Launcher()
    {
        var expected = new NotSupportedException("no browser here");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SystemBrowserWebAuthenticationBroker.AuthenticateAsync(
                new Uri("http://input.com/authorize?client_id=abc"),
                new Uri($"http://127.0.0.1:{GetFreePort()}/callback"),
                s_timeout,
                _ => throw expected,
                null,
                TestContext.Current.CancellationToken));

        Assert.Same(expected, exception.InnerException);
    }

    [Fact]
    public async Task Should_Cancel_When_The_Callback_Times_Out()
    {
        // The user never completes the flow in the browser.
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => SystemBrowserWebAuthenticationBroker.AuthenticateAsync(
                new Uri("http://input.com/authorize?client_id=abc"),
                new Uri($"http://127.0.0.1:{GetFreePort()}/callback"),
                TimeSpan.FromMilliseconds(200),
                _ => Task.FromResult(true),
                null,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Should_Cancel_When_The_Caller_Cancels()
    {
        using var cts = new CancellationTokenSource();

        var task = SystemBrowserWebAuthenticationBroker.AuthenticateAsync(
            new Uri("http://input.com/authorize?client_id=abc"),
            new Uri($"http://127.0.0.1:{GetFreePort()}/callback"),
            s_timeout,
            _ =>
            {
                cts.Cancel();
                return Task.FromResult(true);
            },
            null,
            cts.Token);

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);

        // Caller cancellation must not be reported as a timeout.
        Assert.DoesNotContain("Timed out", exception.Message);
    }

    [Fact]
    public async Task Should_Release_The_Port_When_The_Flow_Fails()
    {
        var port = GetFreePort();
        var redirectUri = new Uri($"http://127.0.0.1:{port}/callback");

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => SystemBrowserWebAuthenticationBroker.AuthenticateAsync(
                new Uri("http://input.com/authorize?client_id=abc"),
                redirectUri,
                TimeSpan.FromMilliseconds(200),
                _ => Task.FromResult(true),
                null,
                TestContext.Current.CancellationToken));

        // The listener must not keep the port bound after the flow is over.
        using var listener = new LoopbackHttpListener(port, "/callback");
        Assert.Equal(port, listener.Port);
    }

    private static string RedirectUriOf(Uri requestUri) =>
        HttpUtility.ParseQueryString(requestUri.Query)["redirect_uri"] ??
        throw new InvalidOperationException("The launched uri has no redirect_uri.");

    private static async Task<string> GetAsync(string uri)
    {
        using var http = new HttpClient();
        using var response = await http.GetAsync(uri);
        return await response.Content.ReadAsStringAsync();
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
