using System;
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
