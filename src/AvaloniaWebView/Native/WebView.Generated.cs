#pragma warning disable 108
// ReSharper disable RedundantUsingDirective
// ReSharper disable JoinDeclarationAndInitializer
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable UnusedType.Local
// ReSharper disable InconsistentNaming
// ReSharper disable RedundantNameQualifier
// ReSharper disable RedundantCast
// ReSharper disable IdentifierTypo
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantUnsafeContext
// ReSharper disable RedundantBaseQualifier
// ReSharper disable EmptyStatement
// ReSharper disable RedundantAttributeParentheses
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using MicroCom.Runtime;

namespace AvaloniaWebView.Interop
{
    internal unsafe partial interface IWebViewFactory : global::MicroCom.Runtime.IUnknown
    {
        INativeWebView CreateWebView(INativeWebViewHandlers handlers);
    }

    internal unsafe partial interface INativeWebView : global::MicroCom.Runtime.IUnknown
    {
        void* AsNsView();
        int CanGoBack { get; }

        int GoBack();
        int CanGoForward { get; }

        int GoForward();
        IAvnString Source { get; }

        void Navigate(IAvnString url);
        void NavigateToString(IAvnString text);
        int Refresh();
        int Stop();
        void InvokeScript(IAvnString script, int id);
    }

    internal unsafe partial interface INativeWebViewHandlers : global::MicroCom.Runtime.IUnknown
    {
        void OnScriptResult(int id, int isError, IAvnString result);
        void OnNavigationCompleted(IAvnString url, int success);
        void OnNavigationStarted(IAvnString url, int* cancel);
    }

    internal unsafe partial interface IAvnString : global::MicroCom.Runtime.IUnknown
    {
        void* Pointer();
        int Length();
    }
}

namespace AvaloniaWebView.Interop.Impl
{
    internal unsafe partial class __MicroComIWebViewFactoryProxy : global::MicroCom.Runtime.MicroComProxyBase, IWebViewFactory
    {
        public INativeWebView CreateWebView(INativeWebViewHandlers handlers)
        {
            void* __result;
            __result = (void*)((delegate* unmanaged[Stdcall]<void*, void*, void*>)(*PPV)[base.VTableSize + 0])(PPV, global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(handlers));
            return global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<INativeWebView>(__result, true);
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IWebViewFactory), new Guid("809c652e-7396-11d2-9771-00a0cfb4d50c"), (p, owns) => new __MicroComIWebViewFactoryProxy(p, owns));
        }

        protected __MicroComIWebViewFactoryProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 1;
    }

    unsafe class __MicroComIWebViewFactoryVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void* CreateWebViewDelegate(void* @this, void* handlers);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void* CreateWebView(void* @this, void* handlers)
        {
            IWebViewFactory __target = null;
            try
            {
                {
                    __target = (IWebViewFactory)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.CreateWebView(global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<INativeWebViewHandlers>(handlers, false));
                        return global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        protected __MicroComIWebViewFactoryVTable()
        {
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void*, void*>)&CreateWebView); 
#else
            base.AddMethod((CreateWebViewDelegate)CreateWebView); 
#endif
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IWebViewFactory), new __MicroComIWebViewFactoryVTable().CreateVTable());
    }

    internal unsafe partial class __MicroComINativeWebViewProxy : global::MicroCom.Runtime.MicroComProxyBase, INativeWebView
    {
        public void* AsNsView()
        {
            void* __result;
            __result = (void*)((delegate* unmanaged[Stdcall]<void*, void*>)(*PPV)[base.VTableSize + 0])(PPV);
            return __result;
        }

        public int CanGoBack
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 1])(PPV);
                return __result;
            }
        }

        public int GoBack()
        {
            int __result;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 2])(PPV);
            return __result;
        }

        public int CanGoForward
        {
            get
            {
                int __result;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 3])(PPV);
                return __result;
            }
        }

        public int GoForward()
        {
            int __result;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 4])(PPV);
            return __result;
        }

        public IAvnString Source
        {
            get
            {
                int __result;
                void* __marshal_ppv = null;
                __result = (int)((delegate* unmanaged[Stdcall]<void*, void*, int>)(*PPV)[base.VTableSize + 5])(PPV, &__marshal_ppv);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetSource failed", __result);
                return global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvnString>(__marshal_ppv, true);
            }
        }

        public void Navigate(IAvnString url)
        {
            int __result;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, void*, int>)(*PPV)[base.VTableSize + 6])(PPV, global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(url));
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("Navigate failed", __result);
        }

        public void NavigateToString(IAvnString text)
        {
            int __result;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, void*, int>)(*PPV)[base.VTableSize + 7])(PPV, global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(text));
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("NavigateToString failed", __result);
        }

        public int Refresh()
        {
            int __result;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 8])(PPV);
            return __result;
        }

        public int Stop()
        {
            int __result;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, int>)(*PPV)[base.VTableSize + 9])(PPV);
            return __result;
        }

        public void InvokeScript(IAvnString script, int id)
        {
            int __result;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, void*, int, int>)(*PPV)[base.VTableSize + 10])(PPV, global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(script), id);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("InvokeScript failed", __result);
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(INativeWebView), new Guid("e5aca67b-02b7-4129-aa79-d6e417210bda"), (p, owns) => new __MicroComINativeWebViewProxy(p, owns));
        }

        protected __MicroComINativeWebViewProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 11;
    }

    unsafe class __MicroComINativeWebViewVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate void* AsNsViewDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static void* AsNsView(void* @this)
        {
            INativeWebView __target = null;
            try
            {
                {
                    __target = (INativeWebView)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.AsNsView();
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetCanGoBackDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetCanGoBack(void* @this)
        {
            INativeWebView __target = null;
            try
            {
                {
                    __target = (INativeWebView)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.CanGoBack;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GoBackDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GoBack(void* @this)
        {
            INativeWebView __target = null;
            try
            {
                {
                    __target = (INativeWebView)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.GoBack();
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetCanGoForwardDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetCanGoForward(void* @this)
        {
            INativeWebView __target = null;
            try
            {
                {
                    __target = (INativeWebView)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.CanGoForward;
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GoForwardDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GoForward(void* @this)
        {
            INativeWebView __target = null;
            try
            {
                {
                    __target = (INativeWebView)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.GoForward();
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetSourceDelegate(void* @this, void** ppv);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int GetSource(void* @this, void** ppv)
        {
            INativeWebView __target = null;
            try
            {
                {
                    __target = (INativeWebView)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.Source;
                        *ppv = global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int NavigateDelegate(void* @this, void* url);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int Navigate(void* @this, void* url)
        {
            INativeWebView __target = null;
            try
            {
                {
                    __target = (INativeWebView)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.Navigate(global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvnString>(url, false));
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int NavigateToStringDelegate(void* @this, void* text);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int NavigateToString(void* @this, void* text)
        {
            INativeWebView __target = null;
            try
            {
                {
                    __target = (INativeWebView)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.NavigateToString(global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvnString>(text, false));
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int RefreshDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int Refresh(void* @this)
        {
            INativeWebView __target = null;
            try
            {
                {
                    __target = (INativeWebView)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.Refresh();
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int StopDelegate(void* @this);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int Stop(void* @this)
        {
            INativeWebView __target = null;
            try
            {
                {
                    __target = (INativeWebView)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.Stop();
                        return __result;
                    }
                }
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return default;
            }
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int InvokeScriptDelegate(void* @this, void* script, int id);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int InvokeScript(void* @this, void* script, int id)
        {
            INativeWebView __target = null;
            try
            {
                {
                    __target = (INativeWebView)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.InvokeScript(global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvnString>(script, false), id);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        protected __MicroComINativeWebViewVTable()
        {
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void*>)&AsNsView); 
#else
            base.AddMethod((AsNsViewDelegate)AsNsView); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetCanGoBack); 
#else
            base.AddMethod((GetCanGoBackDelegate)GetCanGoBack); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GoBack); 
#else
            base.AddMethod((GoBackDelegate)GoBack); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GetCanGoForward); 
#else
            base.AddMethod((GetCanGoForwardDelegate)GetCanGoForward); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&GoForward); 
#else
            base.AddMethod((GoForwardDelegate)GoForward); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void**, int>)&GetSource); 
#else
            base.AddMethod((GetSourceDelegate)GetSource); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void*, int>)&Navigate); 
#else
            base.AddMethod((NavigateDelegate)Navigate); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void*, int>)&NavigateToString); 
#else
            base.AddMethod((NavigateToStringDelegate)NavigateToString); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&Refresh); 
#else
            base.AddMethod((RefreshDelegate)Refresh); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int>)&Stop); 
#else
            base.AddMethod((StopDelegate)Stop); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void*, int, int>)&InvokeScript); 
#else
            base.AddMethod((InvokeScriptDelegate)InvokeScript); 
#endif
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(INativeWebView), new __MicroComINativeWebViewVTable().CreateVTable());
    }

    internal unsafe partial class __MicroComINativeWebViewHandlersProxy : global::MicroCom.Runtime.MicroComProxyBase, INativeWebViewHandlers
    {
        public void OnScriptResult(int id, int isError, IAvnString result)
        {
            int __result;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, int, int, void*, int>)(*PPV)[base.VTableSize + 0])(PPV, id, isError, global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(result));
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("OnScriptResult failed", __result);
        }

        public void OnNavigationCompleted(IAvnString url, int success)
        {
            int __result;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, void*, int, int>)(*PPV)[base.VTableSize + 1])(PPV, global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(url), success);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("OnNavigationCompleted failed", __result);
        }

        public void OnNavigationStarted(IAvnString url, int* cancel)
        {
            int __result;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, void*, void*, int>)(*PPV)[base.VTableSize + 2])(PPV, global::MicroCom.Runtime.MicroComRuntime.GetNativePointer(url), cancel);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("OnNavigationStarted failed", __result);
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(INativeWebViewHandlers), new Guid("e5aca67b-02b7-4129-aa79-d6e417210bba"), (p, owns) => new __MicroComINativeWebViewHandlersProxy(p, owns));
        }

        protected __MicroComINativeWebViewHandlersProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 3;
    }

    unsafe class __MicroComINativeWebViewHandlersVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int OnScriptResultDelegate(void* @this, int id, int isError, void* result);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int OnScriptResult(void* @this, int id, int isError, void* result)
        {
            INativeWebViewHandlers __target = null;
            try
            {
                {
                    __target = (INativeWebViewHandlers)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.OnScriptResult(id, isError, global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvnString>(result, false));
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int OnNavigationCompletedDelegate(void* @this, void* url, int success);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int OnNavigationCompleted(void* @this, void* url, int success)
        {
            INativeWebViewHandlers __target = null;
            try
            {
                {
                    __target = (INativeWebViewHandlers)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.OnNavigationCompleted(global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvnString>(url, false), success);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int OnNavigationStartedDelegate(void* @this, void* url, int* cancel);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int OnNavigationStarted(void* @this, void* url, int* cancel)
        {
            INativeWebViewHandlers __target = null;
            try
            {
                {
                    __target = (INativeWebViewHandlers)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    __target.OnNavigationStarted(global::MicroCom.Runtime.MicroComRuntime.CreateProxyOrNullFor<IAvnString>(url, false), cancel);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        protected __MicroComINativeWebViewHandlersVTable()
        {
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int, int, void*, int>)&OnScriptResult); 
#else
            base.AddMethod((OnScriptResultDelegate)OnScriptResult); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void*, int, int>)&OnNavigationCompleted); 
#else
            base.AddMethod((OnNavigationCompletedDelegate)OnNavigationCompleted); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void*, int*, int>)&OnNavigationStarted); 
#else
            base.AddMethod((OnNavigationStartedDelegate)OnNavigationStarted); 
#endif
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(INativeWebViewHandlers), new __MicroComINativeWebViewHandlersVTable().CreateVTable());
    }

    internal unsafe partial class __MicroComIAvnStringProxy : global::MicroCom.Runtime.MicroComProxyBase, IAvnString
    {
        public void* Pointer()
        {
            int __result;
            void* retOut = default;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, void*, int>)(*PPV)[base.VTableSize + 0])(PPV, &retOut);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("Pointer failed", __result);
            return retOut;
        }

        public int Length()
        {
            int __result;
            int ret = default;
            __result = (int)((delegate* unmanaged[Stdcall]<void*, void*, int>)(*PPV)[base.VTableSize + 1])(PPV, &ret);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("Length failed", __result);
            return ret;
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit()
        {
            global::MicroCom.Runtime.MicroComRuntime.Register(typeof(IAvnString), new Guid("233e094f-9b9f-44a3-9a6e-6948bbdd9fbb"), (p, owns) => new __MicroComIAvnStringProxy(p, owns));
        }

        protected __MicroComIAvnStringProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 2;
    }

    unsafe class __MicroComIAvnStringVTable : global::MicroCom.Runtime.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int PointerDelegate(void* @this, void** retOut);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int Pointer(void* @this, void** retOut)
        {
            IAvnString __target = null;
            try
            {
                {
                    __target = (IAvnString)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.Pointer();
                        *retOut = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int LengthDelegate(void* @this, int* ret);
#if NET5_0_OR_GREATER
        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })] 
#endif
        static int Length(void* @this, int* ret)
        {
            IAvnString __target = null;
            try
            {
                {
                    __target = (IAvnString)global::MicroCom.Runtime.MicroComRuntime.GetObjectFromCcw(new IntPtr(@this));
                    {
                        var __result = __target.Length();
                        *ret = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                global::MicroCom.Runtime.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        protected __MicroComIAvnStringVTable()
        {
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, void**, int>)&Pointer); 
#else
            base.AddMethod((PointerDelegate)Pointer); 
#endif
#if NET5_0_OR_GREATER
            base.AddMethod((delegate* unmanaged[Stdcall]<void*, int*, int>)&Length); 
#else
            base.AddMethod((LengthDelegate)Length); 
#endif
        }

        [System.Runtime.CompilerServices.ModuleInitializer()]
        internal static void __MicroComModuleInit() => global::MicroCom.Runtime.MicroComRuntime.RegisterVTable(typeof(IAvnString), new __MicroComIAvnStringVTable().CreateVTable());
    }
}