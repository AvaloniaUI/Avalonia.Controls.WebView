#if ANDROID

using System;
using System.Collections.Generic;
using Android.Webkit;
using Java.Interop;
using System.Net;
using System.Threading.Tasks;
using Android.Content;
using Avalonia.Android;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using IPlatformHandle = Avalonia.Platform.IPlatformHandle;

namespace Avalonia.Controls.Android;

internal class AndroidWebViewAdapter : IWebViewAdapterWithFocus, IWebViewAdapterWithInputRedirect, IWebViewAdapterWithCookieManager, IAndroidWebViewPlatformHandle
{
    private const string PostAvWebViewMessageName = "postAvWebViewMessage";
    private readonly WebView _webView;
    private readonly JavaScriptInterface _jsInterface;

    public AndroidWebViewAdapter(IPlatformHandle parent)
    {
        var parentContext = (parent as AndroidViewControlHandle)?.View.Context
                            ?? global::Android.App.Application.Context;

        _webView = new WebView(parentContext);
        _jsInterface = new JavaScriptInterface(this);

        _webView.Settings.JavaScriptEnabled = true;
        _webView.Settings.DomStorageEnabled = true;
        _webView.AddJavascriptInterface(_jsInterface, PostAvWebViewMessageName);
        _webView.SetWebViewClient(new AvaloniaWebViewClient(this));
        _webView.SetWebChromeClient(new WebChromeClient());

        var enableDevTools = AvaloniaLocator.Current.GetService<WebViewOptions>()?.EnableDevTools == true;
        if (enableDevTools)
        {
            WebView.SetWebContentsDebuggingEnabled(true);
        }
    }

    public IntPtr Handle => _webView.Handle;
    public string HandleDescriptor => "Android.Webkit.WebView";
    public bool IsInitialized => true;
    public event EventHandler? Initialized;

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
    public event EventHandler? GotFocus;
    public event EventHandler? LostFocus;
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
        var androidCookie = global::Android.Webkit.CookieManager.Instance;
        androidCookie?.SetAcceptCookie(true);
        androidCookie?.SetCookie(cookie.Domain, $"{cookie.Name}={cookie.Value}");
    }

    public void DeleteCookie(string name, string domain, string path)
    {
        var androidCookie = global::Android.Webkit.CookieManager.Instance;
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
        var androidCookie = global::Android.Webkit.CookieManager.Instance;
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
            _ = Dispatcher.UIThread.InvokeAsync(() =>
                adapter.WebMessageReceived?.Invoke(adapter, new WebMessageReceivedEventArgs { Body = message }));
        }
    }

    private class AvaloniaWebViewClient(AndroidWebViewAdapter adapter) : WebViewClient
    {
        public override bool ShouldOverrideUrlLoading(WebView? view, IWebResourceRequest? request)
        {
            if (request?.IsForMainFrame == false)
            {
                var args = new WebViewNewWindowRequestedEventArgs { Request = new Uri(request!.Url!.ToString()!) };
                adapter.NewWindowRequested?.Invoke(adapter, args);
                return args.Handled;
            }
            else
            {
                var args = new WebViewNavigationStartingEventArgs { Request = new Uri(request!.Url!.ToString()!) };
                adapter.NavigationStarted?.Invoke(adapter, args);
                return args.Cancel;
            }
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

            var uri = Uri.TryCreate(url, UriKind.Absolute, out var result) ? result : null;
            adapter.NavigationCompleted?.Invoke(adapter,
                new WebViewNavigationCompletedEventArgs { Request = uri, IsSuccess = true });
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
