# JWT Bearer Token Authentication

**Category:** api
**Status:** Active
**Requires:** —
**Conflicts with:** —

## Decision
Authentication uses JWT Bearer tokens sent via the Authorization header. Access tokens are short-lived; refresh tokens are long-lived and rotated on use.

## Rationale
- JWT Bearer tokens are stateless and allow the API to validate requests without a session store
- Short-lived access tokens limit the blast radius of token theft; refresh token rotation detects reuse
- Alternatives considered: session cookies (rejected — not suitable for SPA + API architecture), opaque tokens with introspection (rejected — adds latency per request)

## Constraints (non-negotiable for AI)
- Tokens sent in `Authorization: Bearer <token>` header on every authenticated request
- Access tokens contain claims: user ID (`sub`), role(s), issued-at, expiration
- Access token lifetime: 15-60 minutes (configurable)
- Refresh tokens are long-lived, stored securely (httpOnly cookie or secure storage), and rotated on every use
- Never store JWTs in localStorage — use httpOnly cookies or in-memory storage on the frontend
- Token validation must check signature, expiration, issuer, and audience
- Protect endpoints with `[Authorize]` attribute; role-based access with `[Authorize(Roles = "...")]`
