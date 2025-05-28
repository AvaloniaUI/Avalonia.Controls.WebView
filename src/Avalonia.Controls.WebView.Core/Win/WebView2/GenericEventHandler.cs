using System;
using Avalonia.Controls.Win.WebView2.Interop;

namespace Avalonia.Controls.Win.WebView2;

internal abstract class GenericEventHandler<TArgs>(Action<ICoreWebView2, TArgs> callback) : CallbackBase
{
    public void Invoke(ICoreWebView2 sender, TArgs args) => callback(sender, args);
}
