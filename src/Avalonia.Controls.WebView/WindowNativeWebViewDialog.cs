#if !ANDROID && !BROWSER
using System;
using System.Threading.Tasks;
using Core = Avalonia.Controls;
using Avalonia.Platform;
using Color = Avalonia.Media.Color;
using Colors = Avalonia.Media.Colors;
#if WPF
using System.Windows;
using System.Windows.Media;
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
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    internal class WindowNativeWebViewDialog : Window, Core.INativeWebViewDialog
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        private INativeWebViewControlImpl? _controlHostImpl;
        private Color? _initialDefaultBackground;
        private EventHandler? _closing;

        public WindowNativeWebViewDialog(Task<Core.WebViewAdapter.AdapterFactory?> adapterFactory)
        {
            CompleteAdapter();
            
#if WPF
            SizeChanged += OnSizeChanged;
            LocationChanged += OnLocationOrStateChanged;
            StateChanged += OnLocationOrStateChanged;
#endif

            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Closing += (_, args) =>
            {
                _closing?.Invoke(this, args);
            };

            async void CompleteAdapter()
            {
                _controlHostImpl = await adapterFactory switch
                {
#if !WPF
                    Core.WebViewAdapter.CompositorHostAdapterFactory comp => new NativeWebViewCompositorHost(comp),
#endif
                    Core.WebViewAdapter.NativeHostAdapterFactory native => new NativeWebViewControlHost(native),
                    _ => new EmptyNativeWebViewControlImpl()
                };

                _controlHostImpl.AdapterCreated += (_, adapter) =>
                {
                    // Opaque by default, so web pages without their own background stay readable with a dark theme.
                    var background = _initialDefaultBackground ?? Colors.White;
                    adapter.DefaultBackground = background;
                    ApplyWindowBackground(background);
                    AdapterCreated?.Invoke(this, new Core.WebViewAdapterEventArgs(adapter));
                };
                _controlHostImpl.AdapterDestroyed += (_, adapter) => AdapterDestroyed?.Invoke(this, new Core.WebViewAdapterEventArgs(adapter));

                Content = _controlHostImpl;
            }
        }

#if WPF
        public bool CanUserResize { get => ResizeMode != ResizeMode.NoResize; set => ResizeMode = value ? ResizeMode.CanResize : ResizeMode.NoResize; }
#elif AVALONIA
        public bool CanUserResize { get => CanResize; set => CanResize = value; }
#endif

        public Core.IWebViewAdapter? TryGetAdapter() => _controlHostImpl?.TryGetAdapter();

        public Color DefaultBackground
        {
            set
            {
                _initialDefaultBackground = value;
                if (_controlHostImpl?.TryGetAdapter() is { } adapter)
                {
                    adapter.DefaultBackground = value;
                }
                ApplyWindowBackground(value);
            }
        }

        // The webview may be composited into the window (or transparent on macOS),
        // so the window itself has to use the same color.
        private void ApplyWindowBackground(Color color)
        {
#if WPF
            Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
#elif AVALONIA
            Background = new Media.SolidColorBrush(color);
#endif
        }

        public void Dispose() => Close();

        event EventHandler? Core.INativeWebViewDialog.Closing
        {
            add => _closing += value;
            remove => _closing -= value;
        }

        public event EventHandler<Core.WebViewAdapterEventArgs>? AdapterCreated;
        public event EventHandler<Core.WebViewAdapterEventArgs>? AdapterDestroyed;

        bool Core.INativeWebViewDialog.Show(IPlatformHandle _) => false;

        void Core.INativeWebViewDialog.Focus()
        {
            Activate();
            _controlHostImpl?.FocusWebView();

            if (!IsActive)
            {
                Activated -= OnActivatedFocusWebView;
                Activated += OnActivatedFocusWebView;
            }
        }

        private void OnActivatedFocusWebView(object? sender, EventArgs e)
        {
            Activated -= OnActivatedFocusWebView;
            _controlHostImpl?.FocusWebView();
        }

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
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            TryGetAdapter()?.SizeChanged(PixelSize.FromSizeWithDpi(new Size(e.NewSize.Width, e.NewSize.Height), VisualTreeHelper.GetDpi(this).DpiScaleX));
        }
        private void OnLocationOrStateChanged(object? sender, EventArgs e)
        {
            TryGetAdapter()?.SizeChanged(PixelSize.FromSizeWithDpi(new Size(Width, Height), VisualTreeHelper.GetDpi(this).DpiScaleX));
        }
#endif

#if WPF
        public IPlatformHandle? TryGetPlatformHandle() => XpfWpfAbstraction.GetAvaloniaTopLevelForWindow(this)?.TryGetPlatformHandle();
#endif
    }
}
#endif
