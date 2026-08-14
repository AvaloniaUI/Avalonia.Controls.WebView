# OAuth 2.0 authorization code + PKCE sample

This app discovers the endpoints of an authorization server, starts an authorization code request with PKCE (S256), completes login through `WebAuthenticationBroker`, then exchanges the code at the `token_endpoint`.

Discovery tries the RFC 8414 URL (`/.well-known/oauth-authorization-server`) first and falls back to OpenID Connect discovery, so issuers that publish only one of the two work.

Register a public client with your identity provider and add the redirect URI you use here. The issuer must be an `https` URL, and its endpoints must be `https` as well.
