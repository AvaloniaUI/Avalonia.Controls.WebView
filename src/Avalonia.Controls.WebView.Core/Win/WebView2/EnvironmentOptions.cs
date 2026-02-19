using System;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Win.WebView2.Interop;
using Avalonia.Platform;

namespace Avalonia.Controls.Win.WebView2;

[GeneratedComClass]
internal partial class EnvironmentOptions(WindowsWebView2EnvironmentRequestedEventArgs environmentArgs)
    : CallbackBase, ICoreWebView2EnvironmentOptions, IEquatable<EnvironmentOptions>
{
    public string? UserDataFolder { get; } = environmentArgs.UserDataFolder;
    public string? BrowserExecutableFolder { get; } = environmentArgs.BrowserExecutableFolder;
    public string? AdditionalBrowserArguments { get; } = environmentArgs.AdditionalBrowserArguments;
    public string? Language { get; } = environmentArgs.Language;
    public bool AllowSingleSignOnUsingOSPrimaryAccount { get; } = environmentArgs.AllowSingleSignOnUsingOSPrimaryAccount;

    public string? GetAdditionalBrowserArguments() => AdditionalBrowserArguments;
    public void SetAdditionalBrowserArguments(string value) { } 

    public string? GetLanguage() => Language;
    public void SetLanguage(string value) { }

    public string GetTargetCompatibleBrowserVersion() => "135.0.3179.45";
    public void SetTargetCompatibleBrowserVersion(string targetCompatibleBrowserVersion) { }

    public int GetAllowSingleSignOnUsingOSPrimaryAccount() => AllowSingleSignOnUsingOSPrimaryAccount ? 1 : 0;
    public void SetAllowSingleSignOnUsingOSPrimaryAccount(int value){}

    public bool Equals(EnvironmentOptions? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return UserDataFolder == other.UserDataFolder && BrowserExecutableFolder == other.BrowserExecutableFolder && AdditionalBrowserArguments == other.AdditionalBrowserArguments && Language == other.Language && AllowSingleSignOnUsingOSPrimaryAccount == other.AllowSingleSignOnUsingOSPrimaryAccount;
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
