using System;

namespace Avalonia.Controls;

/// <summary>Identifies a failed initialization or cleanup attempt for a NativeWebView.</summary>
public sealed class WebViewInitializationFailedEventArgs : EventArgs
{
    internal WebViewInitializationFailedEventArgs(Exception exception, long attemptId, bool isCurrentAttempt)
    {
        Exception = exception;
        AttemptId = attemptId;
        IsCurrentAttempt = isCurrentAttempt;
    }

    /// <summary>The original error, or an aggregate preserving initialization and cleanup errors.</summary>
    public Exception Exception { get; }

    /// <summary>
    /// Identifier within this NativeWebView: zero is factory selection; positive values identify
    /// successive host initialization attempts. Reparenting a ready adapter does not create an attempt.
    /// </summary>
    public long AttemptId { get; }

    /// <summary>
    /// Whether this attempt was still current when the failure was reported. A canceled, detached
    /// attempt can fail later; that failure must not be mistaken for a newer adapter's failure.
    /// </summary>
    public bool IsCurrentAttempt { get; }
}
