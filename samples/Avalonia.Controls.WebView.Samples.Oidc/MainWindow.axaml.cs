using System;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.OAuth2;
using Avalonia.Interactivity;

namespace Avalonia.Controls.WebView.Samples.Oidc;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    async void SignIn_OnClick(object? sender, RoutedEventArgs e)
    {
        var issuer = IssuerBox.Text?.Trim() ?? "";
        var clientId = ClientIdBox.Text?.Trim() ?? "";
        var redirectText = RedirectBox.Text?.Trim() ?? "";
        var scope = ScopeBox.Text?.Trim() ?? "";

        if (issuer.Length == 0 || clientId.Length == 0 || redirectText.Length == 0 || scope.Length == 0)
        {
            AppendLog("Fill issuer, client ID, redirect URI, and scope.");
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            AppendLog("TopLevel not found.");
            return;
        }

        try
        {
            AppendLog($"Discovering {issuer}…");
            var session = await AuthorizationCodePkceSession
                .CreateAsync(issuer, clientId, redirectText, scope)
                .ConfigureAwait(true);
            AppendLog($"authorization_endpoint: {session.AuthorizationUri.GetLeftPart(UriPartial.Path)}");

            var options = new WebAuthenticatorOptions(session.AuthorizationUri, session.RedirectUri)
            {
                BrowserOptions = new BrowserOptions
                {
                    // Used when Mode is Browser: keeps the loopback listener waiting
                    // for a callback that carries this session's state.
                    CallbackFilter = session.IsCallbackFor,
                },
            };

            AppendLog("Opening WebAuthenticationBroker…");
            var result = await WebAuthenticationBroker.AuthenticateAsync(topLevel, options).ConfigureAwait(true);

            AppendLog("Authorization code received; exchanging at token_endpoint…");
            var token = await session.ExchangeCodeAsync(result).ConfigureAwait(true);

            var sb = new StringBuilder();
            sb.AppendLine("Token response:");
            sb.AppendLine($"  token_type: {token.TokenType}");
            sb.AppendLine($"  expires_in: {token.ExpiresIn}");
            sb.AppendLine($"  scope: {token.Scope}");
            if (!string.IsNullOrEmpty(token.AccessToken))
                sb.AppendLine($"  access_token: {Preview(token.AccessToken)}");
            if (!string.IsNullOrEmpty(token.IdToken))
                sb.AppendLine($"  id_token (not validated): {Preview(token.IdToken)}");
            if (!string.IsNullOrEmpty(token.RefreshToken))
                sb.AppendLine($"  refresh_token: {Preview(token.RefreshToken)}");
            AppendLog(sb.ToString());
        }
        catch (Exception ex)
        {
            AppendLog(ex.ToString());
        }
    }

    static string Preview(string value)
    {
        const int max = 48;
        return value.Length <= max ? value : value[..max] + "…";
    }

    void AppendLog(string line)
    {
        LogBox.Text += line + Environment.NewLine;
    }
}
