using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.MicroCom;
using AvaloniaWebView.Interop;
using MicroCom.Runtime;

namespace AvaloniaWebView;

internal class NativeWebViewAdapter : IWebViewAdapter
{
    [DllImport("libWebView")]
    private static extern IntPtr CreateWebViewNativeFactory();

    private readonly NativeWebViewCallbacks _callbacks;
    private readonly INativeWebView _nativeWebView;
    private readonly Dictionary<int, TaskCompletionSource<string?>> _scriptResults = new();
    private int _scriptResultsCurrent;

    public NativeWebViewAdapter()
    {
        using var factory = MicroComRuntime.CreateProxyFor<IWebViewFactory>(CreateWebViewNativeFactory(), true);
        _callbacks = new NativeWebViewCallbacks(this);
        _nativeWebView = factory.CreateWebView(_callbacks);
    }
    
    public event EventHandler<WebViewNavigationCompletedEventArgs>? NavigationCompleted;
    public event EventHandler<WebViewNavigationStartingEventArgs>? NavigationStarted;
    public bool CanGoBack => _nativeWebView.CanGoBack == 1;
    public bool CanGoForward => _nativeWebView.CanGoForward == 1;

    public Uri? Source
    {
        get => Uri.TryCreate(_nativeWebView.Source.String, UriKind.RelativeOrAbsolute, out var source) ? source : null;
        set => Navigate(value!);
    }

    public bool GoBack() => _nativeWebView.GoBack() == 1;

    public bool GoForward() => _nativeWebView.GoForward() == 1;

    public async Task<string?> InvokeScript(string script)
    {
        using var scriptStr = new AvnString(script);
        var index = _scriptResultsCurrent++;
        var tcs = new TaskCompletionSource<string?>();
        _scriptResults.Add(index, tcs);
        _nativeWebView.InvokeScript(scriptStr, index);
        return await tcs.Task;
    }

    public void Navigate(Uri url)
    {
        using var str = new AvnString(url.ToString());
        _nativeWebView.Navigate(str);
    }

    public void NavigateToString(string text)
    {
        using var str = new AvnString(text);
        _nativeWebView.Navigate(str);
    }

    public bool Refresh() => _nativeWebView.Refresh() == 1;

    public bool Stop() => _nativeWebView.Refresh() == 1;

    public void Dispose()
    {
        _nativeWebView.Dispose();
        _callbacks.Dispose();
    }

    public unsafe IntPtr Handle => new(_nativeWebView.AsNsView());
    public string? HandleDescriptor => "NSView";
    public event EventHandler? Initialized;
    public bool IsInitialized => true;
    public void SizeChanged() {}

    private void OnScriptResult(int id, bool isError, string? result)
    {
        var tcs = _scriptResults[id];
        _scriptResults.Remove(id);

        if (isError)
        {
            tcs.TrySetException(new Exception(result ?? "Unknown script execution error"));
        }
        else
        {
            tcs.TrySetResult(result);
        }
    }
    
    private void OnNavigationCompleted(string url, bool success)
    {
        NavigationCompleted?.Invoke(this, new WebViewNavigationCompletedEventArgs
        {
            IsSuccess = success,
            Request = new Uri(url)
        });
    }
    
    private bool OnNavigationStarted(string url)
    {
        var args = new WebViewNavigationStartingEventArgs { Request = new Uri(url) };
        NavigationStarted?.Invoke(this, args);
        return args.Cancel;
    }
    
    private class NativeWebViewCallbacks : CallbackBase, INativeWebViewHandlers
    {
        private readonly NativeWebViewAdapter _adapter;

        public NativeWebViewCallbacks(NativeWebViewAdapter adapter)
        {
            _adapter = adapter;
        }

        public void OnScriptResult(int id, int isError, IAvnString ppv) =>
            _adapter.OnScriptResult(id, isError == 1, ppv.String);

        public void OnNavigationCompleted(IAvnString url, int success) =>
            _adapter.OnNavigationCompleted(url.String, success == 1);

        public unsafe void OnNavigationStarted(IAvnString url, int* cancel) =>
            *cancel = _adapter.OnNavigationStarted(url.String) ? 1 : 0;
    }
}
