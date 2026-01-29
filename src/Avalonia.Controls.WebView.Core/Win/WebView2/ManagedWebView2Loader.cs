using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Logging;
using Microsoft.Win32;

namespace Avalonia.Controls.Win.WebView2;

[SupportedOSPlatform("windows")]
internal static class ManagedWebView2Loader
{
    private const string InstallKeyPath = @"Software\Microsoft\EdgeUpdate\ClientState\";

    private static readonly Dictionary<string, string> s_channelInfo = new()
    {
        { "{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}", "Stable" },
        { "{2CD8A007-E189-409D-A2C8-9AF4EF3C72AA}", "Beta" },
        { "{0D50BFEC-CD6A-4F9A-964C-C7416E3ACB10}", "Dev" },
        { "{65C35B14-6C1D-4122-AC46-7148CC9D6497}", "Canary" },
        { "{BE59E8FD-089A-411B-A3B0-051D9E417818}", "Internal" }
    };

    private static readonly Lazy<(string? runtimePath, string? version)> s_regeditLocation = new(FindRuntimeInRegistryGlobal);

    /// <summary>
    /// Finds the WebView2 runtime installation path using registry
    /// </summary>
    /// <returns>Path to the WebView2 runtime DLL, or null if not found</returns>
    public static (string? path, string? version) FindWebView2Runtime(string? browserFolderOverride)
    {
        if (string.IsNullOrEmpty(browserFolderOverride))
        {
            browserFolderOverride = Environment.GetEnvironmentVariable("WEBVIEW2_BROWSER_EXECUTABLE_FOLDER");
        }

        if (browserFolderOverride is { Length: > 0 })
        {
            var dllPath = ComputeDllPath(browserFolderOverride);
            if (File.Exists(dllPath))
            {
                Logger.TryGet(LogEventLevel.Information, "WebView")?
                    .Log(null, "Found WebView2 runtime using custom browser executable folder at: {RuntimePath}", dllPath);
                return (dllPath, null);
            }
            else
            {
                Logger.TryGet(LogEventLevel.Warning, "WebView")?
                    .Log(null, "EmbeddedBrowserWebView.dll cannot be found at custom browser executable folder.");
            }
        }

        return s_regeditLocation.Value;
    }

    private static (string? path, string? version) FindRuntimeInRegistryGlobal()
    {
        // Try HKLM first (machine-wide installation)
        foreach (var channel in s_channelInfo)
        {
            var (runtimePath, version) = FindRuntimeInRegistry(RegistryHive.LocalMachine, channel.Key);
            if (!string.IsNullOrEmpty(runtimePath))
            {
                Logger.TryGet(LogEventLevel.Information, "WebView")?
                    .Log(null, "Found WebView2 {Channel} runtime at: {RuntimePath} HKLM", channel.Key, runtimePath);
                return (runtimePath, version);
            }
        }

        // Then try HKCU (user installation)
        foreach (var channel in s_channelInfo)
        {
            var (runtimePath, version) = FindRuntimeInRegistry(RegistryHive.CurrentUser, channel.Key);
            if (!string.IsNullOrEmpty(runtimePath))
            {
                Logger.TryGet(LogEventLevel.Information, "WebView")?
                    .Log(null, "Found WebView2 {Channel} runtime at: {RuntimePath} HKCU", channel.Key, runtimePath);
                return (runtimePath, version);
            }
        }

        return default;
    }

    private static (string? path, string? version) FindRuntimeInRegistry(RegistryHive hive, string channelUuid)
    {
        // Using Registry32 view automatically handles WOW6432Node redirection
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry32);
        var keyPath = Path.Combine(InstallKeyPath, channelUuid);

        using var key = baseKey.OpenSubKey(keyPath);
        if (key == null)
            return default;

        // Look for the EBWebView value or any other values that might contain the path
        string? path = null;
        string? version = null;
        foreach (var valueName in key.GetValueNames())
        {
            if (key.GetValue(valueName) is not string value || value.Length == 0)
                continue;

            // Check if this value contains a path to the WebView2 runtime
            if (valueName == "EBWebView" ||
                (value.Contains("EBWebView") &&
                 Directory.Exists(value)))
            {
                var dllPath = ComputeDllPath(value);
                if (File.Exists(dllPath))
                {
                    path = dllPath;
                }
            }
            
            if (valueName == "pv")
            {
                version = value;
            }
        }

        return path != null ? (path, version) : default;
    }

    private static string ComputeDllPath(string browserFolder)
    {
        // Construct the path to the actual DLL based on architecture
        var architecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm => "arm",
            Architecture.Arm64 => "arm64",
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            _ => "x64"
        };
        var dllPath = Path.Combine(browserFolder, "EBWebView", architecture, "EmbeddedBrowserWebView.dll");
        return dllPath;
    }
}
