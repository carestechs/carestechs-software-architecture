# Async All the Way

**Category:** dotnet
**Status:** Active
**Requires:** —
**Conflicts with:** —

## Decision
All I/O-bound operations use async/await consistently from controller actions down through services to DbContext calls. Controller actions return `Task<IActionResult>`. Service methods return `Task<T>`. All EF Core queries use their async variants. Service interface methods use the `Async` suffix.

## Rationale
- Synchronous I/O in ASP.NET Core blocks thread pool threads, reducing throughput under load. Async/await releases threads back to the pool during I/O waits, allowing the server to handle more concurrent requests with fewer threads.
- Alternatives considered: synchronous-only (rejected — wastes thread pool threads, degrades scalability), mixing sync and async (rejected — leads to deadlocks and thread pool starvation, especially with `.Result` or `.Wait()` calls).
- The "async all the way" principle prevents the most common async pitfall: blocking on async code. If every layer is async, there is no temptation to call `.Result` or `.Wait()`.
- EF Core's async methods (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`) are purpose-built for this pattern.

## Constraints (non-negotiable for AI)
- Controller actions MUST return `Task<IActionResult>` or `Task<ActionResult<T>>`.
- Service interface methods MUST return `Task<T>` or `Task` and use the `Async` suffix (e.g., `GetProductByIdAsync`).
- All EF Core query and save operations MUST use their async variants: `ToListAsync()`, `FirstOrDefaultAsync()`, `SingleOrDefaultAsync()`, `SaveChangesAsync()`, `AnyAsync()`, `CountAsync()`, etc.
- NEVER call `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` on a Task. These block the calling thread and can cause deadlocks.
- NEVER use `async void` except for event handlers (which do not exist in this architecture).
- Always pass `CancellationToken` through the call chain when available.
