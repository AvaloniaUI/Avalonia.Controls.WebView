using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AvaloniaUI.WebView.TinyMCE.Core;

namespace AvaloniaUI.WebView.TinyMCE;

public partial class TinyMceView : ContentControl
{
    public static readonly DependencyProperty HtmlTextProperty = DependencyProperty.Register(
        nameof(HtmlText), typeof(string), typeof(TinyMceView), new PropertyMetadata(default(string?), OnHtmlTextChanged));

    public static readonly DependencyProperty ToolBarProperty = DependencyProperty.Register(
        nameof(ToolBar), typeof(string), typeof(TinyMceView), new PropertyMetadata(ToolBarDefaultValue, OnOtherPropertyChanged));
    
    public static readonly DependencyProperty PluginsProperty = DependencyProperty.Register(
        nameof(Plugins), typeof(string), typeof(TinyMceView), new PropertyMetadata(PluginsDefaultValue, OnOtherPropertyChanged));

    public static readonly DependencyProperty RequestedThemeVariantProperty = DependencyProperty.Register(
        nameof(RequestedThemeVariant), typeof(TinyMceThemeVariant), typeof(TinyMceView), new PropertyMetadata(TinyMceThemeVariant.Light, OnOtherPropertyChanged));
    
    public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(typeof(TinyMceView), new FrameworkPropertyMetadata(OnOtherPropertyChanged));
    public static readonly DependencyProperty BackgroundProperty = Border.BackgroundProperty.AddOwner(typeof(TinyMceView), new FrameworkPropertyMetadata(OnOtherPropertyChanged));
    public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(typeof(TinyMceView), new FrameworkPropertyMetadata(OnOtherPropertyChanged));
    
    public TinyMceView()
    {
        Content = _nativeWebView = new NativeWebView();
        _nativeWebView.WebMessageReceived += NativeWebViewOnWebMessageReceived;
        _nativeWebView.Loaded += (_, _) => RebuildPage();
        _nativeWebView.NavigationCompleted += NativeWebViewOnNavigationCompleted;
    }
    
    public TinyMceThemeVariant RequestedThemeVariant
    {
        get { return (TinyMceThemeVariant)GetValue(RequestedThemeVariantProperty); }
        set { SetValue(RequestedThemeVariantProperty, value); }
    }
    
    private static void OnHtmlTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((TinyMceView)d).SendCurrentText();
    }
    
    private static void OnOtherPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((TinyMceView)d).RebuildPage();
    }

    private void RebuildPage()
    {
        if (!IsLoaded)
        {
            return;
        }

        var topLevel = Window.GetWindow(this);

        var html = HtmlPageBuilder.Build(
            LoadTinyMceStyle(RequestedThemeVariant),
            JsonEncodedText.Encode(LoadTinyMceContentStyle(RequestedThemeVariant)).ToString(),
            "Arial",
            (int)FontSize,
            (Background as SolidColorBrush ?? topLevel?.Background as SolidColorBrush)?.Color.ToString(),
            (Foreground as SolidColorBrush ?? topLevel?.Foreground as SolidColorBrush)?.Color.ToString(),
            ToolBar,
            Plugins);
        _nativeWebView.NavigateToString(html);
    }
}
