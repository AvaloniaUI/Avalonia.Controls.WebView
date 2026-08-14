using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Authentication;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

public class LoopbackHttpListenerTests
{
    private const string RedirectPath = "/callback";

    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    private static Func<Uri, BrowserResponse, Task> Html(string body) =>
        (_, response) =>
        {
            response.StatusCode = HttpStatusCode.OK;
            return WriteAsync(response, body);
        };

    private static async Task WriteAsync(BrowserResponse response, string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        await response.OutputStream.WriteAsync(bytes);
    }

    [Fact]
    public async Task Should_Return_Uri_With_Query_For_Matching_Path()
    {
        using var listener = new LoopbackHttpListener(0, RedirectPath);
        using var cts = new CancellationTokenSource(s_timeout);

        var waitTask = listener.WaitForCallbackAsync(null, Html("<html>ok</html>"), cts.Token);

        using var http = new HttpClient();
        using var response = await http.GetAsync(
            $"http://127.0.0.1:{listener.Port}{RedirectPath}?code=abc&state=xyz", cts.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var uri = await waitTask;
        Assert.Equal(RedirectPath, uri.AbsolutePath);
        Assert.Equal("?code=abc&state=xyz", uri.Query);
        Assert.Equal(listener.Port, uri.Port);
    }

    [Fact]
    public async Task Should_Allocate_A_Free_Port_When_Zero_Is_Requested()
    {
        using var listener = new LoopbackHttpListener(0, RedirectPath);

        Assert.NotEqual(0, listener.Port);

        // The allocated port must be the one actually accepting connections.
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, listener.Port, TestContext.Current.CancellationToken);
        Assert.True(client.Connected);
    }

    [Fact]
    public async Task Should_Send_A_Default_Success_Page_When_No_Handler_Is_Provided()
    {
        using var listener = new LoopbackHttpListener(0, RedirectPath);
        using var cts = new CancellationTokenSource(s_timeout);

        var waitTask = listener.WaitForCallbackAsync(null, null, cts.Token);

        using var http = new HttpClient();
        using var response = await http.GetAsync(
            $"http://127.0.0.1:{listener.Port}{RedirectPath}?code=abc", cts.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Authentication complete", await response.Content.ReadAsStringAsync(cts.Token));

        await waitTask;
    }

    [Fact]
    public async Task Should_Reply_404_For_Non_Matching_Path_And_Keep_Listening()
    {
        var handlerCalls = 0;

        using var listener = new LoopbackHttpListener(0, RedirectPath);
        using var cts = new CancellationTokenSource(s_timeout);

        var waitTask = listener.WaitForCallbackAsync(
            null,
            (_, response) =>
            {
                Interlocked.Increment(ref handlerCalls);
                return WriteAsync(response, "<html>ok</html>");
            },
            cts.Token);

        using var http = new HttpClient();

        // Favicon prefetch: should receive 404, listener keeps running.
        using var faviconResponse = await http.GetAsync(
            $"http://127.0.0.1:{listener.Port}/favicon.ico", cts.Token);
        Assert.Equal(HttpStatusCode.NotFound, faviconResponse.StatusCode);
        Assert.False(waitTask.IsCompleted, "Listener should keep running after 404.");

        // The user's handler must not observe requests that are not the callback.
        Assert.Equal(0, Volatile.Read(ref handlerCalls));

        // Actual callback follows.
        using var callbackResponse = await http.GetAsync(
            $"http://127.0.0.1:{listener.Port}{RedirectPath}?code=abc", cts.Token);
        Assert.Equal(HttpStatusCode.OK, callbackResponse.StatusCode);

        var uri = await waitTask;
        Assert.Equal(RedirectPath, uri.AbsolutePath);
        Assert.Equal(1, Volatile.Read(ref handlerCalls));
    }

    [Fact]
    public async Task Should_Send_Html_Body_From_Response_Handler()
    {
        using var listener = new LoopbackHttpListener(0, RedirectPath);
        using var cts = new CancellationTokenSource(s_timeout);

        var waitTask = listener.WaitForCallbackAsync(null, Html("<html>custom body</html>"), cts.Token);

        using var http = new HttpClient();
        using var response = await http.GetAsync(
            $"http://127.0.0.1:{listener.Port}{RedirectPath}?code=abc", cts.Token);

        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType?.CharSet);
        Assert.Equal("<html>custom body</html>", await response.Content.ReadAsStringAsync(cts.Token));

        await waitTask;
    }

    [Fact]
    public async Task Should_Pass_The_Callback_Uri_To_The_Response_Handler()
    {
        Uri? handlerUri = null;

        using var listener = new LoopbackHttpListener(0, RedirectPath);
        using var cts = new CancellationTokenSource(s_timeout);

        var waitTask = listener.WaitForCallbackAsync(
            null,
            (uri, response) =>
            {
                handlerUri = uri;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                return Task.CompletedTask;
            },
            cts.Token);

        using var http = new HttpClient();
        using var response = await http.GetAsync(
            $"http://127.0.0.1:{listener.Port}{RedirectPath}?code=abc", cts.Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var uri = await waitTask;
        Assert.Equal(uri, handlerUri);
    }

    [Fact]
    public async Task Should_Send_Redirect_When_Response_Is_Configured_To_Redirect()
    {
        var location = new Uri("https://example.com/signed-in");

        using var listener = new LoopbackHttpListener(0, RedirectPath);
        using var cts = new CancellationTokenSource(s_timeout);

        var waitTask = listener.WaitForCallbackAsync(
            null,
            (_, response) =>
            {
                response.Redirect(location);

                // Redirecting must select the status code on its own.
                Assert.Equal(HttpStatusCode.Found, response.StatusCode);
                return Task.CompletedTask;
            },
            cts.Token);

        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var http = new HttpClient(handler);
        using var response = await http.GetAsync(
            $"http://127.0.0.1:{listener.Port}{RedirectPath}?code=abc", cts.Token);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(location, response.Headers.Location);
        Assert.Empty(await response.Content.ReadAsStringAsync(cts.Token));

        await waitTask;
    }

    [Fact]
    public async Task Should_Ignore_Absolute_Form_Request_Target()
    {
        using var listener = new LoopbackHttpListener(0, RedirectPath);
        using var cts = new CancellationTokenSource(s_timeout);

        var waitTask = listener.WaitForCallbackAsync(null, Html("<html>ok</html>"), cts.Token);

        // An absolute-form target must not be concatenated into a spoofed callback uri.
        await SendRawRequestAsync(listener.Port,
            $"GET http://evil.com{RedirectPath}?code=abc HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n",
            cts.Token);

        Assert.False(waitTask.IsCompleted, "Absolute-form target should not complete the flow.");

        // A well formed request still completes it.
        using var http = new HttpClient();
        using var response = await http.GetAsync(
            $"http://127.0.0.1:{listener.Port}{RedirectPath}?code=real", cts.Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var uri = await waitTask;
        Assert.Equal("127.0.0.1", uri.Host);
        Assert.Equal("?code=real", uri.Query);
    }

    [Theory]
    [InlineData("evil.example")]
    [InlineData("user@evil.example")]
    [InlineData("127.0.0.1:1")]
    public async Task Should_Ignore_The_Host_Header(string host)
    {
        using var listener = new LoopbackHttpListener(0, RedirectPath);
        using var cts = new CancellationTokenSource(s_timeout);

        var waitTask = listener.WaitForCallbackAsync(null, Html("<html>ok</html>"), cts.Token);

        // Any local process can connect to the port and claim any authority it likes; the callback
        // uri handed to the application must describe the socket the request actually arrived on.
        await SendRawRequestAsync(listener.Port,
            $"GET {RedirectPath}?code=abc HTTP/1.1\r\nHost: {host}\r\n\r\n",
            cts.Token);

        var uri = await waitTask;
        Assert.Equal("127.0.0.1", uri.Host);
        Assert.Equal(listener.Port, uri.Port);
        Assert.Empty(uri.UserInfo);
        Assert.Equal("?code=abc", uri.Query);
    }

    [Fact]
    public async Task Should_Reject_A_Filtered_Callback_And_Keep_Listening()
    {
        var handlerCalls = 0;

        using var listener = new LoopbackHttpListener(0, RedirectPath);
        using var cts = new CancellationTokenSource(s_timeout);

        var waitTask = listener.WaitForCallbackAsync(
            uri => uri.Query.Contains("state=mine", StringComparison.Ordinal),
            (_, response) =>
            {
                Interlocked.Increment(ref handlerCalls);
                return WriteAsync(response, "<html>ok</html>");
            },
            cts.Token);

        using var http = new HttpClient();

        // A callback that isn't ours must be indistinguishable from a request to an unrelated path.
        using var rejected = await http.GetAsync(
            $"http://127.0.0.1:{listener.Port}{RedirectPath}?code=evil&state=theirs", cts.Token);
        Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
        Assert.False(waitTask.IsCompleted, "A rejected callback must not end the flow.");
        Assert.Equal(0, Volatile.Read(ref handlerCalls));
        Assert.Equal(1, listener.RejectedCallbackCount);

        // The real callback still completes it.
        using var accepted = await http.GetAsync(
            $"http://127.0.0.1:{listener.Port}{RedirectPath}?code=real&state=mine", cts.Token);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        var uri = await waitTask;
        Assert.Equal("?code=real&state=mine", uri.Query);
        Assert.Equal(1, Volatile.Read(ref handlerCalls));
        Assert.Equal(1, listener.RejectedCallbackCount);
    }

    [Fact]
    public async Task Should_Surface_An_Exception_Thrown_By_The_Filter()
    {
        var expected = new InvalidOperationException("bad filter");

        using var listener = new LoopbackHttpListener(0, RedirectPath);
        using var cts = new CancellationTokenSource(s_timeout);

        var waitTask = listener.WaitForCallbackAsync(_ => throw expected, null, cts.Token);

        // The listener faults before writing a response, so send raw rather than waiting on HttpClient
        // for a reply that never comes.
        await SendRawRequestAsync(listener.Port,
            $"GET {RedirectPath}?code=abc HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n",
            cts.Token);

        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => waitTask));
    }

    [Fact]
    public async Task Should_Respect_Cancellation()
    {
        using var listener = new LoopbackHttpListener(0, RedirectPath);
        using var cts = new CancellationTokenSource();

        var waitTask = listener.WaitForCallbackAsync(null, null, cts.Token);

        await cts.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitTask);
    }

    [Fact]
    public void Should_Fail_When_The_Port_Is_Already_In_Use()
    {
        using var first = new LoopbackHttpListener(0, RedirectPath);

        _ = Assert.Throws<InvalidOperationException>(
            () => new LoopbackHttpListener(first.Port, RedirectPath));
    }

    private static async Task SendRawRequestAsync(int port, string request, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port, cancellationToken);

        var stream = client.GetStream();
        var bytes = Encoding.ASCII.GetBytes(request);
        await stream.WriteAsync(bytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);

        // Read until the listener closes the connection, so the request is fully processed
        // before the assertions run.
        using var drain = new MemoryStream();
        await stream.CopyToAsync(drain, cancellationToken);
    }
}
