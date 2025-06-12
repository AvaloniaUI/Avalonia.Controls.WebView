using System;
using System.Threading.Tasks;
using Avalonia.Controls;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Avalonia.Platform;

internal class HeadlessWebViewEnvironmentRequestedEventArgs : WebViewEnvironmentRequestedEventArgs
{
    /// <summary>
    /// Optional: Allows injecting a custom script engine for testing.
    /// </summary>
    public Func<string, Task<ScriptResult?>>? ScriptEngine { get; set; }

    /// <summary>
    /// Optional: Allows injecting a custom HTTP handler for navigation.
    /// </summary>
    public Func<Uri, Task<HttpResult>>? HttpHandler { get; set; }

    /// <summary>
    /// Optional: Allows async initialization logic for the headless adapter. Emulating delayed set-up.
    /// </summary>
    public Func<Task>? InitializeAsync { get; set; }

    public record ScriptResult(ScriptResultCommand Command, string? Argument);
    public enum ScriptResultCommand
    {
        None,
        InvokeCSharpAction,
        OpenNewWindow,
        OpenLink,
        GetHtmlContent
    }

    public record HttpResult(bool IsSuccess, string Content);
}
