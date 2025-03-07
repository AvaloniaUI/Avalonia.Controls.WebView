#if AVALONIA || WPF
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using IPlatformHandle = Avalonia.Platform.IPlatformHandle;
using Core = Avalonia.Controls;
#if WPF
using Avalonia.Controls;
using System.Windows;
using System.Windows.Threading;
using NativeControlHost = AvaloniaUI.Xpf.WpfAbstractions.NativeControlHost;
#elif AVALONIA
using Avalonia.Threading;
#endif

#if AVALONIA
namespace Avalonia.Controls
#elif WPF
namespace Avalonia.Xpf.Controls
#endif
{
    internal class NativeWebViewControlHost : NativeControlHost
    {
        private TaskCompletionSource<IWebViewAdapter> _webViewReadyCompletion = new();
        private ReparentingScope? _reparentingScope;

        static NativeWebViewControlHost()
        {
#if WPF
            FocusableProperty.OverrideMetadata(typeof(NativeWebViewControlHost), new UIPropertyMetadata(true));
#elif AVALONIA
            FocusableProperty.OverrideDefaultValue<NativeWebViewControlHost>(true);
#endif
        }

        public event EventHandler<IWebViewAdapter>? AdapterInitialized;
        public event EventHandler<IWebViewAdapter>? AdapterDeinitialized;

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            if (_reparentingScope is not null)
            {
                return _reparentingScope.ReparentRequested(parent);
            }

            IWebViewAdapter? adapter = null;

            if (OperatingSystemEx.IsMacOS() || OperatingSystemEx.IsIOS())
            {
                adapter = new Core.Macios.MaciosWebViewAdapter();
            }
            //else
            //if (OperatingSystemEx.IsLinux())
            //{
            //    adapter = new Core.Macios.GtkWebViewAdapter();
            //}
            else
            // if (OperatingSystemEx.IsBrowser())
            // {
            //     adapter = new Core.Browser.BrowserIFrameAdapter();
            // } else
#if NET6_0_OR_GREATER || NETFRAMEWORK
            if (OperatingSystemEx.IsWindows())
            {
                if (WebViewHelper.IsMsWebView2Available)
                {
                    adapter = new Core.Win.WebView2Adapter(base.CreateNativeControlCore(parent));
                }
                // else if (WebViewCapabilities.IsMsWebView1Available)
                // {
                //     adapter = new Core.Win.WebView1Adapter(base.CreateNativeControlCore(parent));
                // }
                // else if (IE Supported)
                // {
                //    adapter = new Core.Win.WebBrowserAdapter();
                // }
            }
#endif
            if (adapter is null)
            {
                return base.CreateNativeControlCore(parent);
            }

            if (adapter.IsInitialized)
            {
                WebViewAdapterOnInitialized(adapter, EventArgs.Empty);
            }
            else
            {
                adapter.Initialized += WebViewAdapterOnInitialized;
            }

            return adapter;
        }

        internal Task<IWebViewAdapter> GetAdapterAsync() => _webViewReadyCompletion.Task;

        internal IWebViewAdapter? TryGetAdapter() => _webViewReadyCompletion.Task.Status == TaskStatus.RanToCompletion ?
            _webViewReadyCompletion.Task.Result :
            null;

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            if (control is IWebViewAdapter adapter)
            {
                if (_reparentingScope is not null)
                {
                    _reparentingScope.SetDestroyingAdapter(adapter);
                    return;
                }

                Debug.Assert(!(TryGetAdapter() is { } oldAdapter && oldAdapter != adapter));

                _webViewReadyCompletion.TrySetCanceled();
                _webViewReadyCompletion = new TaskCompletionSource<IWebViewAdapter>();
                adapter.Initialized -= WebViewAdapterOnInitialized;
                AdapterDeinitialized?.Invoke(this, adapter);
                adapter.Dispose();
            }
        }

        private void WebViewAdapterOnInitialized(object? sender, EventArgs e)
        {
            var adapter = (IWebViewAdapter)sender!;
            _webViewReadyCompletion.TrySetResult(adapter);
            AdapterInitialized?.Invoke(this, adapter);
        }

        /// <inheritdoc cref="NativeWebView.BeginReparenting"/>.
        public IDisposable BeginReparenting(bool yieldOnLayoutBeforeExiting = true)
        {
            if (_reparentingScope is not null)
                throw new InvalidOperationException("Nested BeginReparenting is not allowed.");

            return _reparentingScope = new ReparentingScope(this, yieldOnLayoutBeforeExiting);
        }

        /// <inheritdoc cref="NativeWebView.BeginReparentingAsync"/>.
        public IAsyncDisposable BeginReparentingAsync()
        {
            if (_reparentingScope is not null)
                throw new InvalidOperationException("Nested BeginReparenting is not allowed.");

            return _reparentingScope = new ReparentingScope(this, true);
        }

        private sealed class ReparentingScope(NativeWebViewControlHost webView, bool yieldOnLayoutBeforeExiting) : IDisposable, IAsyncDisposable
        {
            private IWebViewAdapter? _pendingAdapter;
            public void Dispose()
            {
                var task = DisposeAsync().AsTask();
                if (!task.IsCompleted)
                {
                    var frame = new DispatcherFrame();
                    _ = task.ContinueWith(static (_, s) => ((DispatcherFrame)s).Continue = false, frame);
#if WPF
                    Dispatcher.PushFrame(frame);
#elif AVALONIA
                    Dispatcher.UIThread.PushFrame(frame);
#endif
                }

                task.GetAwaiter().GetResult();
            }

            public async ValueTask DisposeAsync()
            {
                if (_pendingAdapter is not null && yieldOnLayoutBeforeExiting)
                {
#if WPF
                    await Dispatcher.Yield(DispatcherPriority.Render);
#elif AVALONIA
                    var tcs = new TaskCompletionSource<bool>();
                    Dispatcher.UIThread.Post(_ => tcs.TrySetResult(true), DispatcherPriority.Render);
                    await tcs.Task.ConfigureAwait(false);
#endif
                }

                webView._reparentingScope = null;
                if (_pendingAdapter is not null
#if WPF
                    && PresentationSource.FromVisual(webView) is null)
#elif AVALONIA
                    && TopLevel.GetTopLevel(webView) is null)
#endif
                {
                    webView.DestroyNativeControlCore(_pendingAdapter);
                }
            }

            public void SetDestroyingAdapter(IWebViewAdapter adapter)
            {
                if (_pendingAdapter is not null)
                {
                    throw new InvalidOperationException("NativeWebView was detached second time without being attached.");
                }

                _pendingAdapter = adapter;
            }

            public IPlatformHandle ReparentRequested(IPlatformHandle parent)
            {
                if (_pendingAdapter is null)
                {
                    throw new InvalidOperationException("NativeWebView wasn't detached from previous parent.");
                }

                var currentAdapter = _pendingAdapter;
                _pendingAdapter = null;

                currentAdapter.SetParent(parent);
                return currentAdapter;
            }
        }
    }
#endif
}
