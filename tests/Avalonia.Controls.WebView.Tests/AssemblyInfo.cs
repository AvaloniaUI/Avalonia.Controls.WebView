using Avalonia.Controls.WebView.Tests;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(TestApplication))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Avalonia.Controls.WebView.Tests;

internal class TestApplication : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<TestApplication>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions
        {
            UseHeadlessDrawing = false 
        });
}
