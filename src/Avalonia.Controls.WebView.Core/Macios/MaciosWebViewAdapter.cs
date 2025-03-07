using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Controls.Macios.Interop;
using Avalonia.Controls.Macios.Interop.WebKit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Controls.Macios;

[SupportedOSPlatform("macos")]
[SupportedOSPlatform("ios")]
public class MaciosWebViewAdapter : IWebViewAdapterWithFocus, IWebViewAdapterWithInputRedirect, IWebViewAdapterWithCookieManager, IWebViewAdapterWithCommands
{
    private const string PostAvWebViewMessageName = "postAvWebViewMessage";

    private readonly NSString _postAvWebViewMessageName = NSString.Create(PostAvWebViewMessageName);
    private readonly WKWebViewConfiguration _config;
    private readonly WKWebView _webView;
    private readonly WKNavigationDelegate _navDelegate;
    private readonly WKScriptMessageHandler _scriptHandler;

    public MaciosWebViewAdapter()
    {
        _scriptHandler = new WKScriptMessageHandler();
        _scriptHandler.DidReceiveScriptMessage += OnScriptHandlerOnDidReceiveScriptMessage;

        _config = new WKWebViewConfiguration { JavaScriptEnabled = true };
        _config.AddScriptMessageHandler(_scriptHandler, _postAvWebViewMessageName);

        var enableDevTools = AvaloniaLocator.Current.GetService<WebViewOptions>()?.EnableDevTools == true;
        if (enableDevTools)
        {
            _config.EnableDeveloperExtras();
        }

        _navDelegate = new WKNavigationDelegate();
        _navDelegate.DidFinishNavigation += OnDelegateOnDidFinishNavigation;
        _navDelegate.DecidePolicyNavigation += OnDelegateOnDecidePolicyNavigation;

        _webView = new WKWebView(_config) { NavigationDelegate = _navDelegate };
        _webView.PerformKeyEquivalent += WebViewOnPerformKeyEquivalent;
        _webView.BecomeFirstResponder += OnWebViewOnBecomeFirstResponder;
        _webView.ResignFirstResponder += OnWebViewOnResignFirstResponder;

        if (OperatingSystemEx.IsIOS() && enableDevTools)
        {
            using var key = NSString.Create("inspectable");
            Libobjc.void_objc_msgSend(
                _webView.Handle,
                Libobjc.sel_getUid("setValue:forKey:"),
                NSNumber.Yes.Handle,
                key.Handle);
        }

        Handle = _webView.Retain();
        _webView.UnsafeDisown();
    }

    public IntPtr Handle { get; }
    public string HandleDescriptor => OperatingSystemEx.IsMacOS() ? "NSView" : "UIView";
    public bool IsInitialized => true;
    public event EventHandler? Initialized;

    public bool CanGoBack => _webView.CanGoBack;
    public bool CanGoForward => _webView.CanGoForward;

    public Uri Source
    {
        get
        {
            using var sourceUrl = _webView.Url;
            return Uri.TryCreate(sourceUrl?.AbsoluteString, UriKind.RelativeOrAbsolute, out var source) ?
                source :
                WebViewHelper.EmptyPage;
        }
        set => Navigate(value);
    }

    public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;
    public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
    public event EventHandler? GotFocus;
    public event EventHandler? LostFocus;
    public event Action<RoutedEventArgs>? Input;
    public bool GoBack() => _webView.GoBack() != default;
    public bool GoForward() => _webView.GoForward() != default;

    public async Task<string?> InvokeScript(string script)
    {
        try
        {
            return await _webView.EvaluateJavaScriptAsync(script);
        }
        catch (NSErrorException ex)
            when (ex is { Domain: "WKErrorDomain", Code: 4 } && ex.Data.Contains("WKJavaScriptExceptionMessage"))
        {
            throw new JavaScriptException(ex.Data["WKJavaScriptExceptionMessage"]!.ToString()!, ex);
        }
    }

    public void Navigate(Uri url)
    {
        using var request = NSURLRequest.FromUri(url);
        _ = _webView.LoadRequest(request);
    }

    public void NavigateToString(string text)
    {
        using var baseUrlStr = NSString.Create("http://localhost:12345/");
        using var baseUrl = new NSUrl(baseUrlStr);
        using var html = NSString.Create(text);
        _ = _webView.LoadHtmlString(html, baseUrl);
    }

    public bool Refresh() => _webView.Reload() != default;

    public bool Stop()
    {
        _webView.StopLoading();
        return true;
    }

    public void Dispose()
    {
        _webView.RemoveFromSuperview();

        _scriptHandler.DidReceiveScriptMessage -= OnScriptHandlerOnDidReceiveScriptMessage;
        _navDelegate.DidFinishNavigation -= OnDelegateOnDidFinishNavigation;
        _navDelegate.DecidePolicyNavigation -= OnDelegateOnDecidePolicyNavigation;
        _webView.PerformKeyEquivalent -= WebViewOnPerformKeyEquivalent;
        _webView.BecomeFirstResponder -= OnWebViewOnBecomeFirstResponder;
        _webView.ResignFirstResponder -= OnWebViewOnResignFirstResponder;

        _config.RemoveScriptMessageHandler(_postAvWebViewMessageName);
        _webView.NavigationDelegate = null;
        _webView.LoadRequest(null);

        _scriptHandler.Dispose();
        _navDelegate.Dispose();
        _config.Dispose();
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _webView.Dispose();
        }, DispatcherPriority.Background);
    }

    public void SizeChanged()
    {
    }

    public void SetParent(IPlatformHandle parent)
    {
        // no-op
        // macOS control don't need to be explicitly parented
    }

    public bool Focus() => OperatingSystemEx.IsMacOS() && _webView.MakeFirstResponder();

    public bool ResignFocus() => OperatingSystemEx.IsMacOS() && _webView.RemoveFirstResponder();

    private async void OnScriptHandlerOnDidReceiveScriptMessage(object? sender, WKScriptMessageHandler.ScriptMessageEventArgs args)
    {
        if (args.Name == PostAvWebViewMessageName)
        {
            var tcs = new TaskCompletionSource<string?>();
            var state = new WKWebView.JSCallState(_webView.Handle, tcs);
            await WKWebView.PtrResultToString(args.Body, state);
            var str = await tcs.Task;
            WebMessageReceived?.Invoke(this, new WebMessageReceivedEventArgs { Body = str });
        }
    }

    private void OnDelegateOnDecidePolicyNavigation(object? _, WKNavigationDelegate.DecidePolicyNavigationEventArgs args)
    {
        var startedArgs = new WebViewNavigationStartingEventArgs { Request = args.Request };
        NavigationStarted?.Invoke(this, startedArgs);
        args.Cancel = startedArgs.Cancel;
    }

    private async void OnDelegateOnDidFinishNavigation(object? sender, EventArgs args)
    {
        _ = await InvokeScript($"function invokeCSharpAction(data){{window.webkit.messageHandlers.{PostAvWebViewMessageName}.postMessage(data);}}");

        using var url = _webView.Url;
        NavigationCompleted?.Invoke(this, new WebViewNavigationCompletedEventArgs { Request = Uri.TryCreate(url.AbsoluteString, UriKind.Absolute, out var uri) ? uri : null, IsSuccess = true });
    }

    private void OnWebViewOnResignFirstResponder(object? o, EventArgs eventArgs)
    {
        LostFocus?.Invoke(this, EventArgs.Empty);
    }

    private void OnWebViewOnBecomeFirstResponder(object? o, EventArgs eventArgs)
    {
        GotFocus?.Invoke(this, EventArgs.Empty);
    }

    private void WebViewOnPerformKeyEquivalent(object? sender, AppleView.PerformKeyEquivalentEventArgs e)
    {
        if (!OperatingSystemEx.IsMacOS() || !_webView.IsFirstResponder)
            return;

        var chars = e.Event.CharactersIgnoringModifiers;
        var code = e.Event.KeyCode;
        var modifier = e.Event.ModifierFlags;

        if (e.Event.Type == 10 /* NSEventType.KeyDown */)
        {
            bool isCommandFlag = (modifier & NSEvent.NSEventModifierMask.CommandKeyMask) != 0;
            bool isShiftFlag = (modifier & NSEvent.NSEventModifierMask.ShiftKeyMask) != 0;

            if (isCommandFlag)
            {
                if (chars == "c")
                {
                    _webView.Copy();
                    e.Handled = true;
                }
                else if (chars == "v")
                {
                    _webView.Paste();
                    e.Handled = true;
                }
                else if (chars == "x")
                {
                    _webView.Cut();
                    e.Handled = true;
                }
                else if (chars == "a")
                {
                    _webView.SelectAll();
                    e.Handled = true;
                }
                // why charactersIgnoringModifiers didn't ignore modifiers?
                else if ((chars == "z" || chars == "Z") && isShiftFlag)
                {
                    e.Handled = _webView.Redo();
                }
                else if (chars == "z")
                {
                    e.Handled = _webView.Undo();
                }
            }

            if (e.Handled)
            {
                return;
            }
        }

        KeyModifiers modifiers = 0;
        if ((modifier & NSEvent.NSEventModifierMask.ControlKeyMask) != 0)
            modifiers |= KeyModifiers.Control;
        if ((modifier & NSEvent.NSEventModifierMask.ShiftKeyMask) != 0)
            modifiers |= KeyModifiers.Shift;
        if ((modifier & NSEvent.NSEventModifierMask.AlternateKeyMask) != 0)
            modifiers |= KeyModifiers.Alt;
        if ((modifier & NSEvent.NSEventModifierMask.CommandKeyMask) != 0)
            modifiers |= KeyModifiers.Meta;

        var physicalKey = KeyTransform.GetPhysicalKeyForCode(code);

        if (physicalKey != 0 &&
            (modifiers != 0 || physicalKey is >= PhysicalKey.F1 and <= PhysicalKey.F12))
        {
            if (modifiers != 0)
            {
                if (modifiers.HasFlag(KeyModifiers.Control))
                    _ = RaiseKeyBubbleEvent(InputElement.KeyDownEvent, PhysicalKey.ControlLeft, KeyModifiers.None);
                if (modifiers.HasFlag(KeyModifiers.Shift))
                    _ = RaiseKeyBubbleEvent(InputElement.KeyDownEvent, PhysicalKey.ShiftLeft, KeyModifiers.None);
                if (modifiers.HasFlag(KeyModifiers.Alt))
                    _ = RaiseKeyBubbleEvent(InputElement.KeyDownEvent, PhysicalKey.AltLeft, KeyModifiers.None);
                if (modifiers.HasFlag(KeyModifiers.Meta))
                    _ = RaiseKeyBubbleEvent(InputElement.KeyDownEvent, PhysicalKey.MetaLeft, KeyModifiers.None);
            }

            var handled = RaiseKeyBubbleEvent(InputElement.KeyDownEvent, physicalKey, modifiers) ||
                      RaiseKeyBubbleEvent(InputElement.KeyUpEvent, physicalKey, modifiers);

            if (modifiers != 0)
            {
                if (modifiers.HasFlag(KeyModifiers.Control))
                    _ = RaiseKeyBubbleEvent(InputElement.KeyUpEvent, PhysicalKey.ControlLeft, KeyModifiers.None);
                if (modifiers.HasFlag(KeyModifiers.Shift))
                    _ = RaiseKeyBubbleEvent(InputElement.KeyUpEvent, PhysicalKey.ShiftLeft, KeyModifiers.None);
                if (modifiers.HasFlag(KeyModifiers.Alt))
                    _ = RaiseKeyBubbleEvent(InputElement.KeyUpEvent, PhysicalKey.AltLeft, KeyModifiers.None);
                if (modifiers.HasFlag(KeyModifiers.Meta))
                    _ = RaiseKeyBubbleEvent(InputElement.KeyUpEvent, PhysicalKey.MetaLeft, KeyModifiers.None);
            }

            if (handled)
            {
                e.Handled = true;
            }
        }
    }

    private bool RaiseKeyBubbleEvent(RoutedEvent routedEvent, PhysicalKey physicalKey, KeyModifiers modifiers)
    {
        var args = new KeyEventArgs
        {
            RoutedEvent = routedEvent,
            Route = RoutingStrategies.Bubble,
            PhysicalKey = physicalKey,
            Key = physicalKey.ToQwertyKey(),
            KeyModifiers = modifiers
        };
        Dispatcher.UIThread.Invoke(() => Input?.Invoke(args), DispatcherPriority.Input);
        return args.Handled;
    }

    public void AddOrUpdateCookie(Cookie cookie)
    {
        using var cookieStore = _config.WebsiteDataStore.HttpCookieStore;
        cookieStore.SetCookie(cookie);
    }

    public void DeleteCookie(string name, string domain, string path)
    {
        using var cookieStore = _config.WebsiteDataStore.HttpCookieStore;
        cookieStore.DeleteCookie(new Cookie(name, ".", path, domain));
    }

    public async Task<IReadOnlyList<Cookie>> GetCookiesAsync()
    {
        using var cookieStore = _config.WebsiteDataStore.HttpCookieStore;
        return await cookieStore.GetAllCookies();
    }

    public void Copy() => _webView.Copy();
    public void Cut() => _webView.Cut();
    public void Paste() => _webView.Paste();
    public void SelectAll() => _webView.SelectAll();
    public void Undo() => _webView.Undo();
    public void Redo() => _webView.Redo();
}

