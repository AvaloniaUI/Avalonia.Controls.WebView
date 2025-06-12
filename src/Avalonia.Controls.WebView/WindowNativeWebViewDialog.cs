using System;
using Core = Avalonia.Controls;
using Avalonia.Platform;
#if WPF
using System.Windows;
using AvaloniaUI.Xpf.WpfAbstractions;
using WindowStartupLocation = System.Windows.WindowStartupLocation;
using Window = System.Windows.Window;
#endif

#if AVALONIA
namespace Avalonia.Controls
#elif WPF
namespace Avalonia.Xpf.Controls
#endif
{
    internal class WindowNativeWebViewDialog : Window, Core.INativeWebViewDialog
    {
        private readonly INativeWebViewControlImpl _controlHostImpl;
        private EventHandler? _closing;

        public WindowNativeWebViewDialog(Core.WebViewAdapter.AdapterFactory? adapterFactory)
        {
            _controlHostImpl = adapterFactory switch
            {
#if !WPF
                Core.WebViewAdapter.CompositorHostAdapterFactory comp => new NativeWebViewCompositorHost(comp),
#endif
                Core.WebViewAdapter.NativeHostAdapterFactory native => new NativeWebViewControlHost(native),
                _ => new EmptyNativeWebViewControlImpl()
            };

            _controlHostImpl.AdapterInitialized += (_, adapter) => AdapterInitialized?.Invoke(this, new Core.WebViewAdapterEventArgs(adapter));
            _controlHostImpl.AdapterDestroyed += (_, adapter) => AdapterDestroyed?.Invoke(this, new Core.WebViewAdapterEventArgs(adapter));

            Content = _controlHostImpl;

            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Closing += (_, args) =>
            {
                _closing?.Invoke(this, args);
            };
        }

#if WPF
        public bool CanUserResize { get => ResizeMode != ResizeMode.NoResize; set => ResizeMode = value ? ResizeMode.CanResize : ResizeMode.NoResize; }
#elif AVALONIA
        public bool CanUserResize { get => CanResize; set => CanResize = value; }
#endif

        public Core.IWebViewAdapter? TryGetAdapter() => _controlHostImpl.TryGetAdapter();
        public void Dispose() {}

        event EventHandler? Core.INativeWebViewDialog.Closing
        {
            add => _closing += value;
            remove => _closing -= value;
        }

        public event EventHandler<Core.WebViewAdapterEventArgs>? AdapterInitialized;
        public event EventHandler<Core.WebViewAdapterEventArgs>? AdapterDestroyed;

        bool Core.INativeWebViewDialog.Show(IPlatformHandle _) => false;

        public bool Resize(int width, int height)
        {
            Width = width;
            Height = height;
            return true;
        }

        public bool Move(int x, int y)
        {
#if WPF
            Left = x;
            Top = y;
#elif AVALONIA
            Position = new PixelPoint(x, y);
#endif
            if (!IsVisible)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
            }
            return true;
        }

#if WPF
        public IPlatformHandle? TryGetPlatformHandle() => XpfWpfAbstraction.GetAvaloniaTopLevelForWindow(this)?.TryGetPlatformHandle();
#endif
    }
}
