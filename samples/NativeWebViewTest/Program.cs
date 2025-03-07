using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Avalonia.Xpf.Controls;

// This code is received from the costumer, and I was too lazy to change it to normal Main method with [StaThread].
if (OperatingSystem.IsWindows())
{
    Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
    Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
}

Init.Go();

string html = $"""
               <!DOCTYPE html>
               <html lang="en">
                   <head>
                       <meta charset="utf-8">
                       <title>Test</title>
                   </head>
                   <body>
                       <p>This is HTML</p>
                       <form>
                           <textarea rows="5" cols="50">
               HTML textarea line 1
               HTML textarea line 2
               HTML textarea line 3
                           </textarea>
                       </form> 
                   </body>
               </html>
               """;

File.WriteAllText("test.html", html);
Uri uri = new Uri(Path.GetFullPath("test.html"));

RadioButton btnOne, btnTwo;
DockPanel browserPanel1, browserPanel2;

var window = new Window
{
    Content = new DockPanel { Margin = new Thickness(10) }.AddChildren
    (
        new StackPanel { Orientation = Orientation.Horizontal }.SetDock (Dock.Top).AddChildren
        (
            btnOne = new RadioButton { Content = "Browser Panel 1", IsChecked = true },
            btnTwo = new RadioButton { Content = "Browser Panel 2" }
        ),
        new Grid().AddChildren
        (
            browserPanel1 = CreateBrowserPanel().SetVisible (true),
            browserPanel2 = CreateBrowserPanel().SetVisible (false)
        )
    ),
    Width = 500,
    Height = 300,
    WindowStartupLocation = WindowStartupLocation.CenterScreen
};

btnOne.Padding = btnTwo.Padding = default;
btnOne.Margin = btnTwo.Margin = new Thickness (0, 0, 20, 10);

btnOne.Click += (sender, args) =>
{
    browserPanel2.SetVisible(false);
    browserPanel1.SetVisible(true);
};

btnTwo.Click += (sender, args) =>
{
    browserPanel2.SetVisible(true);
    browserPanel1.SetVisible(false);
};

window.ShowDialog();

DockPanel CreateBrowserPanel()
{
    NativeWebView? webView;
    GroupBox groupBox;
    TextBox txtWpf;

    var panel = new DockPanel().AddChildren
    (
        new Label { Content = "Press Cmd+T to focus the WPF TextBox" }.SetDock (Dock.Top),
        txtWpf = new TextBox { Text = "WPF TextBox", Margin = new Thickness (3, 0, 0, 15), Padding = new Thickness (5) }.SetDock (Dock.Top),
        groupBox = new GroupBox
        {
            Header = "NativeWebView",
            Content = webView = new(),
        }
    );
    webView.Navigate(uri);
    webView.KeyDown += (sender, args) =>
    {
        if (args.Key == Key.T && Keyboard.Modifiers == ModifierKeys.Windows)
        {
            txtWpf.Focus();
        }
    };
    txtWpf.PreviewKeyDown += (sender, args) =>
    {
        if (args.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Windows)
        {
            if (webView?.Parent is GroupBox groupBox)
            {
                groupBox.Content = null;
                webView = null;
                GC.Collect();
                GC.Collect();
            }
        }
        if (args.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Windows)
        {
            if (webView is null)
            {
                groupBox.Content = webView = new();
                webView.Navigate(uri);
            }
        }
        if (args.Key == Key.I && Keyboard.Modifiers == ModifierKeys.Windows)
        {
            webView?.Focus();
        }
    };

    return panel;
}

