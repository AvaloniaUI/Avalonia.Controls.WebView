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
    internal class NativeWebViewControlHost(WebViewAdapter.NativeHostAdapterFactory factory) : NativeControlHost, INativeWebViewControlImpl
    {
        private TaskCompletionSource<IWebViewAdapter?>? _webViewReadyCompletion;
        private ReparentingScope? _reparentingScope;

        /// <inheritdoc />
        public event EventHandler<IWebViewAdapter>? AdapterCreated;

        /// <inheritdoc />
        public event EventHandler<IWebViewAdapter>? AdapterDestroyed;

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            if (_reparentingScope is not null)
            {
                return _reparentingScope.ReparentRequested(parent);
            }

            _webViewReadyCompletion = new TaskCompletionSource<IWebViewAdapter?>();
            var adapter = factory.Invoke(parent, p => base.CreateNativeControlCore(p));
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

        /// <inheritdoc />
        public Task<IWebViewAdapter?> GetAdapterAsync() =>
            _webViewReadyCompletion?.Task ?? Task.FromResult<IWebViewAdapter?>(null);

        /// <inheritdoc />
        public IWebViewAdapter? TryGetAdapter() => _webViewReadyCompletion?.Task.Status == TaskStatus.RanToCompletion ?
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

                _webViewReadyCompletion?.TrySetCanceled();
                _webViewReadyCompletion = null;
                adapter.Initialized -= WebViewAdapterOnInitialized;
                AdapterDestroyed?.Invoke(this, adapter);
                adapter.Dispose();
            }
        }

        private void WebViewAdapterOnInitialized(object? sender, EventArgs e)
        {
            Core.WebViewDispatcher.CheckAccess();
            var adapter = (IWebViewAdapter)sender!;
            _webViewReadyCompletion?.TrySetResult(adapter);
            AdapterCreated?.Invoke(this, adapter);
        }

        /// <inheritdoc />
        public IDisposable BeginReparenting(bool yieldOnLayoutBeforeExiting = true)
        {
            if (_reparentingScope is not null)
                throw new InvalidOperationException("Nested BeginReparenting is not allowed.");

            return _reparentingScope = new ReparentingScope(this, yieldOnLayoutBeforeExiting);
        }

        /// <inheritdoc />
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
                    Core.WebViewDispatcher.PushFrameForTask(task);
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
