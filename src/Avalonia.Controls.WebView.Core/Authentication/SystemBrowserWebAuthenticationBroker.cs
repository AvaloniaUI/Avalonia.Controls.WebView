using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Controls.Authentication;

/// <summary>
/// Authentication flow that opens the request in the user's browser and captures the callback on a loopback HTTP listener.
/// </summary>
internal static class SystemBrowserWebAuthenticationBroker
{
    public static async Task<Uri> AuthenticateAsync(
        Uri requestUri,
        Uri redirectUri,
        TimeSpan timeout,
        Func<Uri, Task<bool>> launcher,
        Func<Uri, bool>? callbackFilter,
        Func<Uri, BrowserResponse, Task>? responseFactory,
        CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsBrowser())
        {
            throw new PlatformNotSupportedException(
                "The system browser authentication mode requires a local HTTP listener, which is not available in the browser.");
        }

        ValidateRedirectUri(redirectUri);

        using var listener = new LoopbackHttpListener(redirectUri);

        var actualRedirectUri = new UriBuilder(redirectUri) { Port = listener.Port }.Uri;

        if (actualRedirectUri != redirectUri)
        {
            // Coerce requestUri parameter
            requestUri = ReplaceRedirectUri(requestUri, actualRedirectUri);
        }

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // Start accepting before the browser is launched. The callback can arrive as soon as the
        // browser opens, and a launcher is not required to return before that happens.
        var callbackTask = listener.WaitForCallbackAsync(callbackFilter, responseFactory, linkedCts.Token);

        try
        {
            var success = false;
            Exception? error = null;
            try
            {
                success = await launcher(requestUri).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                error = ex;   
            }

            if (!success || error != null)
            {
                throw new InvalidOperationException(
                    $"Failed to open '{requestUri}' in the system browser.", error);
            }

            return await callbackTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested &&
                                                 !cancellationToken.IsCancellationRequested)
        {
            // A filter that never matches otherwise presents as a silent wait until the timeout.
            var rejected = listener.RejectedCallbackCount > 0
                ? $" {listener.RejectedCallbackCount} callback(s) were rejected by the callback filter."
                : "";

            throw new OperationCanceledException(
                $"Timed out after {timeout} waiting for the authentication callback.{rejected}");
        }
        finally
        {
            if (!callbackTask.IsCompleted)
            {
                await linkedCts.CancelAsync().ConfigureAwait(false);
                _ = callbackTask.ContinueWith(static t => _ = t.Exception, TaskScheduler.Default);
            }
        }
    }

    private static Uri ReplaceRedirectUri(Uri requestUri, Uri redirectUri)
    {
        var pairs = UriQuery.Split(requestUri.Query);
        var replacements = 0;
        var query = new StringBuilder();

        foreach (var (name, pair) in pairs)
        {
            if (query.Length > 0)
                query.Append('&');

            if (name != "redirect_uri")
            {
                query.Append(pair);
                continue;
            }

            replacements++;

            var separator = pair.IndexOf('=');
            query.Append(pair, 0, separator < 0 ? pair.Length : separator)
                .Append('=')
                .Append(Uri.EscapeDataString(redirectUri.AbsoluteUri));
        }

        if (replacements != 1)
        {
            throw new InvalidOperationException(
                "The request URI must contain exactly one 'redirect_uri' query parameter.");
        }

        return new UriBuilder(requestUri) { Query = query.ToString() }.Uri;
    }

    private static void ValidateRedirectUri(Uri redirectUri)
    {
        if (!redirectUri.IsAbsoluteUri)
        {
            throw new ArgumentException(
                "Redirect uri must be absolute when using the system browser.", nameof(redirectUri));
        }

        if (redirectUri.Scheme != Uri.UriSchemeHttp)
        {
            throw new ArgumentException(
                $"Redirect uri must use the '{Uri.UriSchemeHttp}' scheme when using the system browser, but was '{redirectUri.Scheme}'.",
                nameof(redirectUri));
        }

        if (!redirectUri.IsLoopback)
        {
            throw new ArgumentException(
                $"Redirect uri host must be a loopback address or 'localhost' when using the system browser, but was '{redirectUri.Host}'.",
                nameof(redirectUri));
        }
    }
}

