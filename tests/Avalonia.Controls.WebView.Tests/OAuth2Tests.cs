using System;
using Avalonia.Controls.OAuth2;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

public class OAuth2Tests
{
    [Fact]
    public void Creating_S256_code_challenge_should_match_RFC7636_appendix_B_vector()
    {
        const string verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        var challenge = Pkce.CreateCodeChallengeS256(verifier);
        Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", challenge);
    }

    [Fact]
    public void Metadata_client_should_build_RFC8414_well_known_URL()
    {
        var url = AuthorizationServerMetadataClient.GetWellKnownMetadataUrl("https://example.com/tenant/");
        Assert.Equal("https://example.com/tenant/.well-known/oauth-authorization-server", url);
    }

    [Fact]
    public void Authorization_callback_parser_should_parse_code_and_validate_state()
    {
        var callback = new Uri("http://localhost/callback?code=abc&state=xyz");
        var r = AuthorizationCallbackParser.Parse(callback, "xyz");
        Assert.Equal("abc", r.AuthorizationCode);
    }

    [Fact]
    public void Authorization_callback_parser_should_throw_when_state_mismatch()
    {
        var callback = new Uri("http://localhost/callback?code=abc&state=wrong");
        Assert.Throws<InvalidOperationException>(() => AuthorizationCallbackParser.Parse(callback, "xyz"));
    }

    [Fact]
    public void Authorization_code_pkce_session_should_throw_when_S256_not_supported()
    {
        var metadata = new AuthorizationServerMetadata
        {
            AuthorizationEndpoint = "https://id.example.com/authorize",
            CodeChallengeMethodsSupported = new[] { "plain" },
        };

        Assert.Throws<InvalidOperationException>(() =>
            AuthorizationCodePkceSession.Create(
                metadata,
                "client",
                "http://localhost/cb",
                "openid"));
    }

    [Fact]
    public void Authorization_code_pkce_session_should_build_authorization_request_uri()
    {
        var metadata = new AuthorizationServerMetadata
        {
            AuthorizationEndpoint = "https://id.example.com/authorize",
            CodeChallengeMethodsSupported = new[] { "S256" },
        };

        var session = AuthorizationCodePkceSession.Create(
            metadata,
            "my-client",
            "http://localhost/cb",
            "openid offline_access");

        Assert.Equal("http://localhost/cb", session.RedirectUriString);
        Assert.Contains("response_type=code", session.AuthorizationUri.Query, StringComparison.Ordinal);
        Assert.Contains("code_challenge_method=S256", session.AuthorizationUri.Query, StringComparison.Ordinal);
        Assert.Contains("client_id=my-client", session.AuthorizationUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public void Authorization_code_pkce_session_should_preserve_redirect_uri_without_trailing_slash()
    {
        var metadata = new AuthorizationServerMetadata
        {
            AuthorizationEndpoint = "https://id.example.com/authorize",
            CodeChallengeMethodsSupported = new[] { "S256" },
        };

        var session = AuthorizationCodePkceSession.Create(
            metadata,
            "id",
            "http://localhost",
            "openid");

        Assert.Equal("http://localhost", session.RedirectUriString);
        Assert.Contains("redirect_uri=http%3A%2F%2Flocalhost&", session.AuthorizationUri.Query, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "redirect_uri=http%3A%2F%2Flocalhost%2F",
            session.AuthorizationUri.Query,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Should_Read_Expires_In_Sent_As_A_String()
    {
        var session = CreateSession();
        using var client = StubClient("""
            { "access_token": "at", "token_type": "Bearer", "expires_in": "3600" }
            """);

        var token = await session.ExchangeCodeAsync(
            Callback($"code=abc&state={session.State}"), httpClient: client, cancellationToken: Ct);

        Assert.Equal(3600, token.ExpiresIn);
        Assert.Equal("at", token.AccessToken);
    }

    [Fact]
    public void Should_Parse_Callback_Parameters()
    {
        var result = Callback("code=abc&state=xyz&error_description=not%20used");

        Assert.Equal("abc", result.Code);
        Assert.Equal("xyz", result.State);
        Assert.Null(result.Error);
        Assert.Equal("not used", result.ErrorDescription);
        Assert.Equal(3, result.Parameters.Count);
    }

    [Fact]
    public void Should_Reparse_Parameters_After_Callback_Uri_Is_Replaced()
    {
        var result = Callback("code=first&state=xyz");
        Assert.Equal("first", result.Code);

        var replaced = result with { CallbackUri = new Uri("http://127.0.0.1:1234/cb?code=second&state=xyz") };
        Assert.Equal("second", replaced.Code);
    }


    static WebAuthenticationResult Callback(string query) =>
        new(new Uri($"http://127.0.0.1:1234/cb?{query}"));

    static HttpClient StubClient(string json) => new(new StubHandler(json));

    sealed class StubHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            });
    }
}
