---
category: api
stack: any
status: Active
requires:
  - adrs/api/jwt-bearer-auth.md | adrs/api/cognito-authentication.md
conflicts_with: []
last_reviewed: 2026-07-30
---

# Role-Based Authorization with Service-Layer Ownership Checks

## Decision
Access control has two layers. Coarse role gates are enforced declaratively at the endpoint layer — `[Authorize(Roles = "...")]` in .NET, a role-checking dependency in FastAPI — using roles carried as claims in the validated JWT. Fine-grained, per-resource decisions (does *this* user own *this* record?) are enforced in the service layer against current database state. Everything is deny-by-default: an endpoint without an explicit authorization declaration does not ship.

## Rationale
- Endpoint-layer role gates are declarative, auditable in one glance, and fail closed before any business code runs. But roles are coarse: "is a User" says nothing about *whose* order may be read — ownership can only be answered next to the data, in the service layer.
- Roles in JWT claims keep authorization stateless per request. Per-resource permissions in the token are rejected: they go stale the moment ownership changes and bloat the token; ownership MUST be checked against the database at request time.
- Deny-by-default inverts the failure mode: a forgotten annotation produces a 401 in testing, not an open endpoint in production.
- Alternatives considered: policy engines (OPA, Casbin — rejected at current scale; the two-layer pattern can migrate into one later), permissions-in-JWT (rejected — stale and unbounded), authorization checks scattered in controllers (rejected — invisible, untestable, inconsistently applied).

## Constraints (non-negotiable for AI)
- Every endpoint MUST declare its authorization explicitly: a role requirement, an authenticated-only marker, or an explicit anonymous opt-out. NEVER ship an endpoint whose access level is implicit.
- Role gates MUST be declared at the endpoint layer (`[Authorize(Roles = "...")]` / FastAPI dependency) — NEVER as ad-hoc `if` checks inside endpoint bodies.
- The current user's identity and roles MUST come from validated token claims (`sub`, roles) — NEVER from the request body, query string, or headers the client controls.
- Per-resource ownership checks MUST live in the service layer and compare the caller's ID from claims against the resource's owner in the database — an endpoint-level role gate alone is NOT sufficient for resource access.
- NEVER encode per-resource permissions in the JWT — roles only. Ownership is checked at request time against current data.
- Return 401 for unauthenticated requests and 403 for authenticated-but-forbidden ones. Where revealing a resource's existence is itself sensitive, returning 404 instead of 403 is permitted — pick one behavior per resource type and apply it consistently.
- Service methods that enforce ownership MUST receive the caller's identity as an explicit parameter — NEVER resolve the current user from ambient/static state inside the service layer.

## Examples

**Violation — trusting the client and skipping ownership:**
```csharp
[HttpGet("orders/{id}")] // no [Authorize]
public async Task<ActionResult<OrderDto>> Get(Guid id, [FromQuery] Guid userId)
    => Ok(await _orderService.GetOrderAsync(id)); // caller-supplied userId, no ownership check
```

**Compliant:**
```csharp
[Authorize(Roles = "User")]
[HttpGet("orders/{id}")]
public async Task<ActionResult<OrderDto>> Get(Guid id, CancellationToken ct)
{
    var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var dto = await _orderService.GetOrderAsync(id, callerId, ct); // service verifies ownership
    return dto is null ? NotFound() : Ok(dto);
}
```
