using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Avalonia.Media;
using Avalonia.Platform;
using IPlatformHandle = Avalonia.Platform.IPlatformHandle;

#if BROWSER
namespace Avalonia.Controls.Browser;

/// <summary>
/// Native implementation that creates _blank window dialog with iframe in it.
/// Nested iframe is bridged by BrowserIFrameAdapter, returned from <see cref="TryGetAdapter"/>.
/// Window is materialized when Show is called.
/// </summary>
[SupportedOSPlatform("browser")]
internal class BrowserWindowNativeWebViewDialog(Action<WebViewEnvironmentRequestedEventArgs> environmentRequested)
    : INativeWebViewDialog
{
    private JSObject? _popup;
    private IWebViewAdapter? _adapter;
    private Action? _unsubClose;
    private string? _title;
    private Color _defaultBackground = Colors.White;
    private bool _disposed;

    public IWebViewAdapter? TryGetAdapter() => _adapter;

    public Color DefaultBackground
    {
        set
        {
            _defaultBackground = value;
            if (_adapter is { } adapter)
                adapter.DefaultBackground = value;
        }
    }

    public string? Title
    {
        get => _title;
        set
        {
            _title = value;
            if (_popup is { } popup)
                WebViewInterop.SetDialogTitle(popup, value);
        }
    }

    public bool CanUserResize { get; set; }

    public event EventHandler? Closing;
    public event EventHandler<WebViewAdapterEventArgs>? AdapterCreated;
    public event EventHandler<WebViewAdapterEventArgs>? AdapterDestroyed;

    public async void Show()
    {
        if (_disposed) return;

        try
        {
            var deferralManager = new DeferralManager();
            var envArgs = new BrowserWebViewEnvironmentRequestedEventArgs(deferralManager);
            environmentRequested(envArgs);
            await deferralManager.WaitForDeferralsAsync();

            var results = WebViewInterop.OpenDialogWindow(_title, 800, 600);
            (_popup, var iframe) = (results[0], results[1]);

            _unsubClose = WebViewInterop.SubscribeDialogClose(_popup, OnPopupClosed);

            var adapterImpl = await BrowserIFrameAdapter.CreateFromIframe(iframe, envArgs);
            _adapter = adapterImpl;

            _adapter.DefaultBackground = _defaultBackground;
            AdapterCreated?.Invoke(this, new WebViewAdapterEventArgs(_adapter));
        }
        catch (Exception)
        {
            if (_popup is { } popup)
            {
                WebViewInterop.CloseDialogWindow(popup);
                _popup = null;
            }
            _adapter = null;
            AdapterDestroyed?.Invoke(this, new WebViewAdapterEventArgs(_adapter));
            throw;
        }
    }

    public bool Show(IPlatformHandle owner)
    {
        Show();
        return true;
    }

    public void Close()
    {
        if (_popup is { } popup)
        {
            Closing?.Invoke(this, EventArgs.Empty);
            DestroyAdapter();
            WebViewInterop.CloseDialogWindow(popup);
            _popup = null;
        }
    }

    public bool Resize(int width, int height)
    {
        if (_popup is { } popup)
            return WebViewInterop.ResizeDialogWindow(popup, width, height);
        return false;
    }

    public bool Move(int x, int y)
    {
        if (_popup is { } popup)
            return WebViewInterop.MoveDialogWindow(popup, x, y);
        return false;
    }

    public IPlatformHandle? TryGetPlatformHandle() => _adapter as IPlatformHandle;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        DestroyAdapter();
        _unsubClose?.Invoke();
        _unsubClose = null;

        if (_popup is { } popup)
        {
            WebViewInterop.CloseDialogWindow(popup);
            _popup = null;
        }
    }

    private void OnPopupClosed()
    {
        Closing?.Invoke(this, EventArgs.Empty);
        DestroyAdapter();
        _unsubClose?.Invoke();
        _unsubClose = null;
        _popup = null;
    }

    private void DestroyAdapter()
    {
        if (_adapter is { } adapter)
        {
            _adapter = null;
            AdapterDestroyed?.Invoke(this, new WebViewAdapterEventArgs(adapter));
            adapter.Dispose();
        }
    }
}
#endif
