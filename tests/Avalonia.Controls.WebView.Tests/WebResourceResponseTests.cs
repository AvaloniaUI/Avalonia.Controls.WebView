using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Utils;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

public class WebResourceResponseTests : HeadlessTestsBase
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [AvaloniaFact]
    public async Task Response_Replaces_The_Loaded_Content()
    {
        var (webView, completed) = CreateWebView();
        webView.WebResourceRequested += (_, e) => e.Response = Html("<html>local</html>");

        webView.Source = new Uri("https://appassets.example/index.html");
        var args = await completed.Task.WaitAsync(Timeout);

        Assert.True(args.IsSuccess);
        Assert.Equal("<html>local</html>", await webView.InvokeScript("getHTMLContent()"));
    }

    [AvaloniaFact]
    public async Task Response_Can_Be_Set_After_A_Deferral()
    {
        var (webView, completed) = CreateWebView();
        webView.WebResourceRequested += async (_, e) =>
        {
            using var deferral = e.GetDeferral();
            await Task.Delay(20);
            e.Response = Html("<html>deferred</html>");
        };

        webView.Source = new Uri("https://appassets.example/index.html");
        var args = await completed.Task.WaitAsync(Timeout);

        Assert.True(args.IsSuccess);
        Assert.Equal("<html>deferred</html>", await webView.InvokeScript("getHTMLContent()"));
    }

    [AvaloniaFact]
    public async Task Error_Status_Fails_The_Navigation()
    {
        var (webView, completed) = CreateWebView();
        webView.WebResourceRequested += (_, e) => e.Response = new WebViewWebResourceResponse { StatusCode = 500 };

        webView.Source = new Uri("https://appassets.example/index.html");

        Assert.False((await completed.Task.WaitAsync(Timeout)).IsSuccess);
    }

    [AvaloniaFact]
    public async Task Response_Content_Is_Disposed()
    {
        var (webView, completed) = CreateWebView();
        var content = new MemoryStream(Encoding.UTF8.GetBytes("<html>local</html>"));
        webView.WebResourceRequested += (_, e) => e.Response = new WebViewWebResourceResponse { Content = content };

        webView.Source = new Uri("https://appassets.example/index.html");
        await completed.Task.WaitAsync(Timeout);

        Assert.False(content.CanRead);
    }

    [AvaloniaFact]
    public async Task Response_Content_Is_Disposed_When_The_Navigation_Is_Cancelled()
    {
        var (webView, completed) = CreateWebView();
        var content = new MemoryStream(Encoding.UTF8.GetBytes("<html>local</html>"));
        webView.WebResourceRequested += (_, e) => e.Response = new WebViewWebResourceResponse { Content = content };
        webView.NavigationStarted += (_, e) => e.Cancel = true;

        webView.Source = new Uri("https://appassets.example/index.html");
        var args = await completed.Task.WaitAsync(Timeout);

        Assert.False(args.IsSuccess);
        Assert.False(content.CanRead);
    }

    [AvaloniaFact]
    public async Task Response_Content_Is_Disposed_When_Stopped_During_A_Deferral()
    {
        var (webView, completed) = CreateWebView();
        var content = new MemoryStream(Encoding.UTF8.GetBytes("<html>late</html>"));
        var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        webView.WebResourceRequested += async (_, e) =>
        {
            using var deferral = e.GetDeferral();
            requested.SetResult();
            await release.Task;
            e.Response = new WebViewWebResourceResponse { Content = content };
        };

        webView.Source = new Uri("https://appassets.example/index.html");
        await requested.Task;
        Assert.True(webView.Stop());
        release.SetResult();
        await WaitFor(() => !content.CanRead);

        Assert.False(content.CanRead);
        Assert.False(completed.Task.IsCompleted);
        // Set before the content is disposed.
        Assert.NotEqual("<html>late</html>", await webView.InvokeScript("getHTMLContent()"));
    }

    [AvaloniaFact]
    public async Task Deferred_Response_Content_Is_Disposed_On_Back_Navigation()
    {
        var (webView, _) = CreateWebView();
        var completedCount = 0;
        webView.NavigationCompleted += (_, _) => completedCount++;
        var contents = new List<MemoryStream>();
        webView.WebResourceRequested += async (_, e) =>
        {
            using var deferral = e.GetDeferral();
            await Task.Delay(20);
            var content = new MemoryStream(Encoding.UTF8.GetBytes("<html>deferred</html>"));
            contents.Add(content);
            e.Response = new WebViewWebResourceResponse { Content = content };
        };

        webView.Source = new Uri("https://appassets.example/a.html");
        await WaitFor(() => completedCount == 1);
        webView.Source = new Uri("https://appassets.example/b.html");
        await WaitFor(() => completedCount == 2);
        // The page already has HTML, so this response isn't read.
        Assert.True(webView.GoBack());
        await WaitFor(() => contents.Count == 3);

        await WaitFor(() => contents.TrueForAll(c => !c.CanRead));
        Assert.All(contents, c => Assert.False(c.CanRead));
    }

    [AvaloniaFact]
    public async Task Deferral_Disposed_Twice_Waits_For_Other_Deferrals()
    {
        var (webView, completed) = CreateWebView();
        webView.WebResourceRequested += async (_, e) =>
        {
            var deferral = e.GetDeferral();
            // After the second handler took its deferral.
            await Task.Yield();
            deferral.Dispose();
            deferral.Dispose();
        };
        webView.WebResourceRequested += async (_, e) =>
        {
            using var deferral = e.GetDeferral();
            await Task.Delay(50);
            e.Response = Html("<html>second</html>");
        };

        webView.Source = new Uri("https://appassets.example/index.html");

        Assert.True((await completed.Task.WaitAsync(Timeout)).IsSuccess);
        Assert.Equal("<html>second</html>", await webView.InvokeScript("getHTMLContent()"));
    }

    [AvaloniaFact]
    public async Task Unanswered_Custom_Scheme_Request_Fails()
    {
        var (webView, completed) = CreateWebView(e => e.CustomSchemes.Add("app"));
        var requested = false;
        webView.WebResourceRequested += (_, _) => requested = true;

        webView.Source = new Uri("app://localhost/index.html");

        Assert.False((await completed.Task.WaitAsync(Timeout)).IsSuccess);
        Assert.True(requested);
    }

    [AvaloniaFact]
    public async Task Custom_Scheme_Request_Is_Answered()
    {
        var (webView, completed) = CreateWebView(e => e.CustomSchemes.Add("app"));
        Uri? requestedUri = null;
        webView.WebResourceRequested += (_, e) =>
        {
            requestedUri = e.Request.Uri;
            e.Response = Html("<html>app</html>");
        };

        webView.Source = new Uri("app://localhost/index.html");

        Assert.True((await completed.Task.WaitAsync(Timeout)).IsSuccess);
        Assert.Equal(new Uri("app://localhost/index.html"), requestedUri);
        Assert.Equal("<html>app</html>", await webView.InvokeScript("getHTMLContent()"));
    }

    [Theory]
    [InlineData("https")]
    [InlineData("HTTP")]
    [InlineData("file")]
    [InlineData("1app")]
    [InlineData("my app")]
    [InlineData("")]
    public void Invalid_Custom_Schemes_Are_Rejected_When_Added(string scheme)
    {
        var args = new HeadlessWebViewEnvironmentRequestedEventArgs(new DeferralManager());

        Assert.Throws<ArgumentException>(() => args.CustomSchemes.Add(scheme));
        Assert.Empty(args.CustomSchemes);
    }

    [Fact]
    public void Custom_Schemes_Are_Validated_When_Replaced()
    {
        var args = new HeadlessWebViewEnvironmentRequestedEventArgs(new DeferralManager());
        args.CustomSchemes.Add("app");

        Assert.Throws<ArgumentException>(() => args.CustomSchemes[0] = "https");
        Assert.Throws<ArgumentNullException>(() => args.CustomSchemes.Add(null!));
        Assert.Equal(["app"], args.CustomSchemes);
    }

    [Fact]
    public void Custom_Schemes_Are_Lowercased_And_Sorted_Without_Duplicates()
    {
        var args = new HeadlessWebViewEnvironmentRequestedEventArgs(new DeferralManager());
        args.CustomSchemes.Add("my-app+v1.2");
        args.CustomSchemes.Add("App");
        args.CustomSchemes.Add("app");

        Assert.Equal(["app", "my-app+v1.2"], WebResourceResponseHelper.GetCustomSchemes(args));
    }

    [Fact]
    public void Response_Defaults()
    {
        var response = new WebViewWebResourceResponse { Headers = { ["content-type"] = "text/css" } };

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("OK", WebResourceResponseHelper.GetReasonPhrase(response));
        Assert.Equal("text/css", response.Headers["Content-Type"]);
        Assert.Null(response.Content);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(99)]
    [InlineData(600)]
    public void Invalid_Status_Codes_Are_Rejected(int statusCode)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WebViewWebResourceResponse { StatusCode = statusCode });
    }

    [Theory]
    [InlineData(404, null, "Not Found")]
    [InlineData(206, null, "Partial Content")]
    [InlineData(299, null, "OK")]
    [InlineData(404, "Missing", "Missing")]
    [InlineData(404, "Missing\r\nX-Injected: 1", "Not Found")]
    [InlineData(404, " ", "Not Found")]
    [InlineData(404, "Não encontrado", "Not Found")]
    public void Reason_Phrase(int statusCode, string? reasonPhrase, string expected)
    {
        var response = new WebViewWebResourceResponse { StatusCode = statusCode, ReasonPhrase = reasonPhrase };

        Assert.Equal(expected, WebResourceResponseHelper.GetReasonPhrase(response));
    }

    [Theory]
    [InlineData("text/html; charset=utf-8", "text/html", "utf-8")]
    [InlineData("application/javascript", "application/javascript", null)]
    [InlineData("text/plain; charset=\"UTF-8\"", "text/plain", "UTF-8")]
    [InlineData(null, null, null)]
    public void Content_Type_Is_Split(string? contentType, string? mediaType, string? charSet)
    {
        var response = new WebViewWebResourceResponse();
        if (contentType is not null)
            response.Headers["Content-Type"] = contentType;

        Assert.Equal((mediaType, charSet), WebResourceResponseHelper.GetContentType(response));
    }

    [Fact]
    public void Invalid_Headers_Are_Skipped()
    {
        var response = new WebViewWebResourceResponse
        {
            Headers =
            {
                ["Content-Type"] = "text/plain",
                ["X-Tab"] = "a\tb",
                ["X-Injected"] = "a\r\nAccess-Control-Allow-Origin: *",
                ["X-Null"] = null!,
                ["Bad Name"] = "a",
                ["Bad:Name"] = "a",
                [""] = "a"
            }
        };

        Assert.Equal(
            new KeyValuePair<string, string>[] { new("Content-Type", "text/plain"), new("X-Tab", "a\tb") },
            WebResourceResponseHelper.GetValidHeaders(response));
    }

    [Fact]
    public async Task Content_Is_Read_From_The_Current_Position()
    {
        var bytes = Encoding.UTF8.GetBytes("xabcdef");
        // A visible buffer at an offset, read without a copy.
        var visible = new MemoryStream(bytes, 1, 6, writable: false, publiclyVisible: true) { Position = 2 };
        var memory = new MemoryStream(Encoding.UTF8.GetBytes("abcdef")) { Position = 2 };
        var buffered = new BufferedStream(new MemoryStream(Encoding.UTF8.GetBytes("abcdef")));
        buffered.Position = 2;

        Assert.True(MemoryMarshal.TryGetArray(
            await WebResourceResponseHelper.ReadContentAsync(visible), out var segment));
        Assert.Same(bytes, segment.Array);
        Assert.Equal("cdef", Encoding.UTF8.GetString(segment));
        Assert.Equal("cdef", await ReadAsync(memory));
        Assert.Equal("cdef", await ReadAsync(buffered));
        Assert.Equal("", await ReadAsync(null));

        static async Task<string> ReadAsync(Stream? content) =>
            Encoding.UTF8.GetString((await WebResourceResponseHelper.ReadContentAsync(content)).Span);
    }

    [AvaloniaFact]
    public async Task Throwing_Handler_Leaves_The_Request_Unanswered()
    {
        var content = new MemoryStream();
        EventHandler<WebResourceRequestedEventArgs> handler = (_, e) =>
        {
            e.Response = new WebViewWebResourceResponse { Content = content };
            throw new InvalidOperationException();
        };
        var request = new WebViewWebResourceRequest
        {
            Uri = new Uri("app://localhost/index.html"),
            Method = System.Net.Http.HttpMethod.Get,
            Headers = new NativeHeadersCollection(DictionaryNativeHttpRequestHeaders.ImmutableInstance)
        };

        Assert.Null(await WebResourceResponseHelper.RaiseAsync(this, handler, request));
        Assert.False(content.CanRead);
    }

    [Theory]
    [InlineData("bytes=0-4", true, 0, 4)]
    [InlineData(" bytes=5- ", true, 5, 9)]
    [InlineData("Bytes=-3", true, 7, 9)]
    [InlineData("bytes=-20", true, 0, 9)]
    [InlineData("bytes=8-100", true, 8, 9)]
    [InlineData("bytes=10-", true, 0, -1)]
    [InlineData("bytes=-0", true, 0, -1)]
    [InlineData("bytes=99999999999999999999-", false, 0, -1)]
    [InlineData("bytes=0-1,4-5", false, 0, -1)]
    [InlineData("items=0-1", false, 0, -1)]
    [InlineData("bytes=5-2", false, 0, -1)]
    [InlineData("bytes=a-b", false, 0, -1)]
    [InlineData("bytes=+1-2", false, 0, -1)]
    [InlineData("bytes=-", false, 0, -1)]
    [InlineData("bytes=1", false, 0, -1)]
    public void Range_Is_Parsed(string range, bool parsed, long first, long last)
    {
        var parsedRange = WebResourceResponseHelper.ParseRange(range);
        Assert.Equal(parsed, parsedRange is not null);
        if (parsedRange is not null)
            Assert.Equal((first, last), WebResourceResponseHelper.GetBounds(parsedRange, 10));
    }

    [Theory]
    [InlineData("bytes=0-", 0, 3)]
    [InlineData("bytes=8-", 8, 9)]
    [InlineData("bytes=10-", 0, -1)]
    [InlineData("bytes=0-6", 0, 6)]
    [InlineData("bytes=-6", 4, 9)]
    public void Open_Ended_Range_Is_Capped(string range, long first, long last)
    {
        var parsedRange = WebResourceResponseHelper.ParseRange(range);
        Assert.NotNull(parsedRange);
        Assert.Equal((first, last), WebResourceResponseHelper.GetBounds(parsedRange, 10, 4));
    }

    [Fact]
    public async Task Capped_Range_Reports_The_Bytes_It_Sends()
    {
        var response = new WebViewWebResourceResponse { Content = new MemoryStream(Encoding.UTF8.GetBytes("abcdefghij")) };

        var partial = WebResourceResponseHelper.ApplyRange(Request(("Range", "bytes=2-")), response, maxOpenEndedLength: 3);

        Assert.Equal(206, partial.StatusCode);
        Assert.Equal("bytes 2-4/10", partial.Headers["Content-Range"]);
        Assert.Equal("3", partial.Headers["Content-Length"]);
        Assert.Equal("cde", Encoding.UTF8.GetString((await WebResourceResponseHelper.ReadContentAsync(partial.Content)).Span));
    }

    [Fact]
    public async Task Range_Is_Answered_With_Partial_Content()
    {
        var content = new MemoryStream(Encoding.UTF8.GetBytes("xabcdefghij")) { Position = 1 };
        var response = new WebViewWebResourceResponse
        {
            Headers = { ["Content-Type"] = "audio/wav", ["Content-Length"] = "10" },
            Content = content
        };

        var partial = WebResourceResponseHelper.ApplyRange(Request(("range", "bytes=2-4")), response);

        Assert.Equal(206, partial.StatusCode);
        Assert.Equal("bytes 2-4/10", partial.Headers["Content-Range"]);
        Assert.Equal("3", partial.Headers["Content-Length"]);
        Assert.Equal("audio/wav", partial.Headers["Content-Type"]);
        Assert.Equal("bytes", partial.Headers["Accept-Ranges"]);
        Assert.Equal("cde", Encoding.UTF8.GetString((await WebResourceResponseHelper.ReadContentAsync(partial.Content)).Span));
        partial.Content!.Dispose();
        Assert.False(content.CanRead);
    }

    [Theory]
    [InlineData("bytes=2-4", "abcde", "bytes 2-4/10")]
    [InlineData("bytes=-3", "hij", "bytes 7-9/10")]
    public async Task Range_Keeps_The_Bytes_The_Engine_Skips(string range, string body, string contentRange)
    {
        var response = new WebViewWebResourceResponse
        {
            Content = new MemoryStream(Encoding.UTF8.GetBytes("xabcdefghij")) { Position = 1 }
        };

        var partial = WebResourceResponseHelper.ApplyRange(
            Request(("Range", range)), response, engineSkipsToFirstByte: true);

        Assert.Equal(contentRange, partial.Headers["Content-Range"]);
        Assert.Equal(body, Encoding.UTF8.GetString((await WebResourceResponseHelper.ReadContentAsync(partial.Content)).Span));
    }

    [Fact]
    public void Unsatisfiable_Range_Is_Answered_With_416()
    {
        var content = new MemoryStream(new byte[10]);
        var response = new WebViewWebResourceResponse { Content = content };

        var unsatisfiable = WebResourceResponseHelper.ApplyRange(Request(("Range", "bytes=10-")), response);

        Assert.Equal(416, unsatisfiable.StatusCode);
        Assert.Equal("bytes */10", unsatisfiable.Headers["Content-Range"]);
        Assert.Null(unsatisfiable.Content);
        Assert.False(content.CanRead);
    }

    [Fact]
    public void Range_Is_Ignored_When_It_Cannot_Apply()
    {
        var seekable = Response(new MemoryStream(new byte[10]));
        Assert.Same(seekable, WebResourceResponseHelper.ApplyRange(Request(), seekable));
        Assert.Equal("bytes", seekable.Headers["Accept-Ranges"]);

        var notSeekable = Response(new NonSeekableStream());
        Assert.Same(notSeekable, WebResourceResponseHelper.ApplyRange(Request(("Range", "bytes=0-1")), notSeekable));
        Assert.False(notSeekable.Headers.ContainsKey("Accept-Ranges"));

        var notFound = new WebViewWebResourceResponse { StatusCode = 404, Content = new MemoryStream(new byte[10]) };
        Assert.Same(notFound, WebResourceResponseHelper.ApplyRange(Request(("Range", "bytes=0-1")), notFound));

        var post = Response(new MemoryStream(new byte[10]));
        var postRequest = new WebViewWebResourceRequest
        {
            Uri = new Uri("app://localhost/audio.wav"),
            Method = System.Net.Http.HttpMethod.Post,
            Headers = Request(("Range", "bytes=0-1")).Headers
        };
        Assert.Same(post, WebResourceResponseHelper.ApplyRange(postRequest, post));

        var encoded = Response(new MemoryStream(new byte[10]));
        encoded.Headers["Content-Encoding"] = "gzip";
        Assert.Same(encoded, WebResourceResponseHelper.ApplyRange(Request(("Range", "bytes=0-1")), encoded));

        static WebViewWebResourceResponse Response(Stream content) => new() { Content = content };
    }

    [Theory]
    [InlineData("\"v1\"", 206)]
    [InlineData("\"v2\"", 200)]
    [InlineData("W/\"v1\"", 200)]
    [InlineData("Tue, 13 Oct 2026 10:00:00 GMT", 206)]
    [InlineData("Wed, 14 Oct 2026 10:00:00 GMT", 200)]
    public void If_Range_Must_Match(string ifRange, int statusCode)
    {
        var response = new WebViewWebResourceResponse
        {
            Headers = { ["ETag"] = "\"v1\"", ["Last-Modified"] = "Tue, 13 Oct 2026 10:00:00 GMT" },
            Content = new MemoryStream(new byte[10])
        };

        var result = WebResourceResponseHelper.ApplyRange(
            Request(("Range", "bytes=0-1"), ("If-Range", ifRange)), response);

        Assert.Equal(statusCode, result.StatusCode);
    }

    private static WebViewWebResourceRequest Request(params (string Name, string Value)[] headers) => new()
    {
        Uri = new Uri("app://localhost/audio.wav"),
        Method = System.Net.Http.HttpMethod.Get,
        Headers = new NativeHeadersCollection(new DictionaryNativeHttpRequestHeaders(
            Array.ConvertAll(headers, header => new KeyValuePair<string, string>(header.Name, header.Value)), true))
    };

    private sealed class NonSeekableStream : MemoryStream
    {
        public override bool CanSeek => false;
    }

    private static WebViewWebResourceResponse Html(string html) => new()
    {
        Headers = { ["Content-Type"] = "text/html; charset=utf-8" },
        Content = new MemoryStream(Encoding.UTF8.GetBytes(html))
    };

    private static (NativeWebView WebView, TaskCompletionSource<WebViewNavigationCompletedEventArgs> Completed)
        CreateWebView(Action<WebViewEnvironmentRequestedEventArgs>? environmentRequested = null)
    {
        var window = new Window();
        var webView = new NativeWebView();
        var completed = new TaskCompletionSource<WebViewNavigationCompletedEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        webView.EnvironmentRequested += (_, e) => environmentRequested?.Invoke(e);
        webView.NavigationCompleted += (_, e) => completed.TrySetResult(e);
        window.Content = webView;
        window.Show();
        return (webView, completed);
    }
}
