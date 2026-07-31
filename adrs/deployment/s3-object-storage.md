---
category: deployment
stack: any
status: Active
requires: []
conflicts_with: []
last_reviewed: 2026-07-31
---

# Object Storage with Tenant-Scoped Keys and Presigned Transfer

## Decision
Binary content (media, attachments, template assets, export files) lives in object storage (S3), never in the database. Keys are tenant-scoped: `{org}/{workspace}/{domain}/...`, and each key prefix has exactly one owning module. Clients upload and download through short-lived presigned URLs issued by the owning module — bytes never proxy through application compute. Buckets are per environment; because bucket names are globally namespaced, the bucket name is the ONE place environment may appear in runtime-resolved configuration, behind a single helper. Object metadata (ownership, content type, size, lifecycle state) is mastered in the database; the object store holds bytes only.

## Rationale
- Database blobs are rejected for the usual reasons (backup bloat, connection amplification, no CDN path); proxying media through Lambda/API compute is rejected for payload limits and per-GB compute cost — presigned URLs move bytes directly between client and store while authorization stays server-side at URL issuance.
- Tenant identifiers in the key prefix make tenant scoping structural: offboarding is a prefix delete, cross-tenant access requires constructing a key the issuing module would never sign.
- S3's global bucket namespace forces environment into bucket names — an explicit, contained exception to the no-environment-suffixes-in-runtime-code rule. One resolver helper owns it; nothing else reads the environment for storage addressing.
- Metadata in the database keeps queries (list a conversation's attachments, find orphans) in SQL/DDB where they belong; `ListObjects` on unbounded prefixes is an outage pattern, not an API.

## Constraints (non-negotiable for AI)
- Every key MUST begin with tenant identifiers (`{org}/{workspace}/`). A key that omits tenant scope is a defect regardless of bucket policy.
- Presigned URLs are short-lived (minutes) and constrained: uploads pin content type and size (presigned POST policies); downloads sign a single key. NEVER issue a presigned URL for a prefix.
- Only the owning module issues presigned URLs for its prefixes. Other modules request the owning module's contract; they do not sign foreign keys.
- Account-level public-access block stays ON. NEVER create a public bucket or object ACL; public delivery goes through a CDN with origin access control if ever needed.
- NEVER call `ListObjects` on a request path. Object inventories come from the metadata store; listing is for reconciliation jobs.
- Uploads are quarantined until confirmed: the owning module records the object in its metadata store when the client confirms (or via bucket notification), and unconfirmed objects are lifecycle-expired.
- Tenant offboarding MUST include prefix removal, and lifecycle rules (transition/expiry) are declared in infrastructure code per prefix class, not set by hand.
