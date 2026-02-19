using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Controls.Win.Interop;

[GeneratedComInterface]
[Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90")]
internal partial interface IInspectable
{
    void GetIids(out ulong iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    int GetTrustLevel();
}
