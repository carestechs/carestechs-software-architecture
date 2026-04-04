# Environment-Based Configuration with Connection URLs

**Category:** deployment
**Status:** Active
**Requires:** —
**Conflicts with:** —

## Decision
All runtime configuration is provided through environment variables. External services (databases, message brokers, caches, search APIs) are referenced exclusively by connection URLs or API keys passed as environment variables. The application never bundles or assumes co-located infrastructure — it connects to whatever the environment provides. A typed settings class validates and centralizes all configuration at startup.

## Rationale
- Connection URLs decouple the application from its infrastructure topology. The same image runs against a local PostgreSQL in development and a managed cloud database in production — only the URL changes. This is a core tenet of twelve-factor app methodology.
- Alternatives considered: config files baked into the image (rejected — requires rebuilding for each environment), runtime config fetched from a service like Consul or Vault (viable for secrets rotation but overkill as the primary config mechanism — can layer on top), YAML/TOML config files mounted as volumes (rejected — harder to manage in container orchestrators compared to env vars).
- A typed settings class (e.g., Pydantic `BaseSettings` in Python, `IConfiguration` binding in .NET) catches missing or malformed configuration at startup rather than at first use, failing fast with a clear error.
- `.env.example` and `.env.production.example` files serve as self-documenting configuration references without containing real secrets.

## Constraints (non-negotiable for AI)
- All runtime configuration MUST be read from environment variables. NEVER hardcode connection strings, API keys, or environment-specific values in source code.
- External service connections (database, Redis, LLM providers, search APIs) MUST be configured via URL-style environment variables (e.g., `DATABASE_URL`, `REDIS_URL`).
- The project MUST include a typed settings/configuration class that loads and validates all environment variables at application startup (e.g., Pydantic `BaseSettings` in Python, strongly-typed `IConfiguration` sections in .NET).
- The repository MUST contain `.env.example` with all required variables documented with placeholder values. NEVER commit a real `.env` file with actual secrets.
- A `.env.production.example` SHOULD exist showing production-specific variable expectations (e.g., different default model, stricter CORS origins).
- API keys and secrets MUST NOT have default values in the settings class. The application MUST fail to start if required secrets are missing.
- The settings/configuration class MUST be a singleton loaded once at startup. NEVER re-read environment variables scattered across the codebase.
