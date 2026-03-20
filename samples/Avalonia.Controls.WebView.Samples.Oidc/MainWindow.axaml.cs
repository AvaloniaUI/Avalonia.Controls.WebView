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

        try
        {
            AppendLog($"GET {AuthorizationServerMetadataClient.GetWellKnownMetadataUrl(issuer)}");
            var metadata = await AuthorizationServerMetadataClient.GetAsync(issuer).ConfigureAwait(true);
            if (metadata.AuthorizationEndpoint is { } ae)
                AppendLog($"authorization_endpoint: {ae}");
            if (metadata.TokenEndpoint is { } te)
                AppendLog($"token_endpoint: {te}");

            var session = AuthorizationCodePkceSession.Create(metadata, clientId, redirectText, scope);

            var options = new WebAuthenticatorOptions(session.AuthorizationUri, session.RedirectUri)
            {
                PreferNativeWebDialog = true,
            };

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null)
            {
                AppendLog("TopLevel not found.");
                return;
            }

            AppendLog("Opening WebAuthenticationBroker…");
            var result = await WebAuthenticationBroker.AuthenticateAsync(topLevel, options).ConfigureAwait(true);

            var parsed = AuthorizationCallbackParser.Parse(result.CallbackUri, session.State);
            AppendLog("Authorization code received; exchanging at token_endpoint…");

            var token = await AuthorizationServerTokenClient.ExchangeAuthorizationCodeAsync(
                metadata,
                clientId,
                parsed.AuthorizationCode,
                session.RedirectUriString,
                session.CodeVerifier).ConfigureAwait(true);

            var sb = new StringBuilder();
            sb.AppendLine("Token response:");
            sb.AppendLine($"  token_type: {token.TokenType}");
            sb.AppendLine($"  expires_in: {token.ExpiresIn}");
            sb.AppendLine($"  scope: {token.Scope}");
            if (!string.IsNullOrEmpty(token.AccessToken))
                sb.AppendLine($"  access_token: {Preview(token.AccessToken)}");
            if (!string.IsNullOrEmpty(token.IdToken))
                sb.AppendLine($"  id_token: {Preview(token.IdToken)}");
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
