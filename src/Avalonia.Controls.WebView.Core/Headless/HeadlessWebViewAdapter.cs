using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Rendering;
using Avalonia.Controls.Utils;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using IPlatformHandle = Avalonia.Platform.IPlatformHandle;

namespace Avalonia.Controls.Headless;

/// <summary>
/// Headless implementation useful with unit testing.
/// Right now I am going to keep it internal, but might be a public feature at some point.
/// </summary>
internal partial class HeadlessWebViewAdapter : IWebViewAdapterWithOffscreenBuffer
{
    internal record HeadlessWebViewPage(Uri Uri)
    {
        public string? Html { get; set; }
    }

    private static int s_headlessHandleCounted;
    private readonly HeadlessWebViewEnvironmentRequestedEventArgs _environmentArgs;
    private readonly List<HeadlessWebViewPage> _history = [];
    private CancellationTokenSource? _navigationCts;
    private int _historyIndex = -1;
    private bool _disposed;

    public HeadlessWebViewAdapter(HeadlessWebViewEnvironmentRequestedEventArgs environmentArgs)
    {
        _environmentArgs = environmentArgs;

        _ = InitializeAsync();
    }

    public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;
    public event EventHandler<WebViewNewWindowRequestedEventArgs>? NewWindowRequested;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;
    public event EventHandler? Initialized;

    public bool CanGoBack => _historyIndex > 0;
    public bool CanGoForward => _historyIndex >= 0 && _historyIndex < _history.Count - 1;

    public Uri Source
    {
        get => GetCurrentPage()?.Uri ?? WebViewHelper.EmptyPage;
        set => Navigate(value);
    }

    public bool IsInitialized { get; private set; }

    public IntPtr Handle { get; } = new(Interlocked.Increment(ref s_headlessHandleCounted));
    public string HandleDescriptor => "HeadlessWebViewAdapter";

    public void SizeChanged(PixelSize containerSize)
    {
        // No-op for headless
    }

    public void SetParent(IPlatformHandle parent)
    {
        // No-op for headless
    }

    public void Dispose()
    {
        _disposed = true;
        _navigationCts?.Cancel();
        _navigationCts?.Dispose();
    }

    public async Task<string?> InvokeScript(string script)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HeadlessWebViewAdapter));
        var engine = _environmentArgs.ScriptEngine ?? DefaultScriptEngine;
        var result = await engine(script);

        switch (result?.Command)
        {
            case HeadlessWebViewEnvironmentRequestedEventArgs.ScriptResultCommand.InvokeCSharpAction:
                WebMessageReceived?.Invoke(this, new WebMessageReceivedEventArgs { Body = result.Argument });
                return null;
            case HeadlessWebViewEnvironmentRequestedEventArgs.ScriptResultCommand.OpenNewWindow:
                if (Uri.TryCreate(result.Argument, UriKind.Absolute, out var uri))
                {
                    var args = new WebViewNewWindowRequestedEventArgs { Request = uri };
                    NewWindowRequested?.Invoke(this, args);
                }
                return null;
            case HeadlessWebViewEnvironmentRequestedEventArgs.ScriptResultCommand.OpenLink:
                NavigateInternal(new HeadlessWebViewPage(new Uri(result.Argument!)), true, true);
                return null;
            case HeadlessWebViewEnvironmentRequestedEventArgs.ScriptResultCommand.GetHtmlContent:
                return GetCurrentPage()?.Html;
            default:
                return $"Executed script: {script}";
        }
    }

    public void Navigate(Uri url)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HeadlessWebViewAdapter));
        var page = new HeadlessWebViewPage(url);
        NavigateInternal(page, true, true);
    }

    public void NavigateToString(string text)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HeadlessWebViewAdapter));
        var page = new HeadlessWebViewPage(WebViewHelper.EmptyPage) { Html = text };
        NavigateInternal(page, true, true);
    }

    public bool GoBack()
    {
        if (!CanGoBack) return false;
        _historyIndex--;
        return NavigateInternal(_history[_historyIndex], false, false);
    }

    public bool GoForward()
    {
        if (!CanGoForward) return false;
        _historyIndex++;
        return NavigateInternal(_history[_historyIndex], false, false);
    }

    public bool Refresh()
    {
        var page = GetCurrentPage();
        if (page == null) return false;
        return NavigateInternal(page, false, false);
    }

    public bool Stop()
    {
        if (_navigationCts != null)
        {
            _navigationCts.Cancel();
            return true;
        }
        return false;
    }

    private async Task InitializeAsync()
    {
        if (IsInitialized) return;
        if (_environmentArgs.InitializeAsync != null)
            await _environmentArgs.InitializeAsync();
        IsInitialized = true;
        Initialized?.Invoke(this, EventArgs.Empty);
    }

    private HeadlessWebViewPage? GetCurrentPage()
        => _historyIndex >= 0 && _historyIndex < _history.Count ? _history[_historyIndex] : null;

    private bool NavigateInternal(HeadlessWebViewPage page, bool clearForwardHistory, bool appendToHistory)
    {
        _navigationCts?.Cancel();
        _navigationCts = new CancellationTokenSource();
        var ct = _navigationCts.Token;

        if (clearForwardHistory)
        {
            if (_historyIndex < _history.Count - 1)
                _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
        }

        if (appendToHistory)
        {
            _history.Add(page);
            _historyIndex = _history.Count - 1;
        }

        // 1. WebResourceRequested
        if (page.Uri != WebViewHelper.EmptyPage)
        {
            WebResourceRequested?.Invoke(this,
                new WebResourceRequestedEventArgs
                {
                    Request = new WebViewWebResourceRequest
                    {
                        Uri = page.Uri,
                        Method = System.Net.Http.HttpMethod.Get,
                        Headers = new NativeHeadersCollection(
                            new DictionaryNativeHttpRequestHeaders(new Dictionary<string, string>()))
                    }
                });
        }

        // 2. NavigationStarted
        var navArgs = new WebViewNavigationStartingEventArgs { Request = page.Uri };
        NavigationStarted?.Invoke(this, navArgs);
        if (navArgs.Cancel)
        {
            NavigationCompleted?.Invoke(this, new WebViewNavigationCompletedEventArgs { Request = page.Uri, IsSuccess = false });
            return false;
        }

        // 3. NavigationCompleted (simulate HTTP or HTML)
        Task.Run(async () =>
        {
            WebViewNavigationCompletedEventArgs? args = null;
            try
            {
                if (ct.IsCancellationRequested) return;

                if (page.Html != null)
                {
                    await Task.Delay(10, ct);
                    if (!ct.IsCancellationRequested)
                    {
                        args = new WebViewNavigationCompletedEventArgs { Request = page.Uri, IsSuccess = true };
                    }
                }
                else if (page.Uri != WebViewHelper.EmptyPage)
                {
                    var httpHandler = _environmentArgs.HttpHandler ?? DefaultHttpHandler;
                    var httpResult = await httpHandler(page.Uri);
                    if (!ct.IsCancellationRequested)
                    {
                        page.Html = httpResult.Content;

                        if (httpResult.RedirectUri is { } redirect)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                                NavigateInternal(new HeadlessWebViewPage(redirect), true, true));
                            return;
                        }

                        args = new WebViewNavigationCompletedEventArgs
                        {
                            Request = page.Uri, IsSuccess = httpResult.IsSuccess
                        };
                    }
                }
                else
                {
                    args = new WebViewNavigationCompletedEventArgs { Request = page.Uri, IsSuccess = true };
                }
            }
            catch
            {
                if (!ct.IsCancellationRequested)
                    args = new WebViewNavigationCompletedEventArgs { Request = page.Uri, IsSuccess = false };
            }

            if (args is not null)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    NavigationCompleted?.Invoke(this, args);
                    DrawRequested?.Invoke();
                });
            }
        }, ct);

        return true;
    }

    private const string JavaScriptCallRegex = """(?<func>[\w\.]+)\s*\(\s*(?:['"](?<arg1>[^'"]*)['"])*\)""";
#if NET8_0_OR_GREATER
    [GeneratedRegex(JavaScriptCallRegex)]
    private static partial Regex MatchJavaScriptCall();
#else
    private static Regex MatchJavaScriptCall() => new Regex(JavaScriptCallRegex);
#endif

    private Task<HeadlessWebViewEnvironmentRequestedEventArgs.ScriptResult?> DefaultScriptEngine(string script)
    {
        var match = MatchJavaScriptCall().Match(script);
        var func = match.Success ? match.Groups["func"].Value : "";
        var arg1 = match.Success ? match.Groups["arg1"].Value : "";

        HeadlessWebViewEnvironmentRequestedEventArgs.ScriptResult scriptResult = func switch
        {
            "window.external.invokeCSharpAction" => new(
                HeadlessWebViewEnvironmentRequestedEventArgs.ScriptResultCommand.InvokeCSharpAction, arg1),
            "window.open" => new(
                HeadlessWebViewEnvironmentRequestedEventArgs.ScriptResultCommand.OpenNewWindow, arg1),
            "window.location.replace" => new(
                HeadlessWebViewEnvironmentRequestedEventArgs.ScriptResultCommand.OpenLink, arg1),
            "getHTMLContent" => new(
                HeadlessWebViewEnvironmentRequestedEventArgs.ScriptResultCommand.GetHtmlContent, null),
            _ => new(
                HeadlessWebViewEnvironmentRequestedEventArgs.ScriptResultCommand.None, null)
        };
        return Task.FromResult<HeadlessWebViewEnvironmentRequestedEventArgs.ScriptResult?>(scriptResult);
    }

    private Task<HeadlessWebViewEnvironmentRequestedEventArgs.HttpResult> DefaultHttpHandler(Uri uri)
        => Task.FromResult(new HeadlessWebViewEnvironmentRequestedEventArgs.HttpResult(true, $"<html><body>HeadlessWebViewAdapter loaded {uri}</body></html>"));

    public event Action? DrawRequested;
    public Task UpdateWriteableBitmap(FrameChainBase<WriteableBitmap, PixelSize>.IProducer producer)
    {
        using (producer.GetNextFrame(new PixelSize(1, 1), out var frame))
        {
            using var buf = frame.Lock();
        }
        return Task.CompletedTask;
    }
}
