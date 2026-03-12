#if BROWSER
using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Browser;
using Avalonia.Media;
using Avalonia.Platform;
using WebViewInterop = Avalonia.Controls.Browser.WebViewInterop;

namespace Avalonia.Controls.WebView.Core.Browser
{
    [SupportedOSPlatform("browser")]
    internal class BrowserIFrameAdapter : JSObjectControlHandle, IWebViewAdapter
    {
        private static readonly Lazy<Task> _importModule =
            new(() => WebViewInterop.EnsureLoaded());

        private Action? _subscriptions;
        private Uri? _lastSrc;

        public BrowserIFrameAdapter() : base(WebViewInterop.CreateElement("iframe"))
        {
            InitializeTask = InitializeAsync();
        }

        internal Task InitializeTask { get; }

        private async Task InitializeAsync()
        {
            await _importModule.Value;

            var unsub = WebViewInterop.Subscribe(Object,
                src => NavigationCompleted?.Invoke(this, new WebViewNavigationCompletedEventArgs
                {
                    Request = Uri.TryCreate(src, UriKind.Absolute, out var request) ? request : null
                }));

            _subscriptions = unsub;
        }

        public Color DefaultBackground { set { } }

        public void SizeChanged(PixelSize containerSize) { }

        public void SetParent(IPlatformHandle parent) { }

        public bool CanGoBack => WebViewInterop.CanGoBack(Object);

        public bool CanGoForward => false;

        public Uri Source
        {
            get
            {
                if (Uri.TryCreate(WebViewInterop.GetActualLocation(Object), UriKind.Absolute, out var location))
                {
                    return location;
                }
                return _lastSrc!;
            }
            set { Navigate(value); }
        }

        public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
        public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;
        public event EventHandler<WebViewNewWindowRequestedEventArgs>? NewWindowRequested;
        public event EventHandler<WebMessageReceivedEventArgs>? WebMessageReceived;
        public event EventHandler<WebResourceRequestedEventArgs>? WebResourceRequested;

        public bool GoBack() => WebViewInterop.GoBack(Object);

        public bool GoForward() => WebViewInterop.GoForward(Object);

        public Task<string?> InvokeScript(string script)
        {
            return WebViewInterop.Eval(Object, script);
        }

        public void Navigate(Uri url)
        {
            _lastSrc = url;
            NavigationStarted?.Invoke(this, new WebViewNavigationStartingEventArgs { Request = url });
            Object.SetProperty("src", url.AbsoluteUri);
        }

        public void NavigateToString(string text)
        {
            _lastSrc = new Uri("about:srcdoc");
            Object.SetProperty("srcdoc", text);
        }

        public bool Refresh()
        {
            return WebViewInterop.Refresh(Object);
        }

        public bool Stop()
        {
            return WebViewInterop.Stop(Object);
        }

        public void Dispose()
        {
            _subscriptions?.Invoke();
        }

        internal static DetailedWebViewAdapterInfo GetBrowserInfo()
        {
            return new DetailedWebViewAdapterInfo(
                WebViewAdapterType.BrowserIFrame,
                WebViewEngine.Unknown,
                IsSupported: OperatingSystem.IsBrowser(),
                IsInstalled: OperatingSystem.IsBrowser(),
                Version: null,
                UnavailableReason: OperatingSystem.IsBrowser() ? null : "Not running in a browser environment.",
                SupportedScenarios: WebViewEmbeddingScenario.NativeControlHost);
        }
    }
}
#endif
