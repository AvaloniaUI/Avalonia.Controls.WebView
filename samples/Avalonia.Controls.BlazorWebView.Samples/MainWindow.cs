using Avalonia.Controls;
using Avalonia.Controls.BlazorWebView;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.Controls.BlazorWebView.Samples;

public class MainWindow : Window
{
    public MainWindow()
    {
        Title = "Avalonia Blazor Hybrid";
        Width = 900;
        Height = 600;
        Background = Avalonia.Media.Brushes.White;

        var services = new ServiceCollection();
        services.AddAvaloniaBlazorWebView();
        var provider = services.BuildServiceProvider();

        var blazor = new EmbeddedBlazorWebView
        {
            Services = provider,
            // Logical path only; CreateFileProvider serves wwwroot from embedded resources.
            HostPage = Path.Combine("wwwroot", "index.html"),
        };
        blazor.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(Counter),
        });

        Content = blazor;
    }
}
