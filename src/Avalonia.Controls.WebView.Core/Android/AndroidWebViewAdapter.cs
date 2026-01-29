#if ANDROID

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Android.Graphics;
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

internal class AndroidWebViewAdapter : IWebViewAdapterWithFocus, IWebViewAdapterWithInputRedirect, IWebViewAdapterWithCookieManager, IAndroidWebViewPlatformHandle
{
    private const string PostAvWebViewMessageName = "postAvWebViewMessage";
    private static bool s_canSetDataDirectorySuffix = true;
    private readonly JavaScriptInterface _jsInterface;
    private WebView? _webView;

    public static (WebViewEngine engine, string version) GetWebViewEngineInfo()
    {
        try
        {
            // On modern Android, webview always uses Blink. WebKit is no longer available anywhere.
            return (WebViewEngine.Blink, WebView.CurrentWebViewPackage?.VersionName ?? "Unknown");
        }
        catch
        {
            return (WebViewEngine.Unknown, "Unknown");
        }
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
