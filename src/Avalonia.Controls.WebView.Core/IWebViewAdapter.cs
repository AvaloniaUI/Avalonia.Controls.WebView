using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Rendering;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using RoutedEventArgs = Avalonia.Interactivity.RoutedEventArgs;
using IPlatformHandle = Avalonia.Platform.IPlatformHandle;

namespace Avalonia.Controls;

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
    /// <summary>
    /// Gets WebView instance hosted inside the dialog.
    /// </summary>
    IWebView WebView { get; }

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

    /// <summary>
    /// Gets platform handle of the dialog itself.
    /// </summary>
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
    ///     NavigationStarting dispatches before a new navigate starts for the top level document.
    /// </summary>
    event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;

    /// <summary>
    ///     NavigationStarting dispatches before a new navigate starts for the top level document.
    /// </summary>
    event EventHandler<WebViewNewWindowRequestedEventArgs>? NewWindowRequested;

    /// <summary>
    ///     WebMessageReceived dispatches after web content sends a message to the app host via invokeCSharpAction(body).
    /// </summary>
    event EventHandler<WebMessageReceivedEventArgs> WebMessageReceived;

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

    void SizeChanged(PixelSize containerSize);

    void SetParent(IPlatformHandle parent);
}

internal interface IWebViewAdapterWithFocus : IWebViewAdapter
{
    bool Focus();
    bool ResignFocus();
    event EventHandler? GotFocus;
    event EventHandler? LostFocus;
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
