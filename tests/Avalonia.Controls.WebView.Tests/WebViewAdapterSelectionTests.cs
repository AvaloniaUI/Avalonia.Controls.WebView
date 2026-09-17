using Avalonia.Platform;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

public class WebViewAdapterSelectionTests
{
    [Fact]
    public void Should_Use_Default_Order_For_Null_Or_Empty_Preference()
    {
        WebViewAdapterType[] defaultOrder =
        [
            WebViewAdapterType.WebView2,
            WebViewAdapterType.WebView1
        ];

        Assert.Equal(defaultOrder, WebViewAdapter.GetAdapterTypes(null, defaultOrder));
        Assert.Equal(defaultOrder, WebViewAdapter.GetAdapterTypes([], defaultOrder));
    }

    [Fact]
    public void Should_Use_Explicit_Exhaustive_Order()
    {
        WebViewAdapterType[] preference =
        [
            WebViewAdapterType.WebKitGtk,
            WebViewAdapterType.WebView1,
            WebViewAdapterType.WebView2,
            WebViewAdapterType.WebView1
        ];

        WebViewAdapterType[] defaultOrder =
        [
            WebViewAdapterType.WebView2,
            WebViewAdapterType.WebView1
        ];

        var types = WebViewAdapter.GetAdapterTypes(
            preference,
            defaultOrder);

        Assert.Equal(
            [WebViewAdapterType.WebView1, WebViewAdapterType.WebView2],
            types);
    }

    [Fact]
    public void Should_Not_Append_Omitted_Or_Unsupported_Adapters()
    {
        var types = WebViewAdapter.GetAdapterTypes(
            [WebViewAdapterType.WpeWebKit],
            [WebViewAdapterType.WebView2, WebViewAdapterType.WebView1]);

        Assert.Empty(types);
    }
}
