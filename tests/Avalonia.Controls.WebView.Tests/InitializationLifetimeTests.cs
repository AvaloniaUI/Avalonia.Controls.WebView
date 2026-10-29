using System;
using System.Threading.Tasks;
using Avalonia.Controls.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

public class InitializationLifetimeTests : HeadlessTestsBase
{
    [AvaloniaFact]
    public async Task Factory_Is_Single_Flight_Across_Pending_Reattachment()
    {
        IDisposable? deferral = null;
        var calls = 0;
        var webView = new NativeWebView();
        webView.EnvironmentRequested += (_, e) => { calls++; deferral ??= e.GetDeferral(); };
        using var window = new ClosingWindow { Content = webView };
        window.Show();
        window.Content = null;
        window.Content = webView;
        deferral!.Dispose();
        await WaitFor(() => webView.TryGetPlatformHandle() is not null);
        Assert.Equal(1, calls);
        Assert.Single(webView.GetVisualChildren());
    }

    [AvaloniaFact]
    public async Task Detached_Factory_Does_Not_Publish_A_Visual_Until_Reattached()
    {
        IDisposable? deferral = null;
        var webView = new NativeWebView();
        webView.EnvironmentRequested += (_, e) => deferral = e.GetDeferral();
        using var window = new ClosingWindow { Content = webView };
        window.Show();
        window.Content = null;
        deferral!.Dispose();
        await DoDelay();
        Assert.Empty(webView.GetVisualChildren());
        window.Content = webView;
        await WaitFor(() => webView.TryGetPlatformHandle() is not null);
        Assert.NotNull(webView.TryGetPlatformHandle());
    }

    [AvaloniaFact]
    public async Task Compositor_Late_Adapter_Does_Not_Replace_Current_Attempt()
    {
        var first = new TaskCompletionSource<IWebViewAdapterWithOffscreenBuffer>();
        var second = new TaskCompletionSource<IWebViewAdapterWithOffscreenBuffer>();
        var calls = 0;
        var host = new NativeWebViewCompositorHost(new WebViewAdapter.CompositorHostAdapterFactory(
            _ => ++calls == 1 ? first.Task : second.Task, HeadlessWebViewAdapter.GetHeadlessInfo()));
        using var window = new ClosingWindow { Content = host };
        window.Show();
        var oldWait = host.GetAdapterAsync();
        window.Content = null;
        window.Content = host;
        var currentWait = host.GetAdapterAsync();
        var old = await HeadlessWebViewAdapter.CreateAsync(new(new DeferralManager()));
        first.SetResult(old);
        await DoDelay();
        Assert.True(oldWait.IsCanceled);
        Assert.False(currentWait.IsCompleted);
        Assert.Throws<ObjectDisposedException>(() => old.Navigate(new Uri("https://example.invalid")));
        var current = await HeadlessWebViewAdapter.CreateAsync(new(new DeferralManager()));
        second.SetResult(current);
        Assert.Same(current, await currentWait.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [AvaloniaFact]
    public async Task Factory_Failure_Is_Reported_Once_Without_Automatic_Retry()
    {
        var error = new InvalidOperationException("environment failed");
        var webView = new NativeWebView();
        var count = 0;
        Exception? reported = null;
        webView.EnvironmentRequested += (_, _) => throw error;
        webView.InitializationFailed += (_, e) => { reported = e.Exception; count++; };
        using var window = new ClosingWindow { Content = webView };
        window.Show();
        await WaitFor(() => reported is not null);
        window.Content = null;
        window.Content = webView;
        await DoDelay();
        Assert.Same(error, reported);
        Assert.Equal(1, count);
        Assert.Empty(webView.GetVisualChildren());
    }

    [AvaloniaFact]
    public async Task Adapter_Failure_Faults_Wait_And_Reaches_Public_Owner()
    {
        var pending = new TaskCompletionSource();
        var error = new InvalidOperationException("adapter failed");
        var webView = new NativeWebView();
        Exception? reported = null;
        webView.EnvironmentRequested += (_, e) => ((HeadlessWebViewEnvironmentRequestedEventArgs)e).InitializeAsync = () => pending.Task;
        webView.InitializationFailed += (_, e) => reported = e.Exception;
        using var window = new ClosingWindow { Content = webView };
        window.Show();
        await WaitFor(() => webView.TryGetImpl() is not null);
        var wait = webView.TryGetImpl()!.GetAdapterAsync();
        pending.SetException(error);
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() => wait));
        Assert.Same(error, reported);
        Assert.Null(webView.TryGetPlatformHandle());
    }

    [AvaloniaFact]
    public async Task Native_Late_Success_Is_Disposed_And_Cannot_Replace_New_Attempt()
    {
        var first = new TaskCompletionSource<IWebViewAdapter>();
        var second = new TaskCompletionSource<IWebViewAdapter>();
        var calls = 0;
        var host = new TestNativeHost(() => ++calls == 1 ? first.Task : second.Task);
        var events = 0;
        host.AdapterCreated += (_, _) => events++;
        host.Attach();
        var oldWait = host.GetAdapterAsync();
        host.Detach();
        host.Attach();
        var currentWait = host.GetAdapterAsync();
        var current = await HeadlessWebViewAdapter.CreateAsync(new(new DeferralManager()));
        second.SetResult(current);
        Assert.Same(current, await currentWait.WaitAsync(TimeSpan.FromSeconds(5)));
        var old = await HeadlessWebViewAdapter.CreateAsync(new(new DeferralManager()));
        first.SetResult(old);
        await DoDelay();
        Assert.True(oldWait.IsCanceled);
        Assert.Same(current, host.TryGetAdapter());
        Assert.Equal(1, events);
        Assert.Throws<ObjectDisposedException>(() => old.Navigate(new Uri("https://example.invalid")));
        host.Detach();
    }

    [AvaloniaFact]
    public async Task Native_Late_Fault_Is_Reported_Without_Faulting_New_Attempt()
    {
        var first = new TaskCompletionSource<IWebViewAdapter>();
        var second = new TaskCompletionSource<IWebViewAdapter>();
        var calls = 0;
        var host = new TestNativeHost(() => ++calls == 1 ? first.Task : second.Task);
        Exception? reported = null;
        WebViewInitializationFailedEventArgs? identity = null;
        host.InitializationFailed += (_, error) => { reported = error.Exception; identity = error; };
        host.Attach();
        var oldWait = host.GetAdapterAsync();
        host.Detach();
        host.Attach();
        var currentWait = host.GetAdapterAsync();
        var error = new InvalidOperationException("old attempt failed");
        first.SetException(error);
        await WaitFor(() => reported is not null);
        Assert.Same(error, reported);
        Assert.Equal(1, identity!.AttemptId);
        Assert.False(identity.IsCurrentAttempt);
        Assert.True(oldWait.IsCanceled);
        Assert.False(currentWait.IsCompleted);
        host.Detach();
        second.SetResult(await HeadlessWebViewAdapter.CreateAsync(new(new DeferralManager())));
        await DoDelay();
    }

    [AvaloniaFact]
    public async Task Native_Current_Fault_Completes_Wait_With_Original_Error()
    {
        var pending = new TaskCompletionSource<IWebViewAdapter>();
        var host = new TestNativeHost(() => pending.Task);
        Exception? reported = null;
        host.InitializationFailed += (_, error) => reported = error.Exception;
        host.Attach();
        var wait = host.GetAdapterAsync();
        var error = new InvalidOperationException("controller failed");
        pending.SetException(error);
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() => wait));
        Assert.Same(error, reported);
        Assert.Null(host.TryGetAdapter());
        host.Detach();
    }

    [AvaloniaFact]
    public async Task Native_Explicit_Reparenting_Retains_Ready_Adapter()
    {
        var adapter = await HeadlessWebViewAdapter.CreateAsync(new(new DeferralManager()));
        var calls = 0;
        var host = new TestNativeHost(() => { calls++; return Task.FromResult<IWebViewAdapter>(adapter); });
        host.Attach();
        using (host.BeginReparenting(false))
        {
            host.Detach();
            host.Attach();
        }
        Assert.Equal(1, calls);
        Assert.Same(adapter, host.TryGetAdapter());
        host.Detach();
        Assert.Throws<ObjectDisposedException>(() => adapter.Navigate(new Uri("https://example.invalid")));
    }

    [AvaloniaFact]
    public async Task AdapterCreated_Observer_Can_Detach_Without_Leaving_An_Adapter()
    {
        var webView = new NativeWebView();
        using var window = new ClosingWindow { Content = webView };
        var created = false;
        webView.AdapterCreated += (_, _) => { created = true; window.Content = null; };
        window.Show();
        await WaitFor(() => created);
        Assert.True(created);
        Assert.Null(webView.TryGetPlatformHandle());
    }

    [AvaloniaFact]
    public async Task Reparenting_Pending_Adapter_Cancels_Old_Attempt_And_Allows_New_One()
    {
        var first = new TaskCompletionSource<IWebViewAdapter>();
        var current = await HeadlessWebViewAdapter.CreateAsync(new(new DeferralManager()));
        var calls = 0;
        var host = new TestNativeHost(() => ++calls == 1 ? first.Task : Task.FromResult<IWebViewAdapter>(current));
        host.Attach();
        var oldWait = host.GetAdapterAsync();
        using (host.BeginReparenting(false))
        {
            host.Detach();
            host.Attach();
        }
        var old = await HeadlessWebViewAdapter.CreateAsync(new(new DeferralManager()));
        first.SetResult(old);
        await DoDelay();
        Assert.True(oldWait.IsCanceled);
        Assert.Same(current, host.TryGetAdapter());
        Assert.Throws<ObjectDisposedException>(() => old.Navigate(new Uri("https://example.invalid")));
        host.Detach();
    }

    [AvaloniaFact]
    public void Synchronous_Native_Builder_Failure_Is_Reported_And_Still_Throws()
    {
        var error = new InvalidOperationException("builder failed");
        var host = new TestNativeHost(() => throw error);
        WebViewInitializationFailedEventArgs? reported = null;
        host.InitializationFailed += (_, e) => reported = e;
        Assert.Same(error, Assert.Throws<InvalidOperationException>(() => host.Attach()));
        Assert.Same(error, reported!.Exception);
        Assert.True(reported.IsCurrentAttempt);
        Assert.Equal(1, reported.AttemptId);
        Assert.True(host.GetAdapterAsync().IsFaulted);
        host.Detach();
    }

    [AvaloniaFact]
    public async Task Destroyed_Observer_Exception_Does_Not_Keep_Native_Adapter_Alive()
    {
        var adapter = await HeadlessWebViewAdapter.CreateAsync(new(new DeferralManager()));
        var host = new TestNativeHost(() => Task.FromResult<IWebViewAdapter>(adapter));
        host.AdapterDestroyed += (_, _) => throw new InvalidOperationException("observer failed");
        host.Attach();
        Assert.Throws<InvalidOperationException>(() => host.Detach());
        Assert.Null(host.TryGetAdapter());
        Assert.Throws<ObjectDisposedException>(() => adapter.Navigate(new Uri("https://example.invalid")));
    }

    private sealed class TestNativeHost(Func<Task<IWebViewAdapter>> create)
        : NativeWebViewControlHost(new WebViewAdapter.NativeHostAdapterFactory(
            (_, _) => new WebViewAdapter.AdapterWrapper(new PlatformHandle(IntPtr.Zero, "Test"), create()),
            HeadlessWebViewAdapter.GetHeadlessInfo()))
    {
        public void Attach() => CreateNativeControlCore(new PlatformHandle(IntPtr.Zero, "Test"));
        public void Detach() => DestroyNativeControlCore(new PlatformHandle(IntPtr.Zero, "Test"));
    }

    private sealed class ClosingWindow : Window, IDisposable
    {
        public void Dispose() => Close();
    }
}
