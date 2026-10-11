---
category: deployment
stack: any
status: Active
requires: []
conflicts_with: []
last_reviewed: 2026-10-11
---

# Outbound-Only Agent

## Decision
An agent that runs on a machine the operator does not expose (a developer workstation, a build box) initiates every connection to its hub and accepts none from the network: it POSTs events and long-polls for commands over HTTPS, authenticated by a token from its environment. Its only listener, if any, is bound to the loopback interface for same-machine producers.

## Rationale
- Workstations sit behind NAT and personal firewalls. An inbound port means router configuration, dynamic DNS and an attack surface on a machine that holds the operator's credentials; dialling out needs nothing and works from any network.
- One direction keeps the trust model simple: the hub never reaches into a machine, it answers what the agent asks. Commands wait in the hub's queue until the agent asks for them, which gives at-least-once delivery for free: the agent acknowledges what it executed and the hub re-offers what was not acknowledged.
- Alternatives considered: inbound webhooks to the agent (rejected; the reasons above), a reverse tunnel service (rejected; a third party in the path), a VPN or overlay network to the hub (compatible; the agent still dials out through it), WebSocket from the agent (acceptable; long-poll over plain HTTP needs no extra client code in a standard-library program).

## Constraints (non-negotiable for AI)
- The agent MUST NOT bind any network interface other than loopback. A local hook for same-machine producers binds `127.0.0.1` and refuses other hosts.
- Every hub interaction is an outbound HTTPS request with the token in the `Authorization` header. The token comes from the environment or an operator-owned secret store, NEVER from a file in the repository or from the command line.
- Commands are pulled, never pushed: the agent asks (`GET /agents/{id}/commands?wait=25`), executes, then acknowledges by command id. The hub re-offers unacknowledged commands after a visible timeout and marks the repeat delivery as such.
- The command vocabulary is fixed and enumerated in the agent. It MUST reject any command outside it and MUST NOT execute free-form text.
- Reconnection uses bounded exponential backoff with jitter and a ceiling. The agent keeps working locally (buffering events to disk) while the hub is unreachable and never loses an event because the network was down.
- Certificate verification stays on. A private CA or overlay-network certificate is configured, NEVER disabled.
