using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Avalonia.Controls.WebView.Tests;

public abstract class HeadlessTestsBase : IDisposable
{
    public HeadlessTestsBase()
    {
        WebViewAdapter.UseHeadless = true;
    }

    public async Task DoDelay()
    {
        Dispatcher.UIThread.RunJobs();
        await Task.Delay(20);
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// Pumps the dispatcher until <paramref name="condition"/> is true or the timeout runs out.
    /// The headless adapter completes navigation on the thread pool and reports it back through the dispatcher,
    /// so a fixed delay is not enough on a busy machine. On timeout this returns and the caller's assert fails.
    /// </summary>
    public async Task WaitFor(Func<bool> condition, int timeoutMs = 5000)
    {
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            Dispatcher.UIThread.RunJobs();
            if (condition() || stopwatch.ElapsedMilliseconds > timeoutMs)
                return;
            await Task.Delay(5);
        }
    }

    protected Task WaitForAdapterCreation(NativeWebView webView)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        webView.AdapterCreated += OnAdapterCreated;
        return tcs.Task;

        void OnAdapterCreated(object? sender, WebViewAdapterEventArgs e)
        {
            webView.AdapterCreated -= OnAdapterCreated;
            tcs.TrySetResult(true);
        }
    }

    protected Task WaitForAdapterCreation(NativeWebDialog webView)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        webView.AdapterCreated += (_, _) => tcs.SetResult(true);
        return tcs.Task;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            WebViewAdapter.UseHeadless = false;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
