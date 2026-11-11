using System;
using System.Threading.Tasks;
using Avalonia.Controls.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

public class WebViewHostLifetimeTests : HeadlessTestsBase
{
    [AvaloniaFact]
    public async Task Compositor_Late_Adapter_Does_Not_Replace_Current_Attempt()
    {
        var first = new TaskCompletionSource<IWebViewAdapterWithOffscreenBuffer>();
        var second = new TaskCompletionSource<IWebViewAdapterWithOffscreenBuffer>();
        var calls = 0;
        var host = new NativeWebViewCompositorHost(new WebViewAdapter.CompositorHostAdapterFactory(
            _ => ++calls == 1 ? first.Task : second.Task, HeadlessWebViewAdapter.GetHeadlessInfo()));
        var window = new Window { Content = host };
        window.Show();
        var oldWait = host.GetAdapterAsync();
        window.Content = null;
        window.Content = host;
        var currentWait = host.GetAdapterAsync();

        var old = await CreateAdapter();
        first.SetResult(old);
        await DoDelay();

        Assert.True(oldWait.IsCanceled);
        Assert.False(currentWait.IsCompleted);
        AssertDisposed(old);

        var current = await CreateAdapter();
        second.SetResult(current);
        Assert.Same(current, await currentWait.WaitAsync(TimeSpan.FromSeconds(5)));
        window.Close();
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

        var current = await CreateAdapter();
        second.SetResult(current);
        Assert.Same(current, await currentWait.WaitAsync(TimeSpan.FromSeconds(5)));

        var old = await CreateAdapter();
        first.SetResult(old);
        await DoDelay();

        Assert.True(oldWait.IsCanceled);
        Assert.Same(current, host.TryGetAdapter());
        Assert.Equal(1, events);
        AssertDisposed(old);
        host.Detach();
    }

    [AvaloniaFact]
    public async Task Native_Late_Fault_Does_Not_Fault_New_Attempt()
    {
        var first = new TaskCompletionSource<IWebViewAdapter>();
        var second = new TaskCompletionSource<IWebViewAdapter>();
        var calls = 0;
        var host = new TestNativeHost(() => ++calls == 1 ? first.Task : second.Task);
        host.Attach();
        var oldWait = host.GetAdapterAsync();
        host.Detach();
        host.Attach();
        var currentWait = host.GetAdapterAsync();

        first.SetException(new InvalidOperationException("Old attempt failed."));
        await DoDelay();

        Assert.True(oldWait.IsCanceled);
        Assert.False(currentWait.IsCompleted);

        var current = await CreateAdapter();
        second.SetResult(current);
        Assert.Same(current, await currentWait.WaitAsync(TimeSpan.FromSeconds(5)));
        host.Detach();
    }

    [AvaloniaFact]
    public async Task Native_Current_Fault_Completes_Wait_With_Original_Error()
    {
        var pending = new TaskCompletionSource<IWebViewAdapter>();
        var host = new TestNativeHost(() => pending.Task);
        host.Attach();
        var wait = host.GetAdapterAsync();

        var error = new InvalidOperationException("Controller failed.");
        pending.SetException(error);

        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(() => wait));
        Assert.Null(host.TryGetAdapter());
        host.Detach();
    }

    [AvaloniaFact]
    public async Task Native_Explicit_Reparenting_Retains_Ready_Adapter()
    {
        var adapter = await CreateAdapter();
        var calls = 0;
        var host = new TestNativeHost(() =>
        {
            calls++;
            return Task.FromResult<IWebViewAdapter>(adapter);
        });
        host.Attach();
        using (host.BeginReparenting(false))
        {
            host.Detach();
            host.Attach();
        }

        Assert.Equal(1, calls);
        Assert.Same(adapter, host.TryGetAdapter());
        host.Detach();
        AssertDisposed(adapter);
    }

    [AvaloniaFact]
    public async Task Native_Reparenting_Pending_Adapter_Starts_New_Attempt()
    {
        var first = new TaskCompletionSource<IWebViewAdapter>();
        var current = await CreateAdapter();
        var calls = 0;
        var host = new TestNativeHost(() => ++calls == 1 ? first.Task : Task.FromResult<IWebViewAdapter>(current));
        host.Attach();
        var oldWait = host.GetAdapterAsync();
        using (host.BeginReparenting(false))
        {
            host.Detach();
            host.Attach();
        }

        var old = await CreateAdapter();
        first.SetResult(old);
        await DoDelay();

        Assert.True(oldWait.IsCanceled);
        Assert.Same(current, host.TryGetAdapter());
        AssertDisposed(old);
        host.Detach();
    }

    private static Task<HeadlessWebViewAdapter> CreateAdapter() =>
        HeadlessWebViewAdapter.CreateAsync(new(new DeferralManager()));

    private static void AssertDisposed(IWebViewAdapter adapter) =>
        Assert.Throws<ObjectDisposedException>(() => adapter.Navigate(new Uri("https://example.invalid")));

    private sealed class TestNativeHost(Func<Task<IWebViewAdapter>> create)
        : NativeWebViewControlHost(new WebViewAdapter.NativeHostAdapterFactory(
            (_, _) => new WebViewAdapter.AdapterWrapper(new PlatformHandle(IntPtr.Zero, "Test"), create()),
            HeadlessWebViewAdapter.GetHeadlessInfo()))
    {
        private static readonly PlatformHandle s_parent = new(IntPtr.Zero, "Parent");

        public void Attach() => CreateNativeControlCore(s_parent);
        public void Detach() => DestroyNativeControlCore(s_parent);
    }
}
