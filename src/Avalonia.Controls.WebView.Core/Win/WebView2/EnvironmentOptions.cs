using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using Avalonia.Controls.Utils;
using Avalonia.Controls.Win.WebView2.Interop;
using Avalonia.Platform;

namespace Avalonia.Controls.Win.WebView2;

[GeneratedComClass]
[SupportedOSPlatform("windows")]
internal partial class EnvironmentOptions(WindowsWebView2EnvironmentRequestedEventArgs environmentArgs)
    : CallbackBase, ICoreWebView2EnvironmentOptions, ICoreWebView2EnvironmentOptions4, IEquatable<EnvironmentOptions>
{
    public string? UserDataFolder { get; } = environmentArgs.UserDataFolder;
    public string? BrowserExecutableFolder { get; } = environmentArgs.BrowserExecutableFolder;
    public string? AdditionalBrowserArguments { get; } = environmentArgs.AdditionalBrowserArguments;
    public string? Language { get; } = environmentArgs.Language;
    public bool AllowSingleSignOnUsingOSPrimaryAccount { get; } = environmentArgs.AllowSingleSignOnUsingOSPrimaryAccount;
    public string[] CustomSchemes { get; } = WebResourceResponseHelper.GetCustomSchemes(environmentArgs);

    public string? GetAdditionalBrowserArguments() => AdditionalBrowserArguments;
    public void SetAdditionalBrowserArguments(string value) { } 

    public string? GetLanguage() => Language;
    public void SetLanguage(string value) { }

    public string GetTargetCompatibleBrowserVersion() => "135.0.3179.45";
    public void SetTargetCompatibleBrowserVersion(string targetCompatibleBrowserVersion) { }

    public int GetAllowSingleSignOnUsingOSPrimaryAccount() => AllowSingleSignOnUsingOSPrimaryAccount ? 1 : 0;
    public void SetAllowSingleSignOnUsingOSPrimaryAccount(int value){}

    public unsafe void GetCustomSchemeRegistrations(out uint count, out IntPtr schemeRegistrations)
    {
        count = (uint)CustomSchemes.Length;
        if (count == 0)
        {
            schemeRegistrations = IntPtr.Zero;
            return;
        }

        var registrations = (void**)Marshal.AllocCoTaskMem(sizeof(void*) * CustomSchemes.Length);
        for (var i = 0; i < CustomSchemes.Length; i++)
        {
            registrations[i] = ComInterfaceMarshaller<ICoreWebView2CustomSchemeRegistration>.ConvertToUnmanaged(
                new CustomSchemeRegistration(CustomSchemes[i], CustomSchemes));
        }
        schemeRegistrations = (IntPtr)registrations;
    }

    public void SetCustomSchemeRegistrations(uint count, IntPtr schemeRegistrations) { }

    public bool Equals(EnvironmentOptions? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return UserDataFolder == other.UserDataFolder && BrowserExecutableFolder == other.BrowserExecutableFolder && AdditionalBrowserArguments == other.AdditionalBrowserArguments && Language == other.Language && AllowSingleSignOnUsingOSPrimaryAccount == other.AllowSingleSignOnUsingOSPrimaryAccount && CustomSchemes.SequenceEqual(other.CustomSchemes);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((EnvironmentOptions)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (UserDataFolder != null ? UserDataFolder.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (BrowserExecutableFolder != null ? BrowserExecutableFolder.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (AdditionalBrowserArguments != null ? AdditionalBrowserArguments.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Language != null ? Language.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ AllowSingleSignOnUsingOSPrimaryAccount.GetHashCode();
            foreach (var scheme in CustomSchemes)
                hashCode = (hashCode * 397) ^ scheme.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(EnvironmentOptions? left, EnvironmentOptions? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(EnvironmentOptions? left, EnvironmentOptions? right)
    {
        return !Equals(left, right);
    }
}

/// <summary>
/// Registers a scheme as secure and with an authority component, like https.
/// </summary>
[GeneratedComClass]
internal partial class CustomSchemeRegistration(string schemeName, string[] schemes) : CallbackBase, ICoreWebView2CustomSchemeRegistration
{
    public string GetSchemeName() => schemeName;

    public int GetTreatAsSecure() => 1;
    public void SetTreatAsSecure(int treatAsSecure) { }

    // Pages on the registered schemes only. Requests without an Origin header, such as images, are always allowed.
    public unsafe void GetAllowedOrigins(out uint allowedOriginsCount, out IntPtr allowedOrigins)
    {
        // The caller frees the array and the strings.
        var origins = (IntPtr*)Marshal.AllocCoTaskMem(sizeof(IntPtr) * schemes.Length);
        for (var i = 0; i < schemes.Length; i++)
            origins[i] = Marshal.StringToCoTaskMemUni(schemes[i] + Uri.SchemeDelimiter + "*");
        allowedOriginsCount = (uint)schemes.Length;
        allowedOrigins = (IntPtr)origins;
    }
    public void SetAllowedOrigins(uint allowedOriginsCount, IntPtr allowedOrigins) { }

    public int GetHasAuthorityComponent() => 1;
    public void SetHasAuthorityComponent(int hasAuthorityComponent) { }
}
