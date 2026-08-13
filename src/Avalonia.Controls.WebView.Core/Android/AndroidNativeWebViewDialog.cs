#if ANDROID
using System;
using Android.App;
using Android.Content;
using Avalonia.Android;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Controls.Android;

internal class AndroidNativeWebViewDialog(Action<WebViewEnvironmentRequestedEventArgs> environmentRequested)
    : INativeWebViewDialog
{
    private readonly string _webViewId = Guid.NewGuid().ToString();
    private bool _isOpen;
    private Context? _context;

    public event EventHandler? Closing;
    public event EventHandler<WebViewAdapterEventArgs>? AdapterCreated;
    public event EventHandler<WebViewAdapterEventArgs>? AdapterDestroyed;

    public IWebViewAdapter? TryGetAdapter() => AndroidWebViewDialogActivity.WebViewRegistry.Get(_webViewId);

    public Color DefaultBackground { get; set; } = Colors.White;
    public string? Title { get; set; }

    public bool CanUserResize { get => false; set { } }

    public void Show()
    {
        ShowCore(null);
    }

    public bool Show(IPlatformHandle? owner)
    {
        ShowCore(owner);
        return true;
    }
    
    // ReSharper disable once AsyncVoidMethod - let it flow to Dispatcher
    private async void ShowCore(IPlatformHandle? owner)
    {
        if (_isOpen)
            return;

        _context = (owner as AndroidViewControlHandle)?.View.Context ?? global::Android.App.Application.Context;
        var deferralManager = new DeferralManager();
        var args = new AndroidWebViewEnvironmentRequestedEventArgs(deferralManager);
        environmentRequested(args);
        await deferralManager.WaitForDeferralsAsync();
        var adapter = new AndroidWebViewAdapter(_context, args);
        adapter.DefaultBackground = DefaultBackground;

        AdapterCreated?.Invoke(this, new WebViewAdapterEventArgs(adapter));

        AndroidWebViewDialogActivity.WebViewRegistry.Register(_webViewId, adapter, () =>
        {
            _isOpen = false;
            Closing?.Invoke(this, EventArgs.Empty);
        });

        var intent = new Intent(_context, typeof(AndroidWebViewDialogActivity));
        intent.PutExtra(AndroidWebViewDialogActivity.WebViewIdKey, _webViewId);
        intent.PutExtra(AndroidWebViewDialogActivity.TitleIdKey, Title);

        intent.AddFlags(ActivityFlags.NoHistory);
        intent.AddFlags(ActivityFlags.ExcludeFromRecents);

        if (_context is not Activity)
        {
            intent.AddFlags(ActivityFlags.NewTask);
        }
        else
        {
            intent.AddFlags(ActivityFlags.SingleTop);
        }

        _context.StartActivity(intent);

        _isOpen = true;
    }

    public void Focus() => (TryGetAdapter() as IWebViewAdapterWithFocus)?.Focus();

    public void Close()
    {
        if (!_isOpen || _context is null) return;

        AndroidWebViewDialogActivity.ActivityRegistry.FinishActivity(_webViewId);
    }

    public bool Resize(int width, int height) => false;
    public bool Move(int x, int y) => false;

    public IPlatformHandle? TryGetPlatformHandle() => TryGetAdapter();

    public void Dispose()
    {
        Close();
    }
}
#endif
