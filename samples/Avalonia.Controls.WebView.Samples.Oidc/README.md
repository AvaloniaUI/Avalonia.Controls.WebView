# OAuth 2.0 + PKCE sample (RFC 8414)

This app loads **Authorization Server Metadata** from `{issuer}/.well-known/oauth-authorization-server`, starts an **authorization code** request with **PKCE (S256)**, completes login via `WebAuthenticationBroker`, then **exchanges the code** at the metadata `token_endpoint`.

Register a public client with your identity provider and add the redirect URI you use here (for example `http://localhost`). The issuer must publish RFC 8414 metadata; if only OpenID Connect discovery is available, use an issuer that exposes both or a server that implements RFC 8414.
