---
category: deployment
stack: any
status: Active
requires: []
conflicts_with: []
last_reviewed: 2026-10-11
---

# Private-Network Exposure

## Decision
A personal or team service that never needs a public audience is reachable only on a private overlay network (a tailnet or a VPN): the server binds the overlay address, clients join the network with their devices, TLS comes from the overlay's certificate facility, and application authentication stays in place as the second lock. No public DNS name, no open port, no reverse proxy on the internet.

## Rationale
- Exposing a single-user tool to the internet buys an attack surface and an operations burden (TLS renewal, rate limiting, bot traffic) for a product whose users fit on one hand. An overlay network gives device identity, encryption and reachability across NATs with no infrastructure of one's own.
- Application tokens stay mandatory: the network says which device is talking, the application says what it may do. Removing a device from the network and revoking its token are two independent locks.
- Alternatives considered: a public endpoint with TLS and strong authentication (the right choice when users or integrations cannot join a private network; this ADR is swapped for it and nothing else changes), a reverse tunnel service (rejected; a third party in the path), SSH tunnels (rejected; per-client ceremony).

## Constraints (non-negotiable for AI)
- The server binds the overlay interface, or all interfaces behind a host firewall that admits only the overlay. NEVER `0.0.0.0` on a host with a public address without that firewall rule.
- Application authentication is NEVER weakened because the network is private: tokens, scopes and revocation apply unchanged.
- TLS is on, from the overlay's certificate facility or a private CA, and clients verify it. NEVER plain HTTP outside loopback.
- Adding a user means adding a device to the network AND issuing an application token; removing one means both. The operator's checklist states this.
- The service keeps working on a public endpoint later with no code change: the binding address and the TLS source are configuration.
