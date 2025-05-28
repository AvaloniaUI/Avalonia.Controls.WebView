using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using Avalonia.Controls.Win.WebView2.Interop;

namespace Avalonia.Controls.Win.WebView2;

#if COM_SOURCE_GEN
[GeneratedComClass]
#endif
[SupportedOSPlatform("windows")]
internal partial class WebView2ExecuteScriptCompletedHandler : GenericCompletedHandler<string?>,
    ICoreWebView2ExecuteScriptCompletedHandler;
