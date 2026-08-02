---
category: dotnet
stack: dotnet
family: async-all-the-way
status: Active
requires: []
conflicts_with: []
last_reviewed: 2026-07-29
---

# Async All the Way

## Decision
All I/O-bound operations use async/await consistently from the HTTP entry point (controller actions or Minimal API endpoint delegates) down through services or command/query handlers to DbContext calls. Controller actions return `Task<IActionResult>`; Minimal API delegates return `Task<IResult>` (or a `Task` of a typed result). Service methods and handlers return `Task<T>`. All EF Core queries use their async variants. Service interface methods use the `Async` suffix.

## Rationale
- Synchronous I/O in ASP.NET Core blocks thread pool threads, reducing throughput under load. Async/await releases threads back to the pool during I/O waits, allowing the server to handle more concurrent requests with fewer threads.
- Alternatives considered: synchronous-only (rejected — wastes thread pool threads, degrades scalability), mixing sync and async (rejected — leads to deadlocks and thread pool starvation, especially with `.Result` or `.Wait()` calls).
- The "async all the way" principle prevents the most common async pitfall: blocking on async code. If every layer is async, there is no temptation to call `.Result` or `.Wait()`.
- EF Core's async methods (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`) are purpose-built for this pattern.

## Constraints (non-negotiable for AI)
- Controller actions MUST return `Task<IActionResult>` or `Task<ActionResult<T>>`; Minimal API endpoint delegates MUST be `async` and return `Task<IResult>` or a `Task` of a typed result.
- Service interface methods and command/query handler methods MUST return `Task<T>` or `Task`; service interface methods use the `Async` suffix (e.g., `GetProductByIdAsync`).
- All EF Core query and save operations MUST use their async variants: `ToListAsync()`, `FirstOrDefaultAsync()`, `SingleOrDefaultAsync()`, `SaveChangesAsync()`, `AnyAsync()`, `CountAsync()`, etc.
- NEVER call `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` on a Task. These block the calling thread and can cause deadlocks.
- NEVER use `async void`.
- Accept and forward `CancellationToken` parameters through the call chain; bind them from the HTTP request in controllers and endpoint delegates.

## Examples

**Violation — blocking on async (deadlock risk):**
```csharp
public IActionResult GetProduct(Guid id)
{
    var product = _catalogService.GetProductByIdAsync(id).Result;
    return Ok(product);
}
```

**Compliant:**
```csharp
public async Task<ActionResult<ProductDto>> GetProduct(Guid id, CancellationToken ct)
{
    var product = await _catalogService.GetProductByIdAsync(id, ct);
    return Ok(product);
}
```

**Violation — synchronous EF Core query on an async path:**
```csharp
var items = _db.Products.Where(p => p.IsActive).ToList();
```

**Compliant:**
```csharp
var items = await _db.Products.Where(p => p.IsActive).ToListAsync(ct);
```
