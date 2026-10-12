using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Rendering;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using RoutedEventArgs = Avalonia.Interactivity.RoutedEventArgs;
using IPlatformHandle = Avalonia.Platform.IPlatformHandle;

namespace Avalonia.Controls;

/// <summary>
/// Environment arguments that can be used to redefine creation options for the underlying webview implementation. 
/// <list type="bullet">
///     <listheader>
///         <term>Can be one of the following</term>
///     </listheader>
///     <item><see cref="WindowsWebView2EnvironmentRequestedEventArgs"/></item>
///     <item><see cref="WindowsWebView1EnvironmentRequestedEventArgs"/></item>
///     <item><see cref="AppleWKWebViewEnvironmentRequestedEventArgs"/></item>
///     <item><see cref="GtkWebViewEnvironmentRequestedEventArgs"/></item>
///     <item><see cref="AndroidWebViewEnvironmentRequestedEventArgs"/></item>
/// </list>
/// </summary>
public abstract class WebViewEnvironmentRequestedEventArgs : EventArgs
{
    private readonly DeferralManager _deferralManager;

    private protected WebViewEnvironmentRequestedEventArgs(DeferralManager deferralManager)
    {
        _deferralManager = deferralManager;
    }

    /// <summary>
    /// <see cref="EnableDevTools"/> controls whether the user is able to use the context menu or keyboard shortcuts to open the DevTools window.
    /// </summary>
    public bool EnableDevTools { get; set; }

    /// <summary>
    /// Custom URI schemes, such as <c>app</c>, answered through <see cref="WebResourceRequestedEventArgs.Response"/>.
    /// </summary>
    /// <remarks>
    /// Registered as secure and with an authority, so <c>app://localhost</c> is an origin.
    /// Unanswered requests get a 404.
    /// Android WebView can't register schemes, answer an https host there instead,
    /// such as <c>https://appassets.androidplatform.net/</c>, whose unanswered requests go to the network.
    /// WebView2 only lets pages on these schemes fetch from or post to them, other pages can still embed them.
    /// WebView2 web views that share a user data folder need the same schemes.
    /// Adding an invalid or built-in scheme, such as <c>https</c>, throws <see cref="ArgumentException"/>.
    /// </remarks>
    public IList<string> CustomSchemes { get; } = new Utils.CustomSchemeCollection();

    /// <summary>
    /// Gets a deferral that can be used to delay the completion of the event.
    /// </summary>
    public IDisposable GetDeferral() => _deferralManager.GetDeferral();
}

public sealed class WebMessageReceivedEventArgs : EventArgs
{
    public string? Body { get; init; }
}

public sealed class WebResourceRequestedEventArgs : EventArgs
{
    private DeferralManager? _deferralManager;

    public required WebViewWebResourceRequest Request { get; init; }

    /// <summary>
    /// Gets or sets a response that the web view uses instead of loading the resource.
    /// </summary>
    /// <remarks>
    /// Applies to every request on WebView2 and Android,
    /// and to <see cref="WebViewEnvironmentRequestedEventArgs.CustomSchemes"/> on WKWebView, WebKitGTK and WPE.
    /// WebView1 ignores it.
    /// The web view disposes <see cref="WebViewWebResourceResponse.Content"/>,
    /// but not a response replaced by another handler or set after the request continued.
    /// WebView2, WebKitGTK and WPE read the whole body into memory before answering.
    /// A handler that throws leaves the request unanswered. Request bodies aren't available.
    /// </remarks>
    public WebViewWebResourceResponse? Response { get; set; }

    /// <summary>
    /// Gets a deferral that delays the request until it is disposed,
    /// so <see cref="Response"/> can be set asynchronously.
    /// </summary>
    /// <remarks>
    /// Take it before the handler's first await, a later deferral doesn't delay the request.
    /// Only requests that <see cref="Response"/> applies to wait for it.
    /// Disposing it more than once has no effect.
    /// Android WebView blocks one of its loading threads until it's disposed.
    /// </remarks>
    public IDisposable GetDeferral() => new Deferral((_deferralManager ??= new DeferralManager()).GetDeferral());

    internal Task WaitForDeferralsAsync() => _deferralManager?.WaitForDeferralsAsync() ?? Task.CompletedTask;

    // A second Dispose would release another handler's deferral.
    private sealed class Deferral(IDisposable deferral) : IDisposable
    {
        private IDisposable? _deferral = deferral;

        public void Dispose() => Interlocked.Exchange(ref _deferral, null)?.Dispose();
    }
}

/// <summary>
/// A response synthesized by the application, see <see cref="WebResourceRequestedEventArgs.Response"/>.
/// </summary>
public sealed class WebViewWebResourceResponse
{
    private readonly int _statusCode = (int)HttpStatusCode.OK;

    /// <summary>
    /// Gets the HTTP status code, from 100 to 599. Defaults to 200.
    /// </summary>
    /// <remarks>
    /// Android WebView rejects redirect codes (300-399), so they become 500 there,
    /// and WKWebView, WebKitGTK and WPE don't follow them.
    /// </remarks>
    public int StatusCode
    {
        get => _statusCode;
        init => _statusCode = value is >= 100 and <= 599 ?
            value :
            throw new ArgumentOutOfRangeException(nameof(value), value, "HTTP status codes range from 100 to 599.");
    }

    /// <summary>
    /// Gets the HTTP reason phrase. Defaults to the standard phrase of <see cref="StatusCode"/>.
    /// </summary>
    /// <remarks>
    /// WKWebView doesn't pass the reason phrase on.
    /// A blank phrase, or one that isn't printable ASCII, is replaced by the standard phrase.
    /// </remarks>
    public string? ReasonPhrase { get; init; }

    /// <summary>
    /// Gets the response headers, such as <c>Content-Type</c>. Header names are case-insensitive.
    /// </summary>
    public IDictionary<string, string> Headers { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the response body, or null for an empty body.
    /// </summary>
    public Stream? Content { get; init; }
}

public abstract class WebViewWebRequestHeaders : IReadOnlyDictionary<string, string>
{
    public abstract int Count { get; }
    public abstract bool TrySet(string name, string value);
    public abstract bool TryRemove(string name);

    public abstract bool ContainsKey(string key);
    public abstract bool TryGetValue(string key, [MaybeNullWhen(false)] out string value);

    public abstract IEnumerable<string> Keys { get; }
    public abstract IEnumerable<string> Values { get; }

    public abstract IEnumerator<KeyValuePair<string, string>> GetEnumerator();

    public string this[string key] => TryGetValue(key, out var value) ? value : string.Empty;
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class WebViewWebResourceRequest
{
    public required WebViewWebRequestHeaders Headers { get; init; }
    public required HttpMethod Method { get; init; }
    public required Uri Uri { get; init; }

    public override string ToString()
    {
        var request = new StringBuilder();
        request.AppendLine($"{Method} {Uri}");
        foreach (var pair in Headers)
        {
            request.AppendLine($"{pair.Key}: {pair.Value}");
        }
        return request.ToString();
    }
}

public class WebViewNavigationEventArgs : EventArgs
{
    public Uri? Request { get; init; }
}

public sealed class WebViewNavigationCompletedEventArgs : WebViewNavigationEventArgs
{
    public bool IsSuccess { get; init; } = true;
}

public sealed class WebViewNavigationStartingEventArgs : WebViewNavigationEventArgs
{
    public bool Cancel { get; set; }
}

public sealed class WebViewNewWindowRequestedEventArgs : WebViewNavigationEventArgs
{
    public bool Handled { get; set; }
}

public sealed class WebViewAdapterEventArgs(IPlatformHandle? platformHandle) : EventArgs
{
    /// <summary>
    /// Returns a platform handle of the native control adapter.
    /// <list type="bullet">
    ///     <listheader>
    ///         <term>Can be one of the following</term>
    ///     </listheader>
    ///     <item><see cref="IWindowsWebView2PlatformHandle"/></item>
    ///     <item><see cref="IWindowsWebView1PlatformHandle"/></item>
    ///     <item><see cref="IAppleWKWebViewPlatformHandle"/></item>
    ///     <item><see cref="IGtkWebViewPlatformHandle"/></item>
    ///     <item><see cref="IAndroidWebViewPlatformHandle"/></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Return handle can be used to access additional native APIs by using it with PInvokes. 
    /// </remarks>
    public IPlatformHandle? TryGetPlatformHandle() => platformHandle;
}

internal interface INativeWebViewDialog : IDisposable
{
    IWebViewAdapter? TryGetAdapter();

    Color DefaultBackground { set; }
    
    /// <summary>
    /// Gets or sets WebView dialog title.
    /// </summary>
    string? Title { get; set; }

    /// <summary>
    /// Gets or sets if WebView dialog is resizable by user. 
    /// </summary>
    bool CanUserResize { get; set; }

    /// <summary>
    /// Fired before WebView dialog is closed.
    /// </summary>
    event EventHandler Closing;

    /// <see cref="IWebViewHolder.AdapterCreated"/>
    event EventHandler<WebViewAdapterEventArgs>? AdapterCreated;

    /// <see cref="IWebViewHolder.AdapterDestroyed"/>
    event EventHandler<WebViewAdapterEventArgs>? AdapterDestroyed;

    /// <summary>
    /// Opens the WebView dialog.
    /// </summary>
    void Show();

    /// <summary>
    /// Opens the WebView dialog with <see cref="IPlatformHandle"/> owner.
    /// </summary>
    bool Show(IPlatformHandle owner);

    /// <summary>
    /// Closes the WebView dialog.
    /// </summary>
    void Close();

    /// <summary>
    /// Activates the dialog and moves keyboard focus to the hosted web content.
    /// </summary>
    void Focus();

    /// <summary>
    /// Resizes the WebView dialog.
    /// </summary>
    bool Resize(int width, int height);

    /// <summary>
    /// Moves the WebView dialog. Values are defined in screen coordinates.
    /// </summary>
    bool Move(int x, int y);

    IPlatformHandle? TryGetPlatformHandle();
}

internal interface IWebViewHolder
{
    /// <summary>
    ///     AdapterCreated dispatches after underlying webview adapter was initialized.
    /// </summary>
    public event EventHandler<WebViewAdapterEventArgs>? AdapterCreated;

    /// <summary>
    ///     AdapterDestroyed dispatches after underlying webview adapter was destroyed.
    /// </summary>
    public event EventHandler<WebViewAdapterEventArgs>? AdapterDestroyed;

    /// <summary>
    ///     Fired before the underlying webview adapter is created, allowing customization of the webview environment.
    /// </summary>
    /// <remarks>
    ///     Use this event to modify environment options (such as enabling private mode or dev tools) before the webview is initialized.
    ///     The event argument type depends on the platform.
    /// </remarks>
    public event EventHandler<WebViewEnvironmentRequestedEventArgs>? EnvironmentRequested;

    /// <summary>
    /// Returns instance <see cref="Avalonia.Controls.NativeWebViewCommandManager"/> that allows executing common keyboard commands. Or null, if not supported by the platform.
    /// </summary>
    NativeWebViewCommandManager? TryGetCommandManager();

    /// <summary>
    /// Returns instance <see cref="Avalonia.Controls.NativeWebViewCookieManager"/> that allows reading and settings cookies. Or null, if not supported by the platform.
    /// </summary>
    NativeWebViewCookieManager? TryGetCookieManager();

    /// <summary>
    /// Opens the print dialog to print the current web page.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Platform is not supported.</exception>
    void ShowPrintUI();

    /// <summary>
    /// Provides the Pdf data of current web page asynchronously.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">Platform is not supported.</exception>
    Task<Stream> PrintToPdfStreamAsync();

    /// <inheritdoc cref="Avalonia.Controls.WebViewAdapterEventArgs.TryGetPlatformHandle"/>
    /// <remarks>
    /// <para>Return handle can be used to access additional native APIs by using it with PInvokes.</para> 
    /// <para>Should be used together with <see cref="AdapterCreated"/> and <see cref="AdapterDestroyed"/>.</para>
    /// </remarks>
    IPlatformHandle? TryGetPlatformHandle();
}

internal interface IWebView
{
    /// <summary>
    ///     Returns true if the webview can navigate to a previous page in the navigation history via the <see cref="GoBack" />
    ///     method.
    ///     If the underlying native control is not yet initialized or navigation is not supported, this property is false.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    ///     Returns true if the webview can navigate to a next page in the navigation history via the <see cref="GoForward" />
    ///     method.
    ///     If the underlying native control is not yet initialized or navigation is not supported, this property is false.
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    ///     The Source property is the URI of the top level document of the WebView2. Setting the Source is equivalent to
    ///     calling <see cref="Navigate" />.
    /// </summary>
    Uri Source
    {
        [return: System.Diagnostics.CodeAnalysis.MaybeNull]
        get;
        set;
    }

    /// <summary>
    ///     NavigationCompleted dispatches after navigate of the top level document completes rendering either successfully
    ///     or not.
    /// </summary>
    /// <remarks>
    ///     On restricted platforms, including Browser, this event is fired only on programmatic navigation.
    /// </remarks>
    event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;

    /// <summary>
    ///     NavigationStarted dispatches before a new navigate starts for the top level document.
    /// </summary>
    event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;

    /// <summary>
    ///     NewWindowRequested dispatches before a new navigate starts for the top level document.
    /// </summary>
    event EventHandler<WebViewNewWindowRequestedEventArgs>? NewWindowRequested;

    /// <summary>
    ///     WebMessageReceived dispatches after web content sends a message to the app host via invokeCSharpAction(body).
    /// </summary>
    event EventHandler<WebMessageReceivedEventArgs> WebMessageReceived;

    /// <summary>
    /// Fires when the WebView is performing a URL request to a matching URL.
    /// </summary>
    /// <remarks>
    /// Arguments include request information, and headers dictionary.
    /// Headers dictionary can be readonly depending on the request or platform.
    /// Always check result of the `TrySet` and `TryRemove` methods.
    /// Set <see cref="WebResourceRequestedEventArgs.Response"/> to answer the request from the application.
    /// </remarks>
    event EventHandler<WebResourceRequestedEventArgs> WebResourceRequested;
    
    /// <summary>
    ///     Navigates to the previous page in navigation history.
    /// </summary>
    /// <returns>
    ///     True if successful. False if there is no page to navigate, native control is not yet initialized or
    ///     navigation is not supported
    /// </returns>
    bool GoBack();

    /// <summary>
    ///     Navigates to the next page in navigation history.
    /// </summary>
    /// <returns>
    ///     True if successful. False if there is no page to navigate, native control is not yet initialized or
    ///     navigation is not supported
    /// </returns>
    bool GoForward();

    /// <summary>
    ///     Executes the provided script in the top level document.
    /// </summary>
    Task<string?> InvokeScript(string script);

    /// <summary>
    ///     Causes a navigation of the top level document to the specified URI.
    /// </summary>
    void Navigate(Uri url);

    /// <summary>
    ///     Renders the provided HTML as the top level document.
    /// </summary>
    void NavigateToString(string text, Uri? baseUri);

    /// <summary>
    ///     Reloads the top level document.
    /// </summary>
    /// <returns>True if successful. False if not supported.</returns>
    bool Refresh();

    /// <summary>
    ///     Stops any in progress navigation.
    /// </summary>
    /// <returns>True if successful. False if not supported.</returns>
    bool Stop();
}

internal interface IWebViewAdapter : IWebView, IDisposable, IPlatformHandle
{
    Color DefaultBackground { set; }

    /// <summary>
    /// Gets or sets the user agent string used by the WebView for HTTP requests.
    /// Returns null if the platform does not support reading the user agent, or if the default user agent is in use.
    /// Setting to null resets to the default user agent.
    /// </summary>
    string? UserAgent { get; set; }

    void SizeChanged(PixelSize containerSize);

    void SetParent(IPlatformHandle parent);
}

internal interface IWebViewAdapterWithFocus : IWebViewAdapter
{
    void Focus();
    void ResignFocus();
    event EventHandler? GotFocus;
    event EventHandler<LostFocusDirection>? LostFocus;

    public enum LostFocusDirection
    {
        Unknown,
        Next,
        Previous,
    }
}

internal interface IWebViewAdapterWithInputRedirect : IWebViewAdapter
{
    event Action<RoutedEventArgs> Input;
}

internal interface IWebViewAdapterWithCommands
{
    void Copy();
    void Cut();
    void Paste();
    void SelectAll();
    void Undo();
    void Redo();
}
internal interface IWebViewAdapterWithCookieManager : IWebViewAdapter
{
    void AddOrUpdateCookie(System.Net.Cookie cookie);
    void DeleteCookie(string name, string domain, string path);
    void DeleteCookie(System.Net.Cookie cookie);
    Task<IReadOnlyList<System.Net.Cookie>> GetCookiesAsync();
}

internal interface IWebViewAdapterWithOffscreenInput : IWebViewAdapter
{
    bool KeyInput(bool press, PhysicalKey physical, string? symbol, KeyModifiers modifiers);
    bool TextInput(string text);
    bool PointerInput(PointerPoint point, int clickCount, double dpi, KeyModifiers modifiers);
    bool PointerLeaveInput(PointerPoint point, double dpi, KeyModifiers modifiers);
    bool PointerWheelInput(Vector delta, PointerPoint point, double dpi, KeyModifiers modifiers);
}

internal interface IWebViewAdapterWithOffscreenBuffer : IWebViewAdapter
{
    event Action DrawRequested;
    PixelFormat BufferPixelFormat { get; }
    AlphaFormat BufferAlphaFormat { get; }

    Task UpdateWriteableBitmap(PixelSize currentSize, FrameChainBase<WriteableBitmap, PixelSize>.IProducer producer);
}

internal interface IWebViewAdapterWithGpuSurface : IWebViewAdapter
{
    /// <summary>
    /// Returns true if the adapter can export GPU frames (DMABuf).
    /// When false (e.g. SHM mode), the compositor host should use the bitmap path instead.
    /// </summary>
    bool IsGpuExportAvailable { get; }

    /// <summary>
    /// Presents a GPU frame to the given composition surface.
    /// </summary>
    Task PresentGpuFrame(global::Avalonia.Rendering.Composition.ICompositionGpuInterop gpuInterop,
        global::Avalonia.Rendering.Composition.CompositionDrawingSurface surface);
}

internal interface IWebViewAdapterWithVisibility : IWebViewAdapter
{
    void SetVisible(bool visible);
}

internal interface IWebViewAdapterWithExplicitCursor : IWebViewAdapter
{
    StandardCursorType CurrentCursorType { get; }
    event EventHandler CursorChanged;
}

internal interface IWebViewWithPrint : IWebViewAdapter
{
    bool ShowPrintUI();
    Task<Stream> PrintToPdfStreamAsync();
}

internal interface IWebViewWithPrintWithOptions : IWebViewWithPrint
{
    Task<Stream> PrintToPdfStreamAsync(WebViewPrintSettings settings);
}
