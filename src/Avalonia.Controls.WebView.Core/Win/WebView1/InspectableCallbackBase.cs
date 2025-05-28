using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Controls.Win.WebView1.Interop;
using Avalonia.Controls.Win.WebView2;

namespace Avalonia.Controls.Win.WebView1;

[SupportedOSPlatform("windows")]
internal abstract class InspectableCallbackBase : CallbackBase, IInspectable
{
#if !COM_SOURCE_GEN
    public void _VtblGap1_3() { }
#endif

    protected abstract Guid[] GetIids();
    
    unsafe void IInspectable.GetIids(out ulong iidCount, out IntPtr iids)
    {
        var guids = GetIids(); 
        iidCount = (ulong)guids.Length;
        var ptr = (Guid*)Marshal.AllocHGlobal(sizeof(Guid) * guids.Length);
        for (var i = 0; i < guids.Length; i++)
        {
            ptr[i] = guids[i];
        }

        iids = new IntPtr(ptr);
    }

    void IInspectable.GetRuntimeClassName(out IntPtr className)
    {
        var name = GetType().FullName;
        className = new HStringInterop(name).Handle;
    }

    int IInspectable.GetTrustLevel()
    {
        return 0;
    }
}
