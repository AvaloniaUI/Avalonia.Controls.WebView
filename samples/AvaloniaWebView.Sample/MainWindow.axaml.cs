using System;
using Avalonia.Controls;

namespace AvaloniaWebView.Sample;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void NativeWebView_OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        Console.WriteLine(e.Request);

        Console.WriteLine(await ((NativeWebView)sender!).InvokeScript("document.location.href"));
    }

    private void NativeWebView_OnNavigationStarted(object? sender, WebViewNavigationStartingEventArgs e)
    {
        Console.WriteLine(e.Request);
    }
}
