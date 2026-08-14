using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.OAuth2;
using Xunit;

namespace Avalonia.Controls.WebView.Tests;

public class OAuth2Tests
{
    const string AuthorizationEndpoint = "https://id.example.com/authorize";
    const string TokenEndpoint = "https://id.example.com/token";

    static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void Should_Match_RFC7636_Appendix_B_Code_Challenge_Vector()
    {
        const string verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        var challenge = Pkce.CreateCodeChallengeS256(verifier);
        Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", challenge);
    }

    [Fact]
    public void Should_Create_Code_Verifier_Of_RFC7636_Minimum_Length()
    {
        var verifier = Pkce.CreateCodeVerifier();
        Assert.Equal(43, verifier.Length);
        Assert.NotEqual(verifier, Pkce.CreateCodeVerifier());
    }

    [Fact]
    public void Should_Insert_Well_Known_Segment_Before_Issuer_Path()
    {
        var urls = AuthorizationServerMetadataClient.GetWellKnownMetadataUrls(
            new Uri("https://host.example.com/realms/foo"));

        Assert.Equal(3, urls.Count);
        Assert.Equal("https://host.example.com/.well-known/oauth-authorization-server/realms/foo", urls[0]);
        Assert.Equal("https://host.example.com/.well-known/openid-configuration/realms/foo", urls[1]);
        Assert.Equal("https://host.example.com/realms/foo/.well-known/openid-configuration", urls[2]);
    }

    [Fact]
    public void Should_Not_Repeat_Well_Known_Url_For_Issuer_Without_Path()
    {
        var urls = AuthorizationServerMetadataClient.GetWellKnownMetadataUrls(
            new Uri("https://host.example.com/"));

        Assert.Equal(2, urls.Count);
        Assert.Equal("https://host.example.com/.well-known/oauth-authorization-server", urls[0]);
        Assert.Equal("https://host.example.com/.well-known/openid-configuration", urls[1]);
    }

    [Fact]
    public async Task Should_Reject_Issuer_That_Is_Not_Https()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            AuthorizationCodePkceSession.CreateAsync(
                "http://id.example.com", "client", "http://127.0.0.1:1234/cb", "openid", cancellationToken: Ct));
    }

    [Fact]
    public async Task Should_Reject_Metadata_Declaring_Another_Issuer()
    {
        using var client = StubClient($$"""
            {
              "issuer": "https://attacker.example.com",
              "authorization_endpoint": "{{AuthorizationEndpoint}}",
              "token_endpoint": "{{TokenEndpoint}}"
            }
            """);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AuthorizationCodePkceSession.CreateAsync(
                "https://id.example.com", "client", "http://127.0.0.1:1234/cb", "openid", httpClient: client, cancellationToken: Ct));

        Assert.Contains("attacker.example.com", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Should_Reject_Token_Endpoint_That_Is_Not_Https()
    {
        using var client = StubClient("""
            {
              "issuer": "https://id.example.com",
              "authorization_endpoint": "https://id.example.com/authorize",
              "token_endpoint": "http://id.example.com/token"
            }
            """);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AuthorizationCodePkceSession.CreateAsync(
                "https://id.example.com", "client", "http://127.0.0.1:1234/cb", "openid", httpClient: client, cancellationToken: Ct));
    }

    [Fact]
    public async Task Should_Discover_Metadata_And_Build_Authorization_Request()
    {
        using var client = StubClient("""
            {
              "issuer": "https://id.example.com",
              "authorization_endpoint": "https://id.example.com/authorize",
              "token_endpoint": "https://id.example.com/token",
              "code_challenge_methods_supported": [ "S256" ]
            }
            """);

        var session = await AuthorizationCodePkceSession.CreateAsync(
            "https://id.example.com", "my-client", "http://127.0.0.1:1234/cb", "openid", httpClient: client, cancellationToken: Ct);

        Assert.StartsWith("https://id.example.com/authorize?", session.AuthorizationUri.AbsoluteUri, StringComparison.Ordinal);
        Assert.Contains("client_id=my-client", session.AuthorizationUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Should_Throw_When_Metadata_Does_Not_Support_S256()
    {
        using var client = StubClient("""
            {
              "issuer": "https://id.example.com",
              "authorization_endpoint": "https://id.example.com/authorize",
              "token_endpoint": "https://id.example.com/token",
              "code_challenge_methods_supported": [ "plain" ]
            }
            """);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AuthorizationCodePkceSession.CreateAsync(
                "https://id.example.com", "client", "http://127.0.0.1:1234/cb", "openid", httpClient: client, cancellationToken: Ct));
    }

    [Fact]
    public void Should_Build_Authorization_Request_Uri()
    {
        var session = CreateSession(scope: "openid offline_access");

        Assert.Contains("response_type=code", session.AuthorizationUri.Query, StringComparison.Ordinal);
        Assert.Contains("code_challenge_method=S256", session.AuthorizationUri.Query, StringComparison.Ordinal);
        Assert.Contains("client_id=my-client", session.AuthorizationUri.Query, StringComparison.Ordinal);
        Assert.Contains("scope=openid%20offline_access", session.AuthorizationUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_Preserve_Redirect_Uri_Without_Trailing_Slash()
    {
        var session = CreateSession(redirectUri: "http://localhost");

        Assert.Contains("redirect_uri=http%3A%2F%2Flocalhost&", session.AuthorizationUri.Query, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "redirect_uri=http%3A%2F%2Flocalhost%2F",
            session.AuthorizationUri.Query,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Should_Keep_Query_Already_Present_On_Authorization_Endpoint()
    {
        var session = CreateSession(authorizationEndpoint: "https://id.example.com/authorize?tenant=contoso");

        Assert.StartsWith("?tenant=contoso&response_type=code", session.AuthorizationUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_Put_Parameters_In_Query_When_Authorization_Endpoint_Has_Fragment()
    {
        var session = CreateSession(authorizationEndpoint: "https://id.example.com/authorize#top");

        Assert.Contains("response_type=code", session.AuthorizationUri.Query, StringComparison.Ordinal);
        Assert.Equal("#top", session.AuthorizationUri.Fragment);
    }

    [Fact]
    public void Should_Reject_Authorization_Endpoint_That_Is_Not_Https()
    {
        Assert.Throws<ArgumentException>(() => AuthorizationCodePkceSession.Create(
            new Uri("http://id.example.com/authorize"),
            new Uri(TokenEndpoint),
            "my-client",
            "http://127.0.0.1:1234/cb",
            "openid"));
    }

    [Fact]
    public void Should_Match_Callback_By_State()
    {
        var session = CreateSession();

        Assert.True(session.IsCallbackFor(Callback($"code=abc&state={session.State}")));
        Assert.False(session.IsCallbackFor(Callback("code=abc&state=wrong")));
        Assert.False(session.IsCallbackFor(Callback("code=abc")));
    }

    [Fact]
    public async Task Should_Throw_Before_Token_Request_When_State_Does_Not_Match()
    {
        var session = CreateSession();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.ExchangeCodeAsync(Callback("code=abc&state=wrong"), cancellationToken: Ct));
    }

    [Fact]
    public async Task Should_Throw_Before_Token_Request_When_Callback_Reports_Error()
    {
        var session = CreateSession();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.ExchangeCodeAsync(
                Callback($"error=access_denied&error_description=User%20said%20no&state={session.State}"), cancellationToken: Ct));

        Assert.Equal("access_denied: User said no", exception.Message);
    }

    [Fact]
    public async Task Should_Throw_Before_Token_Request_When_Callback_Has_No_Code()
    {
        var session = CreateSession();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.ExchangeCodeAsync(Callback($"state={session.State}"), cancellationToken: Ct));
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

    static AuthorizationCodePkceSession CreateSession(
        string authorizationEndpoint = AuthorizationEndpoint,
        string redirectUri = "http://127.0.0.1:1234/cb",
        string scope = "openid") =>
        AuthorizationCodePkceSession.Create(
            new Uri(authorizationEndpoint),
            new Uri(TokenEndpoint),
            "my-client",
            redirectUri,
            scope);

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
