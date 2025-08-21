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

using IPlatformHandle = Avalonia.Platform.IPlatformHandle;

namespace Avalonia.Controls.Android;

internal class AndroidWebViewAdapter : IWebViewAdapterWithFocus, IWebViewAdapterWithInputRedirect, IWebViewAdapterWithCookieManager, IAndroidWebViewPlatformHandle
{
    private const string PostAvWebViewMessageName = "postAvWebViewMessage";
    private readonly WebView _webView;
    private readonly JavaScriptInterface _jsInterface;

    public AndroidWebViewAdapter(IPlatformHandle parent, AndroidWebViewEnvironmentRequestedEventArgs environmentArgs)
    {
        var parentContext = (parent as AndroidViewControlHandle)?.View.Context
                            ?? global::Android.App.Application.Context;

        _webView = new WebView(parentContext);
        _jsInterface = new JavaScriptInterface(this);

        _webView.Settings.JavaScriptEnabled = true;
        _webView.Settings.DomStorageEnabled = true;
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

    public IntPtr Handle => _webView.Handle;
    public string HandleDescriptor => "Android.Webkit.WebView";
    public bool IsInitialized => true;
    public event EventHandler? Initialized;

    public Media.Color DefaultBackground
    {
        set
        {
            _webView.SetBackgroundColor(new Color(
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

    public bool CanGoBack => _webView.CanGoBack();
    public bool CanGoForward => _webView.CanGoForward();

    public Uri Source
    {
        get => Uri.TryCreate(_webView.Url, UriKind.Absolute, out var uri) ? uri : WebViewHelper.EmptyPage;
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
        if (_webView.CanGoBack())
        {
            _webView.GoBack();
            return true;
        }
        return false;
    }

    public bool GoForward()
    {
        if (_webView.CanGoForward())
        {
            _webView.GoForward();
            return true;
        }
        return false;
    }

    public Task<string?> InvokeScript(string script)
    {
        var tcs = new TaskCompletionSource<string?>();
        _webView.EvaluateJavascript(script, new AndroidJavaScriptValueCallback(tcs));
        return tcs.Task;
    }

    public void Navigate(Uri url)
    {
        _webView.LoadUrl(url.ToString());
    }

    public void NavigateToString(string text)
    {
        _webView.LoadDataWithBaseURL("http://localhost", text, "text/html", "UTF-8", null);
    }

    public bool Refresh()
    {
        _webView.Reload();
        return true;
    }

    public bool Stop()
    {
        _webView.StopLoading();
        return true;
    }

    public void Dispose()
    {
        _webView.Dispose();
    }

    public bool Focus()
    {
        return _webView.RequestFocus();
    }

    public bool ResignFocus()
    {
        _webView.ClearFocus();
        return true;
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

            if (adapter.WebResourceRequested is { } webResourceRequested)
            {
                var webResourceArgs = new WebResourceRequestedEventArgs
                {
                    Request = new WebViewWebResourceRequest
                    {
                        Method = request is null ? HttpMethod.Get : new HttpMethod(request.Method!),
                        Uri = url,
                        Headers = new NativeHeadersCollection(new DictionaryNativeHttpRequestHeaders(
                            request?.RequestHeaders?.AsReadOnly() ?? new ReadOnlyDictionary<string, string>(
                                new Dictionary<string, string>()))),
                    }
                };

                webResourceRequested.Invoke(this, webResourceArgs);
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
