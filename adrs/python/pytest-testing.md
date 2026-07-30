---
category: python
stack: python
status: Active
requires:
  - adrs/python/fastapi-framework.md
conflicts_with:
  - adrs/dotnet/xunit-per-module-tests.md
last_reviewed: 2026-07-30
---

# pytest with Async Fixtures

## Decision
pytest with pytest-asyncio is the test framework for all Python code. Tests live under `tests/`, mirroring the module layout (`tests/modules/<module>/`) with end-to-end tests in `tests/integration/`. Shared fixtures — the async test client and the test database session — live in `tests/conftest.py`. API tests call the FastAPI app in-process through `httpx.AsyncClient` with `ASGITransport`; no live server is started. Tests follow Arrange-Act-Assert and mock only at system boundaries.

## Rationale
- pytest is the de-facto standard Python test runner: plain functions, fixture composition, and parametrization without class ceremony. pytest-asyncio makes `async def` tests first-class, matching the async-all-the-way stack.
- Alternatives considered: unittest (rejected — class boilerplate, weaker fixtures, no native async story), nose2 (rejected — effectively unmaintained).
- `httpx.AsyncClient` over `ASGITransport(app=app)` exercises the full FastAPI pipeline (routing, dependencies, validation, exception handlers) in-process — faster and more reliable than binding a socket, while still testing the real request path.
- Fixtures in `tests/conftest.py` mirror the profile structure already in this catalog; per-test database isolation (transaction rollback or a fresh schema) keeps tests order-independent.
- Mocking only at boundaries keeps tests refactor-safe: internal restructuring does not break tests that assert observable behavior. (The TypeScript stack's equivalent decision is `adrs/typescript/vitest-colocated.md`.)

## Constraints (non-negotiable for AI)
- pytest MUST be the test runner; tests are plain functions — NEVER write `unittest.TestCase` classes.
- Async tests MUST use pytest-asyncio with `asyncio_mode = "auto"` configured once in `pyproject.toml`; NEVER decorate individual tests with `@pytest.mark.asyncio`.
- API tests MUST call the app through `httpx.AsyncClient` with `ASGITransport(app=app)` — NEVER start a real Uvicorn server in tests.
- Shared fixtures (test client, database session/engine) MUST live in `tests/conftest.py`.
- Each test MUST run against isolated database state (per-test transaction rollback, or a fresh schema per run). Tests MUST NOT depend on execution order.
- Mock ONLY at system boundaries: other modules' contract interfaces, external APIs, LLM providers, Celery task dispatch. NEVER patch a module's own internals — test through the public surface.
- Test layout MUST mirror `src/app/modules/` under `tests/modules/`; cross-module end-to-end tests live in `tests/integration/`.
- CI MUST run `pytest` (plus the configured type checker) on every push and pull request.

## Examples

**Violation — real server and ordered, shared state:**
```python
def test_create_then_get():           # sync test, depends on the previous test's row
    requests.post("http://localhost:8000/products", json=payload)
```

**Compliant:**
```python
async def test_create_product(client: AsyncClient):  # client fixture from conftest.py
    response = await client.post("/products", json=payload)
    assert response.status_code == 201
```
