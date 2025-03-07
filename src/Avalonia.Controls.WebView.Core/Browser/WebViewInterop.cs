#if DISABLE
using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Avalonia.Controls.WebView.Core.Browser
{
    internal static partial class WebViewInterop
    {
        [JSImport("globalThis.document.createElement")]
        public static partial JSObject CreateElement(string tagName);

        [JSImport("getActualLocation", "avwebview.js")]
        public static partial string? GetActualLocation(JSObject iframe);

        [JSImport("goBack", "avwebview.js")]
        public static partial bool GoBack(JSObject iframe);

        [JSImport("goForward", "avwebview.js")]
        public static partial bool GoForward(JSObject iframe);

        [JSImport("canGoBack", "avwebview.js")]
        public static partial bool CanGoBack(JSObject iframe);

        [JSImport("refresh", "avwebview.js")]
        public static partial bool Refresh(JSObject iframe);

        [JSImport("stop", "avwebview.js")]
        public static partial bool Stop(JSObject iframe);

        [JSImport("subscribe", "avwebview.js")]
        [return: JSMarshalAs<JSType.Function>]
        public static partial Action Subscribe(
            JSObject iframe,
            [JSMarshalAs<JSType.Function<JSType.String>>]
            Action<string> onload);

        [JSImport("eval", "avwebview.js")]
        public static partial Task<string?> Eval(JSObject iframe, string script);
    }
}
#endif
