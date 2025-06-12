using System;
using Avalonia.Platform;

namespace Avalonia.Controls;

internal static class WebViewAdapter
{
    public static bool UseHeadless { get; set; }

    public abstract record AdapterFactory;

    public delegate IWebViewAdapter CreateNativeWebViewAdapter(IPlatformHandle parent, Func<IPlatformHandle, IPlatformHandle> createChild);
    public record NativeHostAdapterFactory(CreateNativeWebViewAdapter Invoke) : AdapterFactory;

    public delegate IWebViewAdapterWithOffscreenBuffer CreateOffscreenWebViewAdapter(Control parent);
    public record CompositorHostAdapterFactory(CreateOffscreenWebViewAdapter Invoke) : AdapterFactory;

    public static AdapterFactory? CreateFactory(Action<WebViewEnvironmentRequestedEventArgs> environmentRequested)
    {
        if (UseHeadless)
        {
            var args = new HeadlessWebViewEnvironmentRequestedEventArgs();
            environmentRequested(args);
            // Headless platform doesn't support NativeControlHost yet,
            // But compositor solution kinda works there.
            // Even though it's not production ready part of WebView (compositor/offscreen impl is not enabled by default).
            return new CompositorHostAdapterFactory(_ =>
                new Headless.HeadlessWebViewAdapter(args));
        }

#if ANDROID // Android is the only backend which conditionally compiled, the rest is always present and loaded in runtime
            // TODO: I would like to avoid Xamarin.Android here (as an extra target), and make it in-line with the rest. 
        if (OperatingSystem.IsAndroid())
        {
            var args = new AndroidWebViewEnvironmentRequestedEventArgs();
            return new NativeHostAdapterFactory((parent, _) => new Android.AndroidWebViewAdapter(parent, args));
        }
#else
        if (OperatingSystemEx.IsMacOS() || OperatingSystemEx.IsIOS())
        {
            var args = new AppleWKWebView2EnvironmentRequestedEventArgs();
            environmentRequested(args);
            return new NativeHostAdapterFactory((_, _) =>
            {
                // NOTE: we add double platform condition here to shut up Roslyn Analyzer false positives.
                // If you are from the future, please check if it's still the case.
                if (OperatingSystemEx.IsMacOS() || OperatingSystemEx.IsIOS())
                    return new Macios.MaciosWebViewAdapter(args);
                throw new PlatformNotSupportedException();
            });
        }

        if (OperatingSystemEx.IsWindows())
        {
            var args = new WindowsWebView2EnvironmentRequestedEventArgs();
            environmentRequested(args);
            if (Win.WebView2.CoreWebView2Environment.TryFindWebView2Runtime(args.BrowserExecutableFolder)
                != IntPtr.Zero)
            {
                return new NativeHostAdapterFactory((parent, createChild) =>
                {
                    if (OperatingSystemEx.IsWindows())
                        return new Win.WebView2.WebView2HwndAdapter(createChild(parent), args);
                    throw new PlatformNotSupportedException();
                });
            }
            if (Win.WebView1.WebView1Adapter.IsAvailable)
            {
                return new NativeHostAdapterFactory((parent, createChild) =>
                {
                    if (OperatingSystemEx.IsWindows())
                        return new Win.WebView1.WebView1Adapter(createChild(parent));
                    throw new PlatformNotSupportedException();
                });
            }
        }

        if (OperatingSystemEx.IsLinux())
        {
            var args = new GtkWebViewEnvironmentRequestedEventArgs();
            environmentRequested(args);
            if (args.ExperimentalOffscreen)
            {
                return new CompositorHostAdapterFactory(parent =>
                    new Gtk.GtkOffscreenAvaloniaWebViewAdapter(args, parent));
            }

            return new NativeHostAdapterFactory((parent, _) => new Gtk.GtkX11WebViewAdapter(args, parent));
        }

        // if (OperatingSystemEx.IsBrowser())
        // {
        //     new Core.Browser.BrowserIFrameAdapter();
        // }
#endif

        return null;
    }
}
