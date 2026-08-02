---
category: deployment
stack: any
status: Active
requires: []
conflicts_with: []
last_reviewed: 2026-08-02
---

# Staging Environment Cost Downgrades

## Decision

Staging runs on real AWS, deployed from the same infrastructure templates and pipeline as production. Cost is controlled by downgrading **operational qualities** — redundancy, throughput, managed-ness, retention — through template parameters, never by changing service types, message wiring, or auth flows. Every applied downgrade is recorded with its accepted risk, and parameter defaults are the production values, so a forgotten override fails safe (expensive), not unsafe (divergent).

## Rationale

- Local substitution (`local-aws-substitution.md`) covers functional iteration; staging exists to verify what only real AWS exercises: IAM policies, VPC routing, Cognito flows, API Gateway behavior, and the deployment automation itself.
- Behavioral parity and operational parity are different guarantees. Staging must answer "does it work the same way", not "does it survive the same load". Downgrades that preserve the former are sanctioned; anything that changes the code path invalidates staging as evidence.
- The largest fixed line items in an idle serverless staging environment are usually the NAT Gateway (hourly + per-GB) and the database instance — both downgrade well because neither changes how the application behaves at low traffic.
- Alternatives considered: no staging at all (rejected for AWS deployments — IaC, IAM, and auth changes need a rehearsal target); emulator-based staging (rejected — staging's entire purpose is the real control plane); a full production mirror (viable when budget allows; this ADR is for when it does not).

### Sanctioned downgrades

| Production | Staging downgrade | Accepted risk |
|------------|-------------------|---------------|
| NAT Gateway | NAT instance (e.g., a fck-nat AMI on `t4g.nano`) | Single point of failure; bandwidth ceiling; you patch it |
| RDS Multi-AZ | RDS Single-AZ on a smaller instance class | Minutes of downtime on instance failure |
| RDS (managed) | PostgreSQL on EC2 — only with the production major version pinned, scripted daily backups, and rebuild from automation alone | You own patching, backups, and failure recovery; parameter drift if not scripted |
| Provisioned/reserved Lambda concurrency | None | Cold starts visible — never read latency numbers off staging |
| CloudWatch log retention 30-90 days | 7 days | Shorter forensic window |
| DynamoDB provisioned capacity | On-demand | Usually cheaper at staging volume; no behavioral risk |
| Multi-AZ VPC footprint | Single-AZ subnets and endpoints | An AZ event takes staging down entirely |
| Always-on environment | Scheduled teardown and rebuild from the same templates | Not always available; rebuild automation must work — which is itself a rehearsal |

### Forbidden divergences

- Service types and wiring: if production is SQS → Lambda, staging is SQS → Lambda. Never substitute a different kind of service (no in-process bus where production has EventBridge, no self-issued tokens where production has Cognito).
- Authentication: real user pool, real IAM roles per function, same trigger wiring.
- Provenance: no console edits, no forked templates. A hand-tuned staging stack is not staging — it is a second product that happens to share a name.

## Constraints (non-negotiable for AI)

- Staging MUST be deployed from the same templates and stacks as production. Environment differences MUST be expressed as template parameters or conditions (e.g., an `EnvType` parameter selecting the NAT construct and instance sizes) — NEVER as forked templates or console edits.
- Downgrades MUST only reduce operational qualities (redundancy, throughput, retention, managed-ness). Service types, message wiring, and authentication MUST be identical to production.
- Template parameter defaults MUST be the production values; staging overrides them explicitly. A missing override MUST fail expensive, not divergent.
- Every applied downgrade MUST be recorded next to the environment's parameter file together with its accepted risk.
- A self-managed substitution (PostgreSQL on EC2) MUST pin the production engine major version, script its backups, and be rebuildable from automation alone. If any of the three is missing, use the managed service.
- Performance, latency, or failover conclusions MUST NOT be drawn from staging paths that traverse a downgraded resource — the divergence MUST be called out wherever such results are reported.
