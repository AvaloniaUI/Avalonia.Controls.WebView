namespace Avalonia.Controls;

/// <summary>
/// Maps <c>WebKitWebProcessTerminationReason</c> values, shared by the WebKitGTK and WPE WebKit ports.
/// </summary>
internal static class WebKitWebProcessTermination
{
    // WebKitWebProcessTerminationReason
    private enum Reason
    {
        Crashed = 0,
        ExceededMemoryLimit = 1,
        TerminatedByApi = 2
    }

    internal static string GetReasonName(int reason) => reason switch
    {
        (int)Reason.Crashed => "Crashed",
        (int)Reason.ExceededMemoryLimit => "ExceededMemoryLimit",
        (int)Reason.TerminatedByApi => "TerminatedByApi",
        _ => $"Unknown({reason})"
    };
}
