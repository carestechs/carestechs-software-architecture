---
category: api
stack: any
status: Active
requires: []
conflicts_with:
  - adrs/api/jwt-bearer-auth.md
last_reviewed: 2026-07-31
---

# Managed Authentication with Cognito

## Decision
Authentication is delegated to an AWS Cognito user pool. The platform runs ONE user pool with one app client per consuming application (e.g., admin dashboard, agent dashboard); the token's client/audience claim tells the backend which application the caller came through. API Gateway JWT authorizers validate tokens before Lambda code runs. Cognito Lambda triggers customize the flow: pre-token-generation enriches the JWT with application claims (tenant identifiers, app discriminator); post-authentication syncs user lifecycle into the application database (first-login activation, last-login stamps). The application database remains the master for user profile and status; Cognito mirrors credentials and issues tokens.

## Rationale
- Password storage, token signing, key rotation, MFA, and OAuth flows are undifferentiated security burden; a managed issuer removes them. The sibling `jwt-bearer-auth` ADR is the right choice when the platform must own its issuer — the two are mutually exclusive per system.
- One pool with per-app clients (instead of pool-per-app or role-per-pool) keeps one user identity across applications while letting the backend authorize per application via the audience claim.
- Pre-token enrichment puts tenant scope into the token once, at issuance — every downstream authorizer and handler reads validated claims instead of resolving tenancy per request.
- Application-database-as-master avoids the classic drift trap: business logic reads user status from its own store; Cognito is a credential mirror. Production lessons encoded here: keep `username` and `preferred_username` aligned when they both exist, and when migrating users between pools, write custom attributes to the TARGET pool — migrated users with attributes only on the source pool authenticate but arrive claim-less.

## Constraints (non-negotiable for AI)
- Tokens are validated ONLY against the pool's JWKS via the gateway authorizer (or equivalent middleware in local dev). NEVER validate by decoding without signature checks, and NEVER mint parallel homegrown tokens alongside Cognito's.
- Authorization decisions use validated claims (audience/app, tenant identifiers, subject). NEVER trust tenant or identity fields from the request body/query/headers.
- Trigger handlers MUST target the current (V2 where available) event shapes and be registered per pool via infrastructure code — never hand-wired in the console.
- Custom claims are minimal: tenant identifiers and app discriminator. NEVER embed per-resource permissions (see role-based authorization) or mutable profile data.
- User lifecycle transitions (invited → active, disable/enable) are written to the application database by handlers/triggers — application code MUST NOT read lifecycle from Cognito attributes at request time.
- User migrations between pools MUST carry custom attributes to the target pool and keep `username`/`preferred_username` consistent.
