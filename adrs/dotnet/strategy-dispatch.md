---
category: dotnet
stack: dotnet
status: Active
requires:
  - adrs/dotnet/cqrs-handlers.md
conflicts_with: []
last_reviewed: 2026-07-31
---

# Strategy Dispatch for Content-Type Matrices

## Decision
When a handler's behavior varies across a matrix of kinds — content type × session type on the outbound path, content type on the inbound path — each cell is its own strategy class implementing a closed generic interface (e.g., `IOutboundMessageStrategy<TContent>` keyed by session kind, `IInboundMessageStrategy<TContent>`). Strategies are registered in DI (keyed registration or an explicit registry built at startup) and resolved by the handler from the message's discriminators. The handler stays fixed; adding a kind means adding a class and a registration. An explicit fallback strategy handles unknown kinds gracefully.

## Rationale
- The alternative is a `switch` in the handler that grows a case per kind. At real scale (a production system reached 18 outbound and 12 inbound kinds) that switch becomes the most-edited, most-conflicted, least-testable method in the module. One-class-per-cell makes each behavior independently testable and additive to extend — the open/closed principle applied where it actually pays.
- The fallback strategy is a production lesson, not a nicety: external providers add content types without notice. An unknown inbound kind must degrade to a well-defined behavior (persist as unknown, notify, continue) — never throw and poison the queue.
- Registry-at-startup (or keyed DI) keeps resolution explicit and fail-fast: a missing registration surfaces at boot or first resolve with a clear error, and there is no per-message reflection scanning.
- Alternatives considered: switch expressions in the handler (acceptable below ~5 stable kinds — migrate when the matrix starts growing), polymorphic serialization dispatching on CLR type alone (rejected — couples wire format to dispatch and hides the matrix), MediatR-style notification handlers per kind (rejected — this family avoids MediatR, and broadcast semantics are wrong for pick-exactly-one dispatch).

## Constraints (non-negotiable for AI)
- One strategy class per matrix cell. NEVER a strategy that switches internally on the discriminator it was selected by.
- The dispatch matrix (which kinds exist, which strategy owns each cell) MUST be readable in ONE place — the registration module/registry, not scattered discovery.
- A fallback strategy for unknown kinds is mandatory on paths fed by external input. It logs the unknown discriminator, degrades gracefully, and NEVER throws for being unknown.
- Strategies are stateless or scoped services; they receive the message and context as arguments. NEVER cache per-message state on the strategy instance.
- Strategies MUST NOT call other strategies. Shared behavior is extracted to collaborators both strategies use.
- Handlers resolve strategies through the registry/keyed DI only. NEVER `switch` on kind in the handler next to the strategy machinery — one dispatch mechanism per matrix.

## Examples

**Violation — the growing switch:**
```csharp
public async Task<Result> Handle(SendMessageCommand cmd, CancellationToken ct)
{
    switch (cmd.Content) // 18 cases and counting, edited by every feature
    {
        case TextContent t when cmd.Session is UserSession: /* ... */ break;
        case ImageContent i when cmd.Session is UserSession: /* ... */ break;
        // ...
    }
}
```

**Compliant:**
```csharp
public async Task<Result> Handle(SendMessageCommand cmd, CancellationToken ct)
{
    var strategy = _strategies.Resolve(cmd.Content.Kind, cmd.Session.Kind); // registry, fail-fast
    return await strategy.ExecuteAsync(cmd, ct);
}

internal sealed class TextUserStrategy : IOutboundMessageStrategy<TextContent>
{
    public Task<Result> ExecuteAsync(SendMessageCommand cmd, CancellationToken ct) { /* one cell */ }
}
```
