using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text;
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
    /// <summary>
    /// <see cref="EnableDevTools"/> controls whether the user is able to use the context menu or keyboard shortcuts to open the DevTools window.
    /// </summary>
    public bool EnableDevTools { get; set; }
}

public sealed class WebMessageReceivedEventArgs : EventArgs
{
    public string? Body { get; init; }
}

public sealed class WebResourceRequestedEventArgs : EventArgs
{
    public required WebViewWebResourceRequest Request { get; init; }
}

public abstract class WebViewWebRequestHeaders : IReadOnlyDictionary<string, string>
{
    public abstract int Count { get; }
    public abstract bool TrySet(string name, string value);
    public abstract bool TryRemove(string name);

    public abstract bool ContainsKey(string key);
#if NET6_0_OR_GREATER
    public abstract bool TryGetValue(string key, [MaybeNullWhen(false)] out string value);
#else
    public abstract bool TryGetValue(string key, out string value);
#endif

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
#if NET6_0_OR_GREATER
        [return: System.Diagnostics.CodeAnalysis.MaybeNull]
#endif
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
    void NavigateToString(string text);

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
    bool IsInitialized { get; }
    event EventHandler? Initialized;

    Color DefaultBackground { set; }

    void SizeChanged(PixelSize containerSize);

    void SetParent(IPlatformHandle parent);
}

internal interface IWebViewAdapterWithFocus : IWebViewAdapter
{
    bool Focus();
    bool ResignFocus();
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
    Task<IReadOnlyList<System.Net.Cookie>> GetCookiesAsync();
}

internal interface IWebViewAdapterWithOffscreenInput : IWebViewAdapter
{
    bool KeyInput(bool press, PhysicalKey physical, string? symbol, KeyModifiers modifiers);
    bool PointerInput(PointerPoint point, KeyModifiers modifiers);
    bool PointerWheelInput(Vector delta, PointerPoint point, KeyModifiers modifiers);
}

internal interface IWebViewAdapterWithOffscreenBuffer : IWebViewAdapter
{
    event Action DrawRequested;
    Task UpdateWriteableBitmap(FrameChainBase<WriteableBitmap, PixelSize>.IProducer producer);
}
