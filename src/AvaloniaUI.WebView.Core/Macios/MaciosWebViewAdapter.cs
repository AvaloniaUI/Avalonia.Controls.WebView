using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using AppleInterop;
using AppleInterop.WebKit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;

namespace AvaloniaUI.WebView.Macios;

[SupportedOSPlatform("macos")]
[SupportedOSPlatform("ios")]
public class MaciosWebViewAdapter : IWebViewAdapterWithFocus, IWebViewAdapterWithInputRedirect
{
    private const string PostAvWebViewMessageName = "postAvWebViewMessage";

    private readonly WKWebViewConfiguration _config;
    private readonly WKWebView _webView;
    private readonly WKNavigationDelegate _navDelegate;
    private readonly WKScriptMessageHandler _scriptHandler;

    public MaciosWebViewAdapter()
    {
        _scriptHandler = new WKScriptMessageHandler();
        _scriptHandler.DidReceiveScriptMessage += (sender, args) =>
        {
            if (args.Name == PostAvWebViewMessageName)
            {
                WebMessageReceived?.Invoke(this, new WebMessageReceivedEventArgs { Body = args.Body });
            }
        };

        _config = new WKWebViewConfiguration { JavaScriptEnabled = true };
        _config.AddScriptMessageHandler(_scriptHandler, PostAvWebViewMessageName);

        _navDelegate = new WKNavigationDelegate();
        _navDelegate.DidFinishNavigation += async (sender, args) =>
        {
            _ = await InvokeScript(
                $"function invokeCSharpAction(data){{window.webkit.messageHandlers.{PostAvWebViewMessageName}.postMessage(data);}}");

            using var url = _webView!.Url;
            NavigationCompleted?.Invoke(this,
                new WebViewNavigationCompletedEventArgs
                {
                    Request = Uri.TryCreate(url.AbsoluteString, UriKind.Absolute, out var uri) ? uri : null,
                    IsSuccess = true
                });
        };
        _navDelegate.DecidePolicyNavigation += (_, args) =>
        {
            var startedArgs = new WebViewNavigationStartingEventArgs { Request = args.Request };
            NavigationStarted?.Invoke(this, startedArgs);
            args.Cancel = startedArgs.Cancel;
        };

        _webView = new WKWebView(_config) { NavigationDelegate = _navDelegate };
        _webView.PerformKeyEquivalent += WebViewOnPerformKeyEquivalent;
        _webView.BecomeFirstResponder += (_, _) => GotFocus?.Invoke(this, EventArgs.Empty);
        _webView.ResignFirstResponder += (_, _) => LostFocus?.Invoke(this, EventArgs.Empty); 
    }

    public IntPtr Handle => _webView.Handle;
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
            return Uri.TryCreate(sourceUrl.AbsoluteString, UriKind.RelativeOrAbsolute, out var source) ?
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

    public Task<string?> InvokeScript(string script) => _webView.EvaluateJavaScriptAsync(script);

    public void Navigate(Uri url)
    {
        using var nsStr = NSString.Create(url.ToString());
        using var nsUrl = new NSUrl(nsStr);
        using var request = new NSURLRequest(nsUrl);
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
        _webView.NavigationDelegate = null;
        _webView.Dispose();
        _config.Dispose();
        _navDelegate.Dispose();
    }

    public void SizeChanged()
    {
    }

    public void SetParent(IPlatformHandle parent)
    {
        // no-op
        // macOS control don't need to be explicitly parented
    }

    public bool Focus()
    {
        return OperatingSystemEx.IsMacOS() && _webView.MakeFirstResponder();
    }

    public bool ResignFocus()
    {
        return OperatingSystemEx.IsMacOS() && _webView.RemoveFirstResponder();
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
}

