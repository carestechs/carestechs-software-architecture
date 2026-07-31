---
category: api
stack: any
status: Active
requires: []
conflicts_with: []
last_reviewed: 2026-07-31
---

# Machine-to-Machine Authentication with API Tokens

## Decision
Third-party integration clients (server-to-server consumers of the platform's API) authenticate with opaque API tokens, not user JWTs. A token is an identifiable prefix plus a random secret; the server stores only a hash. A dedicated authorizer validates the token and injects tenant scope and permissions into the request context. Tokens are tenant-scoped, permission-scoped, individually revocable, and rotatable. Human authentication (Cognito or self-issued JWT) and machine authentication coexist as separate paths with separate authorizers.

## Rationale
- Integration consumers cannot do interactive login, and issuing them long-lived user JWTs conflates two threat models: a leaked user token expires in minutes; an integration credential lives until rotated, so it needs revocation, scoping, and audit — API-token machinery, not session machinery.
- Opaque tokens keep validation server-side: revocation is immediate (no waiting out a JWT's lifetime), and nothing about internal claims structure leaks into partner systems.
- The identifiable prefix (e.g., `pk_live_`-style) makes tokens greppable in leaks and lets the authorizer look up the hash record without scanning.
- Alternatives considered: OAuth client-credentials via the managed identity provider (viable for OIDC-speaking partners — costs each partner a token-endpoint round trip and ties machine auth to user-pool infrastructure; adopt it later without breaking this model), mTLS (rejected — certificate distribution to third parties is operationally heavier than the problem), signed request schemes à la SigV4 (rejected — high partner integration friction).

## Constraints (non-negotiable for AI)
- Store ONLY a hash of the token secret (SHA-256 or better). A plaintext token exists exactly once: in the response that creates it.
- Tokens travel in the `Authorization` header. NEVER accept tokens from query strings or request bodies — they end up in logs and referrers.
- The authorizer resolves the token record by prefix and compares hashes in constant time; on success it injects tenant identifiers and permission scope into the request context. Handlers read the injected context — NEVER re-parse or re-validate the raw token downstream.
- Every token is scoped: to a tenant (organization/workspace) and to named permissions. A token without explicit scope is invalid, not all-powerful.
- Revocation MUST take effect within a bounded window: if the authorizer caches token records, the TTL is the revocation SLA — document it and keep it in minutes, not hours.
- Record `last_used_at` per token and expose it to operators — unused credentials are attack surface to be culled.
- Rate limits apply per token, not only per tenant, so one runaway integration cannot exhaust a workspace's quota.
