using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Avalonia.Controls.Macios.Interop;

internal class NSPrintOperation(IntPtr handle, bool owns) : NSObject(handle, owns)
{
    private static readonly IntPtr s_class = Foundation.objc_getClass("NSPrintOperation");
    private static readonly IntPtr s_runOperation = Libobjc.sel_getUid("runOperation");
    private static readonly IntPtr s_runOperationModalForWindow = Libobjc.sel_getUid("runOperationModalForWindow:delegate:didRunSelector:contextInfo:");

    public bool RunOperation() => Libobjc
        .int_objc_msgSend(Handle, s_runOperation) == 1;
    public async Task<bool> RunOperationModalForWindow(IntPtr window)
    {
        using var callback = new AvnNSPrintOperationCallback();
        Libobjc.void_objc_msgSend(
            Handle,
            s_runOperationModalForWindow,
            window,
            callback.Handle,
            AvnNSPrintOperationCallback.s_didRunSelector,
            IntPtr.Zero);
        return await callback.Task;
    }

    public unsafe class AvnNSPrintOperationCallback : NSManagedObjectBase
    {
        private static readonly IntPtr s_class;
        private static readonly delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, int, IntPtr, void>
            s_callback = &OnCallback;
        internal static readonly IntPtr s_didRunSelector =
            Libobjc.sel_getUid("printOperationDidRun:success:contextInfo:");
        private readonly TaskCompletionSource<bool> _tcs = new();

        static AvnNSPrintOperationCallback()
        {
            var delegateClass = AllocateClassPair("AvnNSPrintOperationCallback");

            AddMethod(delegateClass, s_didRunSelector, new IntPtr(s_callback), "v@:@i@");

            RegisterManagedMembers(delegateClass);

            Libobjc.objc_registerClassPair(delegateClass);
            s_class = delegateClass;
        }

        public AvnNSPrintOperationCallback() : base(s_class)
        {
            Init();
        }

        public Task<bool> Task => _tcs.Task;

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void OnCallback(IntPtr self, IntPtr sel, IntPtr op, int success, IntPtr data)
        {
            var managed = ReadManagedSelf<AvnNSPrintOperationCallback>(self);
            managed?._tcs.SetResult(success == 1);
        }
    }
}
