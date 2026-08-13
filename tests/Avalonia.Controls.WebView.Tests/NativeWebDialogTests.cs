using Avalonia.Controls.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

public class NativeWebDialogTests : HeadlessTestsBase
{
    [AvaloniaFact]
    public async Task Should_Initialize_As_Headless()
    {
        var webDialog = new NativeWebDialog();
        webDialog.Show();

        await WaitForAdapterCreation(webDialog);

        Assert.Equal("HeadlessWebViewAdapter", webDialog.TryGetWebViewPlatformHandle()?.HandleDescriptor);
    }

    [AvaloniaFact]
    public void Should_Set_Title()
    {
        var webDialog = new NativeWebDialog();
        webDialog.Title = "Hello World";
        webDialog.Show();

        var underlying = webDialog.TryGetWindow()!;
        Assert.Equal("Hello World", underlying.Title);
    }

    [AvaloniaFact]
    public void Should_Set_CanUserResize_Before_And_After_Show()
    {
        var dialog = new NativeWebDialog();
        dialog.CanUserResize = true;
        dialog.Show();
        var underlying = dialog.TryGetWindow()!;
        Assert.True(underlying.CanResize);

        dialog.CanUserResize = false;
        Assert.False(underlying.CanResize);
    }

    [AvaloniaFact]
    public void Should_Set_And_Reflect_Position_And_Size()
    {
        var dialog = new NativeWebDialog();
        dialog.Resize(400, 300);
        dialog.Move(50, 60);
        dialog.Show();
        var underlying = dialog.TryGetWindow()!;
        Assert.Equal(400, underlying.Width);
        Assert.Equal(300, underlying.Height);
        Assert.Equal(50, underlying.Position.X);
        Assert.Equal(60, underlying.Position.Y);
    }

    [AvaloniaFact]
    public async Task Should_Use_White_Background_Without_Owner()
    {
        var dialog = new NativeWebDialog();
        dialog.Show();

        await WaitForAdapterCreation(dialog);

        Assert.Null(dialog.DefaultBackground);
        Assert.Equal(Colors.White, GetAdapterBackground(dialog));
        Assert.Equal(Colors.White, GetWindowBackground(dialog));
    }

    [AvaloniaFact]
    public async Task Should_Use_Owner_Background_When_Not_Set()
    {
        var owner = new Window { Background = new SolidColorBrush(Colors.Blue) };
        owner.Show();

        var dialog = new NativeWebDialog();
        dialog.Show(owner);

        await WaitForAdapterCreation(dialog);

        Assert.Null(dialog.DefaultBackground);
        Assert.Equal(Colors.Blue, GetAdapterBackground(dialog));
        Assert.Equal(Colors.Blue, GetWindowBackground(dialog));
    }

    [AvaloniaFact]
    public async Task Should_Set_DefaultBackground_Before_Show()
    {
        var owner = new Window { Background = new SolidColorBrush(Colors.Blue) };
        owner.Show();

        var dialog = new NativeWebDialog();
        dialog.DefaultBackground = Colors.Red;
        dialog.Show(owner);

        await WaitForAdapterCreation(dialog);

        // Explicit value wins over the owner background.
        Assert.Equal(Colors.Red, dialog.DefaultBackground);
        Assert.Equal(Colors.Red, GetAdapterBackground(dialog));
        Assert.Equal(Colors.Red, GetWindowBackground(dialog));
    }

    [AvaloniaFact]
    public async Task Should_Set_DefaultBackground_After_Show()
    {
        var dialog = new NativeWebDialog();
        dialog.Show();

        await WaitForAdapterCreation(dialog);

        dialog.DefaultBackground = Colors.Red;
        Assert.Equal(Colors.Red, GetAdapterBackground(dialog));
        Assert.Equal(Colors.Red, GetWindowBackground(dialog));
    }

    private static Color? GetAdapterBackground(NativeWebDialog dialog) =>
        ((HeadlessWebViewAdapter)dialog.TryGetWebViewPlatformHandle()!).LastDefaultBackground;

    private static Color? GetWindowBackground(NativeWebDialog dialog) =>
        (dialog.TryGetWindow()!.Background as ISolidColorBrush)?.Color;

    [AvaloniaFact]
    public void Should_Expose_Platform_Handle()
    {
        var dialog = new NativeWebDialog();
        dialog.Show();
        var handle = dialog.TryGetPlatformHandle();
        Assert.NotNull(handle);
        Assert.Equal(dialog.TryGetWindow()?.TryGetPlatformHandle(), handle);
    }

    [AvaloniaFact]
    public void Should_Navigate_And_Expose_Source()
    {
        var dialog = new NativeWebDialog();
        dialog.Show();
        var uri = new Uri("https://avaloniaui.net");
        dialog.Navigate(uri);
        Assert.Equal(uri, dialog.Source);
    }

    [AvaloniaFact]
    public void Should_Close_Without_Exception()
    {
        var dialog = new NativeWebDialog();
        dialog.Show();
        dialog.Close();
        // Should not throw
    }

    [AvaloniaFact]
    public void Should_Raise_Closing_Event()
    {
        var dialog = new NativeWebDialog();
        bool closingRaised = false;
        dialog.Closing += (_, _) => closingRaised = true;
        dialog.Show();
        dialog.Close();
        Assert.True(closingRaised);
    }

    [AvaloniaFact]
    public async Task Should_Raise_AdapterCreated_And_AdapterDestroyed()
    {
        var dialog = new NativeWebDialog();
        bool initialized = false, destroyed = false;
        dialog.AdapterCreated += (_, _) => initialized = true;
        dialog.AdapterDestroyed += (_, _) => destroyed = true;

        dialog.Show();

        await WaitForAdapterCreation(dialog);

        Assert.True(initialized);

        dialog.Close();
        Assert.True(destroyed);
    }
}
