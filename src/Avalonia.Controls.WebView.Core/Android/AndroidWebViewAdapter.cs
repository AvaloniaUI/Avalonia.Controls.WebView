#if ANDROID

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Android.Graphics;
using Android.Graphics.Pdf;
using Android.Print;
using Android.Print.Pdf;
using Android.Runtime;
using Android.Views;
using Android.Webkit;

using Avalonia.Android;
using Avalonia.Controls.Utils;
using Avalonia.Interactivity;
using Avalonia.Platform;

using Java.Interop;
using Java.Net;
using CookieManager = Android.Webkit.CookieManager;
using IPlatformHandle = Avalonia.Platform.IPlatformHandle;

namespace Avalonia.Controls.Android;

internal class AndroidWebViewAdapter : IWebViewAdapterWithFocus, IWebViewAdapterWithInputRedirect,
    IWebViewAdapterWithCookieManager, IAndroidWebViewPlatformHandle, IWebViewWithPrintWithOptions
{
    private const string PostAvWebViewMessageName = "postAvWebViewMessage";
    private static bool s_canSetDataDirectorySuffix = true;
    private readonly JavaScriptInterface _jsInterface;
    private WebView? _webView;

    static AndroidWebViewAdapter()
    {
        WebView.EnableSlowWholeDocumentDraw();
    }
    
    public AndroidWebViewAdapter(IPlatformHandle parent, AndroidWebViewEnvironmentRequestedEventArgs environmentArgs)
        : this(
            (parent as AndroidViewControlHandle)?.View.Context ?? global::Android.App.Application.Context,
            environmentArgs)
    {
        
    }

    public AndroidWebViewAdapter(global::Android.Content.Context parentContext, AndroidWebViewEnvironmentRequestedEventArgs environmentArgs)
    {
        if (s_canSetDataDirectorySuffix && environmentArgs.DataDirectorySuffix is { Length :> 0 } dataDirectorySuffix
            && OperatingSystem.IsAndroidVersionAtLeast(28))
        {
            WebView.SetDataDirectorySuffix(dataDirectorySuffix);
        }

        s_canSetDataDirectorySuffix = false;
        _webView = new WebView(parentContext);
        _jsInterface = new JavaScriptInterface(this);

        _webView.Settings.JavaScriptEnabled = true;
        _webView.Settings.DomStorageEnabled = environmentArgs.DomStorageEnabled;
        _webView.Settings.DatabaseEnabled = environmentArgs.DatabaseEnabled;

        _webView.Settings.CacheMode = environmentArgs.DisableCache
            ? CacheModes.NoCache
            : CacheModes.Default;

        if (environmentArgs.ApplicationNameForUserAgent is { Length: > 0 } userAgentName)
        {
            // Append the application name to the default user agent string
            _webView.Settings.UserAgentString = $"{_webView.Settings.UserAgentString} {userAgentName}";
        }

        if (environmentArgs.BuiltInZoomControls)
        {
            _webView.Settings.BuiltInZoomControls = true;
            _webView.Settings.DisplayZoomControls = false;
            _webView.Settings.SetSupportZoom(true);
            _webView.Settings.LoadWithOverviewMode = true;
            _webView.Settings.UseWideViewPort = true;
        }
        _webView.AddJavascriptInterface(_jsInterface, PostAvWebViewMessageName);
        _webView.SetWebViewClient(new AvaloniaWebViewClient(this));
        _webView.SetWebChromeClient(new WebChromeClient());

        _webView.LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.Lollipop)
        {
            global::Android.Webkit.CookieManager.Instance?.SetAcceptThirdPartyCookies(_webView, true);
        }
        if (environmentArgs.EnableDevTools)
        {
            WebView.SetWebContentsDebuggingEnabled(true);
        }
    }

    public WebView WebView => _webView ?? throw new ObjectDisposedException(nameof(AndroidWebViewAdapter));
    public IntPtr Handle => WebView.Handle;
    public string HandleDescriptor => "Android.Webkit.WebView";

    public Media.Color DefaultBackground
    {
        set
        {
            WebView.SetBackgroundColor(new Color(
                value.R, value.G, value.B, value.A));
        }
    }

    public void SizeChanged(PixelSize containerSize)
    {
        //noop
    }

    public void SetParent(IPlatformHandle parent)
    {
        //noop
    }

    public bool CanGoBack => _webView?.CanGoBack() ?? false;
    public bool CanGoForward => _webView?.CanGoForward() ?? false;

    public Uri Source
    {
        get => Uri.TryCreate(_webView?.Url, UriKind.Absolute, out var uri) ? uri : WebViewHelper.EmptyPage;
        set => Navigate(value);
    }

    public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;
    public event EventHandler<WebViewNewWindowRequestedEventArgs>? NewWindowRequested;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;
    public event EventHandler? GotFocus;
    public event EventHandler<IWebViewAdapterWithFocus.LostFocusDirection>? LostFocus;
    public event Action<RoutedEventArgs>? Input;

    public bool GoBack()
    {
        if (CanGoBack)
        {
            WebView.GoBack();
            return true;
        }
        return false;
    }

    public bool GoForward()
    {
        if (CanGoForward)
        {
            WebView.GoForward();
            return true;
        }
        return false;
    }

    public Task<string?> InvokeScript(string script)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        WebView.EvaluateJavascript(script, new AndroidJavaScriptValueCallback(tcs));
        return tcs.Task;
    }

    public void Navigate(Uri url)
    {
        NavigateCore(url, null);
    }

    public void NavigateToString(string htmlText)
    {
        NavigateCore(new Uri("http://localhost"), htmlText);
    }

    private void NavigateCore(Uri url, string? htmlText)
    {
        // WebViewClient.ShouldOverrideUrlLoading is never called for initial navigation.
        // Instead, do that manually.
        WebViewDispatcher.InvokeAsync(() =>
        {
            if (NavigationStarted is { } navigationStarted)
            {
                var args = new WebViewNavigationStartingEventArgs { Request = url };
                navigationStarted.Invoke(this, args);
                if (args.Cancel)
                    return;
            }

            if (htmlText is not null)
            {
                WebView.LoadDataWithBaseURL(url.ToString(), htmlText, "text/html", "UTF-8", null);
            }
            else
            {
                WebView.LoadUrl(url.ToString());
            }
        });
    }

    public bool Refresh()
    {
        if (_webView is null) return false;
        _webView.Reload();
        return true;
    }

    public bool Stop()
    {
        if (_webView is null) return false;
        _webView.StopLoading();
        return true;
    }

    public void Dispose()
    {
        _webView?.Dispose();
        _webView = null;
    }

    public void Focus()
    {
        _ = WebView.RequestFocus();
    }

    public void ResignFocus()
    {
        WebView.ClearFocus();
    }

    public void AddOrUpdateCookie(Cookie cookie)
    {
        var androidCookie = CookieManager.Instance;
        androidCookie?.SetAcceptCookie(true);
        androidCookie?.SetCookie(cookie.Domain, $"{cookie.Name}={cookie.Value}");
    }

    public void DeleteCookie(string name, string domain, string path)
    {
        var androidCookie = CookieManager.Instance;
        if (androidCookie != null)
        {
            // Set an expired cookie with the same name to delete it
            // Format: [name]=;domain=[domain];path=[path];expires=Thu, 01 Jan 1970 00:00:00 GMT
            var cookieString = $"{name}=;domain={domain};path={path};expires=Thu, 01 Jan 1970 00:00:00 GMT";
            androidCookie.SetCookie(domain, cookieString);

            // Ensure changes are synchronized
            androidCookie.Flush();
        }
    }

    public Task<IReadOnlyList<Cookie>> GetCookiesAsync()
    {
        var cookies = new List<Cookie>();
        var androidCookie = CookieManager.Instance;
        var cookieStr = androidCookie?.GetCookie(Source.ToString());

        if (!string.IsNullOrEmpty(cookieStr))
        {
            foreach (var cookie in cookieStr.Split(';'))
            {
                var parts = cookie.Split('=');
                if (parts.Length == 2)
                {
                    cookies.Add(new Cookie(parts[0].Trim(), parts[1].Trim()));
                }
            }
        }

        return Task.FromResult<IReadOnlyList<Cookie>>(cookies);
    }

    public bool ShowPrintUI()
    {
        if (_webView?.Context?.GetSystemService(global::Android.Content.Context.PrintService) is not PrintManager printManager)
        {
            return false;
        }

        var jobName = "Print to PDF";
        if (_webView?.CreatePrintDocumentAdapter(jobName) is not { } printAdapter)
        {
            return false;
        }

        var builder = new PrintAttributes.Builder();
        builder = builder.SetMediaSize(PrintAttributes.MediaSize.UnknownPortrait!);

        printManager.Print(jobName, printAdapter, builder.Build());
        return true;
    }

    public Task<Stream> PrintToPdfStreamAsync() => Task.FromResult(PrintToPdfStreamInternalAsync(null));
    public Task<Stream> PrintToPdfStreamAsync(WebViewPrintSettings settings) => Task.FromResult(PrintToPdfStreamInternalAsync(settings));

    private Stream PrintToPdfStreamInternalAsync(WebViewPrintSettings? settings)
    {
        if (_webView is null)
            throw new ObjectDisposedException(nameof(AndroidWebViewAdapter));

        // EnableSlowWholeDocumentDraw() is called in static constructor,
        // so Draw() will render the entire document, not just the visible portion
        
        // Width is in device pixels, ContentHeight is in CSS pixels
        // Use Scale to convert ContentHeight to device pixels
#pragma warning disable CS0618 // Scale is deprecated but still works
        var deviceScale = _webView.Scale;
#pragma warning restore CS0618
        var contentWidth = _webView.Width;
        var contentHeight = (int)(_webView.ContentHeight * deviceScale);

        if (contentWidth <= 0) contentWidth = _webView.Width;
        if (contentHeight <= 0) contentHeight = _webView.Height;

        using var bitmap = Bitmap.CreateBitmap(contentWidth, contentHeight, Bitmap.Config.Argb8888!)
            ?? throw new InvalidOperationException("Failed to create bitmap for PDF generation");
        using var canvas = new global::Android.Graphics.Canvas(bitmap);

        canvas.DrawColor(Color.White);
        _webView.Draw(canvas);


        // Calculate PDF page dimensions (in points, 72 points = 1 inch)
        // Default: Letter size (8.5 x 11 inches)
        var basePageWidth = 612; // 8.5 inches * 72
        var basePageHeight = 792; // 11 inches * 72

        var orientation = settings?.Orientation ?? WebViewPrintOrientation.Portrait;
        int pageWidth, pageHeight;
        if (orientation == WebViewPrintOrientation.Landscape)
        {
            pageWidth = basePageHeight;
            pageHeight = basePageWidth;
        }
        else
        {
            pageWidth = basePageWidth;
            pageHeight = basePageHeight;
        }

        var marginLeft = settings?.MarginLeft ?? 0;
        var marginTop = settings?.MarginTop ?? 0;
        var marginRight = settings?.MarginRight ?? 0;
        var marginBottom = settings?.MarginBottom ?? 0;

        var printableWidth = pageWidth - marginLeft - marginRight;
        var printableHeight = pageHeight - marginTop - marginBottom;

        var scaleFactor = (float)(settings?.ScaleFactor ?? 1.0);

        var pdfScale = (float)printableWidth / contentWidth * scaleFactor;
        var scaledContentHeight = (int)(contentHeight * pdfScale);

        var pageCount = (int)Math.Ceiling((double)scaledContentHeight / printableHeight);
        if (pageCount < 1) pageCount = 1;

        using var document = new PdfDocument();

        for (var i = 0; i < pageCount; i++)
        {
            var pageInfo = new PdfDocument.PageInfo.Builder(pageWidth, pageHeight, i).Create();
            var page = document.StartPage(pageInfo);

            var pdfCanvas = page!.Canvas!;

            // Apply margins
            pdfCanvas.Translate(marginLeft, marginTop);

            // Calculate the portion of content for this page (in source bitmap pixels)
            var sourceTop = (int)Math.Round(i * printableHeight / pdfScale);
            var sourceBottom = (int)Math.Round((i + 1) * printableHeight / pdfScale);
            sourceBottom = Math.Min(sourceBottom, contentHeight);
            var sourceHeight = sourceBottom - sourceTop;

            if (sourceHeight <= 0)
            {
                document.FinishPage(page);
                continue;
            }

            // Destination height should match the scaled source height
            var destHeight = (float)sourceHeight * pdfScale;

            pdfCanvas.DrawBitmap(
                bitmap!,
                new(0, sourceTop, contentWidth, sourceBottom),
                new RectF(0, 0, printableWidth, destHeight),
                null);

            document.FinishPage(page);
        }

        var ms = new MemoryStream();
        document.WriteTo(ms);
        ms.Position = 0;

        return ms;
    }

    internal static DetailedWebViewAdapterInfo GetAndroidWebViewInfo(WebViewEmbeddingScenario scenarios =
        WebViewEmbeddingScenario.NativeControlHost | WebViewEmbeddingScenario.NativeDialog)
    {
        if (!OperatingSystem.IsAndroid())
        {
            return WebViewAdapterInfo.PlatformNotSupported(WebViewAdapterType.AndroidWebView);
        }

        WebViewEngine engine;
        string? version;
        try
        {
            (engine, version) = OperatingSystem.IsAndroidVersionAtLeast(26) ?
                (WebViewEngine.Blink, WebView.CurrentWebViewPackage?.VersionName) :
                (WebViewEngine.Blink, null);
        }
        catch
        {
            (engine, version) = (WebViewEngine.Unknown, null);
        }

        return new DetailedWebViewAdapterInfo(
            WebViewAdapterType.AndroidWebView,
            engine,
            IsSupported: true,
            IsInstalled: true,
            Version: version,
            UnavailableReason: null,
            SupportedScenarios: scenarios);
    }
    
    private class JavaScriptInterface(AndroidWebViewAdapter adapter) : Java.Lang.Object
    {
        [Export("postMessage")]
        [JavascriptInterface]
        public void PostMessage(string message)
        {
            WebViewDispatcher.InvokeAsync(() =>
                adapter.WebMessageReceived?.Invoke(adapter, new WebMessageReceivedEventArgs { Body = message }));
        }
    }

#pragma warning disable CS9113 // Parameter is unread.
    private class AvaloniaWebViewClient(AndroidWebViewAdapter adapter) : WebViewClient
#pragma warning restore CS9113 // Parameter is unread.
    {
        private Uri? _lastNavigationUrl;
        private bool _lastNavigationCompleted;
        private bool _lastNavigationSuccess = true;

        public override void DoUpdateVisitedHistory(WebView? view, string? url, bool isReload)
        {
            base.DoUpdateVisitedHistory(view, url, isReload);

            if (adapter._webView is null)
                return;

            if (!string.IsNullOrEmpty(url))
            {
                var uri = new Uri(url);
                if (!WebViewHelper.IsAnchorNavigation(_lastNavigationUrl, uri) && !_lastNavigationCompleted)
                {
                    adapter.NavigationCompleted?.Invoke(adapter,
                        new WebViewNavigationCompletedEventArgs { Request = uri, IsSuccess = true });
                    _lastNavigationCompleted = true;
                    _lastNavigationUrl = uri;
                }
            }
        }

        public override WebResourceResponse? ShouldInterceptRequest(WebView? view, IWebResourceRequest? request)
        {
            if (adapter.WebResourceRequested is { } webResourceRequested)
            {
                var headers = request?.RequestHeaders;
                var canEditHeaders = headers is not null && request?.Method == "GET";
                var headersWrapper = new NativeHeadersCollection(
                    headers is not null ?
                        new DictionaryNativeHttpRequestHeaders(headers, !canEditHeaders) :
                        DictionaryNativeHttpRequestHeaders.ImmutableInstance);
                var webResourceArgs = new WebResourceRequestedEventArgs
                {
                    Request = new WebViewWebResourceRequest
                    {
                        Method = request is null ? HttpMethod.Get : new HttpMethod(request.Method!),
                        Uri = new Uri(request!.Url!.ToString()!),
                        Headers = headersWrapper,
                    }
                };

                // This flow is tricky. It's only possible to modify request headers for GET requests.
                // We also don't want to block thread with sync Invoke if not necessary.
                if (canEditHeaders)
                {
                    WebViewDispatcher.Invoke(() => webResourceRequested.Invoke(this, webResourceArgs));

                    if (headersWrapper.HasChanges)
                    {
                        try
                        {
                            var url = new URL(request.Url.ToString());
                            var connection = (HttpURLConnection)url.OpenConnection()!;

                            foreach (var header in request.RequestHeaders!)
                            {
                                connection.SetRequestProperty(header.Key, header.Value);
                            }

                            connection.Connect();

                            var encoding = connection.ContentEncoding ?? "UTF-8";
                            var mimeType = connection.ContentType?.Split(';')[0] ?? "text/html";

                            var responseHeaders = new Dictionary<string, string>();
                            foreach (var entry in connection.HeaderFields ?? new Dictionary<string, IList<string>>())
                            {
                                if (entry is { Key: { } key, Value: { } value })
                                {
                                    responseHeaders[key] = string.Join(",", value);
                                }
                            }

                            System.IO.Stream? stream = null;
                            try
                            {
                                stream = connection.InputStream;
                            }
                            catch
                            {
                                // Don't care.
                            }

                            return new WebResourceResponse(
                                mimeType,
                                encoding,
                                (int)connection.ResponseCode,
                                connection.ResponseMessage!,
                                responseHeaders,
                                stream
                            );
                        }
                        catch
                        {
                            // Don't care.
                            return null;
                        }
                    }
                }
                else
                {
                    // fallback to base.ShouldInterceptRequest
                    WebViewDispatcher.InvokeAsync(() => webResourceRequested.Invoke(this, webResourceArgs));
                }
            }

            return base.ShouldInterceptRequest(view, request);
        }

#pragma warning disable CS0672 // Member overrides obsolete member
        public override bool ShouldOverrideUrlLoading(WebView? view, string? url)
#pragma warning restore CS0672 // Member overrides obsolete member
        {
            return ShouldOverrideUrlLoading(url!, null);
        }

        public override bool ShouldOverrideUrlLoading(WebView? view, IWebResourceRequest? request)
        {
            return ShouldOverrideUrlLoading(request!.Url!.ToString()!, request);
        }

        private bool ShouldOverrideUrlLoading(string urlStr, IWebResourceRequest? request)
        {
            if (adapter._webView is null)
                return false;

            Uri? url = null;
            if (urlStr.StartsWith("data:text/html;charset=utf-8;base64,", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var base64 = urlStr.Substring("data:text/html;charset=utf-8;base64,".Length);
                    var content = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(base64));

                    // If the decoded content looks like a file URI, use it instead
                    if (content.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                    {
                        url = new Uri(content);
                    }
                }
                catch
                {
                    // Fallback to using the original URL if decoding fails
                }
            }

            url ??= new Uri(urlStr);

            if (WebViewHelper.IsAnchorNavigation(_lastNavigationUrl, url))
            {
                return false;
            }

            if (request?.IsForMainFrame == false)
            {
                var args = new WebViewNewWindowRequestedEventArgs { Request = url };
                adapter.NewWindowRequested?.Invoke(adapter, args);
                return args.Handled;
            }
            else
            {
                var args = new WebViewNavigationStartingEventArgs { Request = url };
                adapter.NavigationStarted?.Invoke(adapter, args);
                return args.Cancel;
            }
        }

        public override void OnPageStarted(WebView? view, string? url, Bitmap? favicon)
        {
            base.OnPageStarted(view, url, favicon);
            _lastNavigationSuccess = true;
            _lastNavigationCompleted = false;
        }

#pragma warning disable CS0672 // Member overrides obsolete member
        public override void OnReceivedError(WebView? view, [GeneratedEnum] ClientError errorCode, string? description, string? failingUrl)
#pragma warning restore CS0672 // Member overrides obsolete member
        {
            _lastNavigationSuccess = false;
#pragma warning disable CA1422 // Validate platform compatibility
            base.OnReceivedError(view, errorCode, description, failingUrl);
#pragma warning restore CA1422 // Validate platform compatibility
        }

        public override void OnPageFinished(WebView? view, string? url)
        {
            base.OnPageFinished(view, url);

            if (adapter._webView is null)
                return;

            adapter._webView.EvaluateJavascript(
                """
                 function invokeCSharpAction(data)
                 {
                    var message = typeof data === 'object' ? JSON.stringify(data) : data;
                    postAvWebViewMessage.postMessage(message);
                 }
                 """
                , null);

            if (!_lastNavigationCompleted)
            {
                var uri = Uri.TryCreate(url, UriKind.Absolute, out var result) ? result : null;
                adapter.NavigationCompleted?.Invoke(adapter,
                    new WebViewNavigationCompletedEventArgs { Request = uri, IsSuccess = _lastNavigationSuccess });
            }
        }
    }

    IntPtr IAndroidWebViewPlatformHandle.WebKitWebView => Handle;
}

internal class AndroidJavaScriptValueCallback(TaskCompletionSource<string?> callback) : Java.Lang.Object, IValueCallback
{
    public void OnReceiveValue(Java.Lang.Object? value)
    {
        callback.SetResult(value?.ToString());
    }
}
#endif
