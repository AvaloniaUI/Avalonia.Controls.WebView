using System;
using System.Net;
using Avalonia.Controls;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Avalonia.Platform;

public sealed class AppleWKWebViewEnvironmentRequestedEventArgs : WebViewEnvironmentRequestedEventArgs
{
    internal AppleWKWebViewEnvironmentRequestedEventArgs(DeferralManager deferralManager) : base(deferralManager)
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use a non-persistent data store that stores website data in memory and does not write data to disk.
    /// </summary>
    public bool NonPersistentDataStore { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for a persistent data store object.
    /// </summary>
    public Guid DataStoreIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the application name that appears in the user agent string.
    /// </summary>
    public string? ApplicationNameForUserAgent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the web view should automatically upgrade supported HTTP requests to HTTPS.
    /// </summary>
    public bool UpgradeKnownHostsToHTTPS { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the web view limits navigation to pages within the application's domain.
    /// </summary>
    public bool LimitsNavigationsToAppBoundDomains { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the name of the script message handler.
    /// </summary>
    public string? ScriptHandlerMessageName { get; set; }  

    /// <summary>
    /// Gets or sets the proxy address.
    /// </summary>
    public Uri? ProxyAddress { get; set; }

    /// <summary>
    /// Gets or sets the credentials used to authenticate with the proxy.
    /// If set, this takes precedence over credentials embedded in <see cref="ProxyAddress"/>.
    /// </summary>
    public NetworkCredential? ProxyCredentials { get; set; }

    /// <summary>
    /// Gets or sets the list of domains to exclude from the proxy.
    /// </summary>
    public string[]? ProxyExcludedDomains { get; set; }
}
