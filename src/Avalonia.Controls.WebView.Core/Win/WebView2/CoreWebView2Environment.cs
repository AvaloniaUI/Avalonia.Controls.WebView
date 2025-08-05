using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Controls.Utils;
using Avalonia.Controls.Win.WebView2.Interop;
using Avalonia.Logging;
using Avalonia.Platform;

// ReSharper disable InconsistentNaming
namespace Avalonia.Controls.Win.WebView2;

[SupportedOSPlatform("windows")]
internal static partial class CoreWebView2Environment
{
    private enum WebView2RunTimeType { kInstalled = 0x0, kRedistributable = 0x1 }

    private static readonly Dictionary<EnvironmentOptions, TaskCompletionSource<ICoreWebView2Environment>> s_environments = new();

    public static Task<ICoreWebView2Environment> CreateAsync(WindowsWebView2EnvironmentRequestedEventArgs environmentArgs)
    {
        if (environmentArgs.ExplicitEnvironment is var customEnv && customEnv != IntPtr.Zero)
        {
            unsafe
            {
                var managed = ComInterfaceMarshaller<ICoreWebView2Environment>.ConvertToManaged(customEnv.ToPointer());
                return managed is not null ? Task.FromResult(managed) : Task.FromException<ICoreWebView2Environment>(
                    new InvalidOperationException("Unable to resolve managed COM interface from the ExplicitEnvironment handle"));
            }
        }

        var options = new EnvironmentOptions(environmentArgs);
        return GetOrCreateEnvForOptions(options);
    }

    private static Task<ICoreWebView2Environment> GetOrCreateEnvForOptions(EnvironmentOptions options)
    {
        if (!s_environments.TryGetValue(options, out var tcs))
        {
            var runtimeFunc = TryFindWebView2Runtime(options.BrowserExecutableFolder);
            if (runtimeFunc == IntPtr.Zero)
            {
                tcs = new TaskCompletionSource<ICoreWebView2Environment>();
                tcs.SetException(new InvalidOperationException("WebView2 runtime not found or CreateWebViewEnvironmentWithOptionsInternal not exported."));
            }
            else
            {
                var envCallback = new WebView2EnvHandler();
                var res = (uint)CreateEnv(runtimeFunc, WebView2RunTimeType.kInstalled, options.UserDataFolder, options, envCallback);
                if (res == 0x80010106)
                {
                    envCallback.Result.TrySetException(new InvalidOperationException("WebView2 requires UI thread to have STAThread flag/attribute set."));
                }
                else if (res != 0)
                {
                    envCallback.Result.TrySetException(Marshal.GetExceptionForHR((int)res) ?? new Win32Exception((int)res));
                }
                tcs = envCallback.Result;
            }
            s_environments[options] = tcs;
        }

        return tcs.Task;
    }

    private static unsafe int CreateEnv(IntPtr createEnvProc, WebView2RunTimeType runTimeType, string? userDataFolder, EnvironmentOptions options, WebView2EnvHandler envCallback)
    {
        var callbackPtr = ComInterfaceMarshaller<ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler>.ConvertToUnmanaged(envCallback);
        var optionsPtr = ComInterfaceMarshaller<ICoreWebView2EnvironmentOptions>.ConvertToUnmanaged(options);
        try
        {
            // TODO, we might want to keep userDataFolder pinned until callback is called.
            // But it's null anyway atm, so ignoring.
            var createEnvFunc = (delegate* unmanaged[Stdcall]<int, WebView2RunTimeType, IntPtr, void*, void*, int>)createEnvProc;
            fixed (char* userDataFolderPtr = userDataFolder)
                return createEnvFunc(1, runTimeType, new IntPtr(userDataFolderPtr), optionsPtr, callbackPtr);
        }
        finally
        {
            ComInterfaceMarshaller<ICoreWebView2EnvironmentOptions>.Free(optionsPtr);
            ComInterfaceMarshaller<ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler>.Free(callbackPtr);
        }
    }

    public static IntPtr TryFindWebView2Runtime(string? browserExecutableFolder)
    {
        var webViewRuntime = ManagedWebView2Loader.FindWebView2Runtime(browserExecutableFolder);
        if (webViewRuntime is null)
        {
            Logger.TryGet(LogEventLevel.Warning, "WebView")
                ?.Log(null, "WebView2 runtime not found. WebView2 will not be initialized.");
            return IntPtr.Zero;
        }

        if (!NativeLibraryEx.TryLoad(webViewRuntime, out var lib))
        {
            Logger.TryGet(LogEventLevel.Warning, "WebView")
                ?.Log(null, "WebView2 runtime was found, but unable to load from {RuntimePath}.", webViewRuntime);
            return IntPtr.Zero;
        }

        if (!NativeLibraryEx.TryGetExport(lib, "CreateWebViewEnvironmentWithOptionsInternal", out var createEnvPtr))
        {
            Logger.TryGet(LogEventLevel.Warning, "WebView")
                ?.Log(null , "CreateWebViewEnvironmentWithOptionsInternal not found in WebView2 runtime.");
            return IntPtr.Zero;
        }

        return createEnvPtr;
    }

#if COM_SOURCE_GEN
    [GeneratedComClass]
#endif
    private partial class WebView2EnvHandler : GenericCompletedHandler<ICoreWebView2Environment>,
        ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler;
}
