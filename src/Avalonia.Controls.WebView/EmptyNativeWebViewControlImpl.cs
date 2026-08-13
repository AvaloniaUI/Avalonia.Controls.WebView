#if AVALONIA || WPF
using System;
using System.Threading.Tasks;
#if WPF
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Documents;
using IWebViewAdapter = Avalonia.Controls.IWebViewAdapter;
#elif AVALONIA
using Avalonia.Controls.Documents;
using Avalonia.Media;
#endif

#if AVALONIA
namespace Avalonia.Controls;
#elif WPF
namespace Avalonia.Xpf.Controls;
#endif

internal class EmptyNativeWebViewControlImpl : Control, INativeWebViewControlImpl
{
    private const string Message = "NativeWebView is not supported on this platform/configuration.";

    public event EventHandler<IWebViewAdapter>? AdapterCreated;
    public event EventHandler<IWebViewAdapter>? AdapterDestroyed;
    public IWebViewAdapter? TryGetAdapter() => null;

    public Task<IWebViewAdapter?> GetAdapterAsync() => Task.FromResult<IWebViewAdapter?>(null);

    public void FocusWebView() { }

    public IDisposable BeginReparenting(bool yieldOnLayoutBeforeExiting) => EmptyDisposable.Instance;
    public IAsyncDisposable BeginReparentingAsync() => EmptyDisposable.Instance;

#if WPF
    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);
        var formattedText = new FormattedText(
            Message,
            System.Globalization.CultureInfo.CurrentUICulture,
            System.Windows.FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            14,
            (Brush)GetValue(TextElement.ForegroundProperty),
            VisualTreeHelper.GetDpi(this).PixelsPerDip
        );
        context.DrawText(formattedText, new System.Windows.Point(5, 5));
    }
#elif AVALONIA
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var formattedText = new FormattedText(
            Message,
            System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            14,
            GetValue(TextElement.ForegroundProperty)
        );
        context.DrawText(formattedText, new Point(5, 5));
    }
#endif
}
#endif
