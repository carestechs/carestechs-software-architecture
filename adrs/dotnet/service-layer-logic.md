---
category: dotnet
stack: dotnet
family: service-layer-logic
status: Active
requires: []
conflicts_with:
  - adrs/dotnet/cqrs-handlers.md
  - adrs/dotnet/rich-domain-entities.md
last_reviewed: 2026-07-29
---

# Service Layer Owns All Business Logic

## Decision
All business logic lives in service classes. Controllers are thin: they validate input, call a service method, and return the result. Services are registered as scoped and injected via interfaces. No business logic is permitted in controllers or repository classes.

## Rationale
- Centralizing business logic in the service layer provides a single, testable location for domain rules. Controllers handle HTTP concerns; services handle business concerns. This separation makes unit testing straightforward — services can be tested without HTTP infrastructure.
- Alternatives considered: rich domain model with logic in entities (can complement services for entity-level invariants, but orchestration and cross-entity logic still belongs in services), MediatR handlers (adds indirection without clear benefit at current scale), CQRS with dedicated handlers (a valid alternative used by the Clean Architecture stack — see the conflicting `cqrs-handlers.md`; never mix the two patterns in one solution), logic in controllers (rejected — untestable without integration test infrastructure, mixes HTTP and domain concerns).
- Scoped lifetime aligns with the per-request DbContext lifetime, ensuring services and their DbContext share the same scope.

## Constraints (non-negotiable for AI)
- Controllers MUST only: parse/validate the request, call one or more service methods, and return an HTTP response.
- All business rules, validations beyond input format, orchestration, and data transformation MUST live in service classes.
- Services MUST be registered as scoped in DI.
- Services MUST be injected via their interface (e.g., `ICatalogService`), never as concrete classes.
- Service interfaces and implementations MUST live in the module's `Services/` folder.
- NEVER place business logic in repository classes — repositories (if used) are thin data-access wrappers only. In most cases, the DbContext itself is the repository.

## Examples

**Violation — business logic in the controller:**
```csharp
[HttpPost]
public async Task<IActionResult> Create(CreateProductRequest request)
{
    if (await _db.Products.AnyAsync(p => p.Sku == request.Sku))
        return Conflict();
    _db.Products.Add(new Product { Sku = request.Sku, Name = request.Name });
    await _db.SaveChangesAsync();
    return Ok();
}
```

**Compliant:**
```csharp
[HttpPost]
public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request, CancellationToken ct)
{
    var dto = await _catalogService.CreateProductAsync(request, ct); // rules live in the service
    return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
}
```
