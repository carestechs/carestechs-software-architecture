---
category: api
stack: any
status: Active
requires: []
conflicts_with: []
last_reviewed: 2026-07-29
---

# JWT Bearer Token Authentication

## Decision
Authentication uses JWT Bearer tokens sent via the Authorization header. Access tokens are short-lived; refresh tokens are long-lived and rotated on use.

## Rationale
- JWT Bearer tokens are stateless and allow the API to validate requests without a session store
- Short-lived access tokens limit the blast radius of token theft; refresh token rotation detects reuse
- Alternatives considered: session cookies (rejected — not suitable for SPA + API architecture), opaque tokens with introspection (rejected — adds latency per request)

## Constraints (non-negotiable for AI)
- Tokens sent in `Authorization: Bearer <token>` header on every authenticated request
- Access tokens contain claims: user ID (`sub`), role(s), issued-at, expiration
- Access token lifetime: 15 minutes by default (configurable); MUST NOT exceed 60 minutes
- Refresh tokens are long-lived but bounded by an absolute maximum lifetime (e.g., 30 days), stored in an httpOnly cookie (web) or OS-secure storage (native), and rotated on every use; on detected reuse of an already-rotated token, revoke the entire token family and force re-authentication
- Never store JWTs in localStorage or sessionStorage — use httpOnly cookies or in-memory storage on the frontend
- Token validation must check signature, expiration, issuer, and audience against an explicit algorithm allowlist (e.g., RS256/ES256 only) — NEVER accept the `none` algorithm or trust the token header's `alg`; tolerate at most ~60 seconds of clock skew
- Cookie-delivered refresh tokens MUST set `Secure`, `HttpOnly`, and `SameSite=Strict` (or `Lax`), and the refresh endpoint MUST be CSRF-protected
- Protect endpoints with `[Authorize]` attribute (or FastAPI auth dependencies); role-based access with `[Authorize(Roles = "...")]` or an equivalent role guard
