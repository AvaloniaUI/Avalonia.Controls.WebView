using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;
using AvaloniaUI.WebView.TinyMCE.Core;

namespace AvaloniaUI.WebView.TinyMCE;

public partial class TinyMceView : ThemeVariantScope
{
    public static readonly StyledProperty<string?> HtmlTextProperty = AvaloniaProperty.Register<TinyMceView, string?>(nameof(HtmlText));
    public static readonly StyledProperty<double> FontSizeProperty = TextElement.FontSizeProperty.AddOwner<TinyMceView>();
    public static readonly StyledProperty<IBrush?> BackgroundProperty = Border.BackgroundProperty.AddOwner<TinyMceView>();
    public static readonly StyledProperty<IBrush?> ForegroundProperty = TextElement.ForegroundProperty.AddOwner<TinyMceView>();
    public static readonly StyledProperty<string> ToolBarProperty = AvaloniaProperty.Register<TinyMceView, string>(nameof(ToolBar), ToolBarDefaultValue);
    public static readonly StyledProperty<string> PluginsProperty = AvaloniaProperty.Register<TinyMceView, string>(nameof(Plugins), PluginsDefaultValue);
    
    public TinyMceView()
    {
        Child = _nativeWebView = new NativeWebView();
        _nativeWebView.WebMessageReceived += NativeWebViewOnWebMessageReceived;
        _nativeWebView.AttachedToVisualTree += (_, _) => RebuildPage();
        _nativeWebView.NavigationCompleted += NativeWebViewOnNavigationCompleted;
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == HtmlTextProperty)
        {
            SendCurrentText();
        }
        else if (change.Property == ToolBarProperty
                 || change.Property == ActualThemeVariantProperty
                 || change.Property == FontSizeProperty)
        {
            RebuildPage();
        }
    }
    private void RebuildPage()
    {
        if (!this.IsAttachedToVisualTree())
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        
        var html = HtmlPageBuilder.Build(
            LoadTinyMceStyle((PlatformThemeVariant?)ActualThemeVariant == PlatformThemeVariant.Dark
                ? TinyMceThemeVariant.Dark : TinyMceThemeVariant.Light),
            JsonEncodedText.Encode(LoadTinyMceContentStyle((PlatformThemeVariant?)ActualThemeVariant == PlatformThemeVariant.Dark
                ? TinyMceThemeVariant.Dark : TinyMceThemeVariant.Light)).ToString(),
            "Arial",
            (int)FontSize,
            (Background as ISolidColorBrush ?? topLevel?.Background as ISolidColorBrush)?.Color.ToString(),
            (Foreground as ISolidColorBrush ?? topLevel?.Foreground as ISolidColorBrush)?.Color.ToString(),
            ToolBar,
            Plugins);
        _nativeWebView.NavigateToString(html);
    }
}
