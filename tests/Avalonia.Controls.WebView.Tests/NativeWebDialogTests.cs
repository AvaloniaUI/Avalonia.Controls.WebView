using Avalonia.Headless.XUnit;
using System;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

public class NativeWebDialogTests : HeadlessTestsBase
{
    [AvaloniaFact]
    public void Should_Initialize_As_Headless()
    {
        var webDialog = new NativeWebDialog();
        webDialog.Show();

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
    public void Should_Raise_AdapterInitialized_And_AdapterDestroyed()
    {
        var dialog = new NativeWebDialog();
        bool initialized = false, destroyed = false;
        dialog.AdapterInitialized += (_, _) => initialized = true;
        dialog.AdapterDestroyed += (_, _) => destroyed = true;

        dialog.Show();
        Assert.True(initialized);

        dialog.Close();
        Assert.True(destroyed);
    }
}
