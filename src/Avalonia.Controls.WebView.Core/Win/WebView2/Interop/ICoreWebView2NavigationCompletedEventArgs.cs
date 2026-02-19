using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.WebView2.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ComObjectWrapper)]
[Guid("30D68B7D-20D9-4752-A9CA-EC8448FBB5C1")]
internal partial interface ICoreWebView2NavigationCompletedEventArgs
{
    int GetIsSuccess();
    int GetWebErrorStatus();
    ulong GetNavigationId();
}
