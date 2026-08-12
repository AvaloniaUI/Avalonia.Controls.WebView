using System;
using System.Runtime.InteropServices;

namespace Avalonia.Controls.Macios.Interop.AuthenticationServices;

internal partial class AuthenticationServices
{
    private const string AuthenticationServicesFramework = "/System/Library/Frameworks/AuthenticationServices.framework/AuthenticationServices";

    private static bool? s_isLoaded;
    public static bool PreloadAuthenticationServices()
        => s_isLoaded ??= objc_getClass("ASWebAuthenticationSession") != IntPtr.Zero;

    [LibraryImport(AuthenticationServicesFramework, StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr objc_getClass(string className);
}

