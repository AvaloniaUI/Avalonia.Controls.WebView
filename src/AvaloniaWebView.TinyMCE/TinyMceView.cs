using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace AvaloniaWebView.TinyMCE;

internal record JsPayload(string type, string body);

public class TinyMceView : ThemeVariantScope
{
    private readonly NativeWebView _nativeWebView;
    private bool _ignoreChanges;

    public TinyMceView()
    {
        Child = _nativeWebView = new NativeWebView();
        _nativeWebView.WebMessageReceived += NativeWebViewOnWebMessageReceived;
        _nativeWebView.AttachedToVisualTree += NativeWebViewOnAttachedToVisualTree;
        _nativeWebView.NavigationCompleted += NativeWebViewOnNavigationCompleted;
    }

    public static readonly StyledProperty<string?> HtmlTextProperty = AvaloniaProperty.Register<TinyMceView, string?>(nameof(HtmlText));

    public string? HtmlText
    {
        get => GetValue(HtmlTextProperty);
        set => SetValue(HtmlTextProperty, value);
    }

    public static readonly StyledProperty<double> FontSizeProperty = TextElement.FontSizeProperty.AddOwner<TinyMceView>();

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }
    
    public static readonly StyledProperty<IBrush?> BackgroundProperty = Border.BackgroundProperty.AddOwner<TinyMceView>();

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public static readonly StyledProperty<IBrush?> ForegroundProperty = TextElement.ForegroundProperty.AddOwner<TinyMceView>();

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }
    
    public static readonly StyledProperty<string> ToolBarProperty = AvaloniaProperty.Register<TinyMceView, string>(nameof(ToolBar),
        "bold italic underline bullist numlist fontselect fontsizeselect");

    public string ToolBar
    {
        get => GetValue(ToolBarProperty);
        set => SetValue(ToolBarProperty, value);
    }

    public static readonly StyledProperty<string> PluginsProperty = AvaloniaProperty.Register<TinyMceView, string>(nameof(Plugins),
        "autoresize fullpage");

    public string Plugins
    {
        get => GetValue(PluginsProperty);
        set => SetValue(PluginsProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == HtmlTextProperty)
        {
            SendCurrentText();
        }
        else if (change.Property == ToolBarProperty
                 || change.Property == ThemeVariantScope.ActualThemeVariantProperty
                 || change.Property == FontSizeProperty)
        {
            RebuildPage();
        }
    }

    private void NativeWebViewOnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        RebuildPage();
    }

    protected virtual string LoadTinyMceStyle(PlatformThemeVariant variant)
    {
        return variant == PlatformThemeVariant.Dark ? HtmlPageBuilder.CharcoalStyle : HtmlPageBuilder.LightGrayStyle;
    }
    
    protected virtual string LoadTinyMceContentStyle(PlatformThemeVariant variant)
    {
        return variant == PlatformThemeVariant.Dark ? HtmlPageBuilder.ContentDarkStyle : HtmlPageBuilder.ContentLightStyle;
    }

    private void RebuildPage()
    {
        if (!this.IsAttachedToVisualTree())
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        
        var html = HtmlPageBuilder.Build(
            LoadTinyMceStyle((PlatformThemeVariant?)ActualThemeVariant ?? PlatformThemeVariant.Light),
            JsonEncodedText.Encode(LoadTinyMceContentStyle((PlatformThemeVariant?)ActualThemeVariant ?? PlatformThemeVariant.Light)).ToString(),
            "Arial",
            (int)FontSize,
            (Background as ISolidColorBrush ?? topLevel?.Background as ISolidColorBrush)?.Color,
            (Foreground as ISolidColorBrush ?? topLevel?.Foreground as ISolidColorBrush)?.Color,
            ToolBar,
            Plugins);
        _nativeWebView.NavigateToString(html);
    }
    
    private void NativeWebViewOnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        SendCurrentText();
    }

    private void NativeWebViewOnWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        if (_ignoreChanges) return;

        _ignoreChanges = true;
        var payload = JsonSerializer.Deserialize<JsPayload>(e.Body!);
        if (payload?.type == "textChanged")
        {
            SetCurrentValue(HtmlTextProperty, payload.body);
        }
        _ignoreChanges = false;
    }

    private async void SendCurrentText()
    {
        if (_ignoreChanges) return;

        _ignoreChanges = true;
        var payload = JsonSerializer.Serialize(new JsPayload("textChanging", HtmlText ?? ""));
        await _nativeWebView.InvokeScript($"sendPayload('{JsonEncodedText.Encode(payload)}')");
        _ignoreChanges = false;
    }
}
