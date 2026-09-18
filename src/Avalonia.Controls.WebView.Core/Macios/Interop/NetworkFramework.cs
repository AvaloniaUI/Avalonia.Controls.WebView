using System;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Macios.Interop;

internal static partial class NetworkFramework
{
    private const string Framework = "/System/Library/Frameworks/Network.framework/Network";

    [LibraryImport(Framework)]
    public static partial IntPtr nw_endpoint_create_host(
        [MarshalAs(UnmanagedType.LPStr)] string host,
        [MarshalAs(UnmanagedType.LPStr)] string port);

    [LibraryImport(Framework)]
    public static partial IntPtr nw_proxy_config_create_http_connect(IntPtr endpoint, IntPtr tlsOptions);
    [LibraryImport(Framework)]
    public static partial IntPtr nw_proxy_config_create_socksv5(IntPtr endpoint);

    [LibraryImport(Framework)]
    public static partial IntPtr nw_tls_create_options();

    [LibraryImport(Framework)]
    public static partial void nw_proxy_config_add_excluded_domain(IntPtr config, [MarshalAs(UnmanagedType.LPStr)] string excludedDomain);

    [LibraryImport(Framework)]
    public static partial void nw_proxy_config_set_username(IntPtr config, [MarshalAs(UnmanagedType.LPStr)] string username);

    [LibraryImport(Framework)]
    public static partial void nw_proxy_config_set_password(IntPtr config, [MarshalAs(UnmanagedType.LPStr)] string password);

    [LibraryImport(Framework)]
    public static partial void nw_release(IntPtr obj);
}
