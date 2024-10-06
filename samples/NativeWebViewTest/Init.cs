using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;
using Avalonia;
using Avalonia.Styling;
using AvaloniaUI.Xpf;
using AvaloniaUI.Xpf.WpfAbstractions;

public class Init
{
    public static void Go()
    {
        var avaloniaApp = Avalonia.AppBuilder.Configure<ProcessServerAvaloniaApp> ()
            .UsePlatformDetect ()
            //.With (new Win32PlatformOptions { DpiAwareness = Win32DpiAwareness.SystemDpiAware })
            .WithAvaloniaXpf ()
            .SetupWithClassicDesktopLifetime (Environment.GetCommandLineArgs (), lifetime =>
            {
                lifetime.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
            }).Instance;

        var app = new System.Windows.Application();
        
        SynchronizationContext.SetSynchronizationContext (AsyncOperationManager.SynchronizationContext = new DispatcherSynchronizationContext (app.Dispatcher));
    }
}

class ProcessServerAvaloniaApp : Avalonia.Application
{
    public ProcessServerAvaloniaApp()
    {
        Name = "Process Server";
        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
    }
}
