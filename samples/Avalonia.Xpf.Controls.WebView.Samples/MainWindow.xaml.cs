using AvaloniaUI.Xpf.WpfAbstractions;
using Window = System.Windows.Window;

namespace Avalonia.Xpf.Controls.WebView.Samples;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var w = XpfWpfAbstraction.GetAvaloniaWindowForWindow(this);
        w?.AttachDevTools();
    }
}
