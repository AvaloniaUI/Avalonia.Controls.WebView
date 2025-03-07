#if DISABLE
using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Browser;

namespace Avalonia.Controls.WebView.Core.Browser
{
    [SupportedOSPlatform("browser")]
    internal class BrowserIFrameAdapter : JSObjectControlHandle, IWebViewAdapter
    {
        private static readonly Lazy<Task> _importModule =
                                                                                                                                                                                                                                                                                                                                                                                                                       new(() => JSHost.ImportAsync("avwebview.js", "avwebview.js"));
        private Action? _subscriptions;
        private Uri? _lastSrc;

        public BrowserIFrameAdapter() : base(WebViewInterop.CreateElement("iframe"))
        {
            Initialize();
        }

        private async void Initialize()
        {
            await _importModule.Value;

            var unsub = WebViewInterop.Subscribe(Object,
                src => NavigationCompleted?.Invoke(this, new WebViewNavigationCompletedEventArgs
                {
                    Request = Uri.TryCreate(src, UriKind.Absolute, out var request) ? request : null
                }));

            _subscriptions = unsub;

            IsInitialized = true;
            Initialized?.Invoke(this, EventArgs.Empty);
        }
        
        public bool IsInitialized { get; private set; }
        public void SizeChanged() { }

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

        public event EventHandler? Initialized;
        public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
        public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;

        public bool GoBack() => WebViewInterop.GoBack(Object);

        public bool GoForward() => WebViewInterop.GoForward(Object);

        public void HandleResize(PixelSize newSize) { }

        public Task<string?> InvokeScript(string script)
        {
            return WebViewInterop.Eval(Object, script);
        }

        public void Navigate(Uri url)
        {
            _lastSrc = url;
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
    }
}
#endif
