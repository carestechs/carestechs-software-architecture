---
category: deployment
stack: dotnet
status: Active
requires:
  - adrs/deployment/aws-lambda-serverless.md
conflicts_with:
  - adrs/deployment/env-connection-urls.md
last_reviewed: 2026-07-29
---

# AWS Secrets Manager and SSM Parameters for Configuration

## Decision

Runtime configuration is managed through two AWS services: Secrets Manager for sensitive values (database credentials, API keys) and SSM Parameter Store for non-sensitive configuration (queue names, feature flags, API URLs). An `ISecretsProvider` and `IParametersProvider` abstraction allows swapping implementations: file-based providers (`.secrets`, `.parameters` JSON files) for development, AWS SDK providers for production. Providers are registered as singletons and read at service startup or on first use.

## Rationale

- Separating secrets from parameters aligns with AWS best practices: Secrets Manager provides automatic rotation, encryption, and audit logging for sensitive data; SSM Parameter Store provides free, simple key-value storage for configuration.
- Alternatives considered: environment variables for everything (rejected — Lambda environment variables are visible in the AWS Console and not suitable for secrets), AWS AppConfig (overkill for simple configuration), Vault (rejected — adds operational complexity with no clear benefit over managed AWS services at current scale).
- The abstraction layer (`ISecretsProvider`, `IParametersProvider`) ensures application code never directly depends on AWS SDKs. Development uses flat JSON files, making local development simple and fast with no AWS credentials required.
- `.secrets` and `.parameters` files are gitignored, with example templates committed to the repository.

## Constraints (non-negotiable for AI)

- Database credentials, API keys, and tokens MUST be stored in AWS Secrets Manager. NEVER store secrets in SSM Parameter Store or environment variables.
- Non-sensitive configuration (queue names, API URLs, feature flags) MUST be stored in SSM Parameter Store.
- Application code MUST access secrets via `ISecretsProvider` and configuration via `IParametersProvider`. NEVER use AWS SDK clients directly in application code.
- Development MUST use file-based providers: `SecretsFileProvider` reading from `.secrets` and `ParametersFileProvider` reading from `.parameters`.
- `.secrets` and `.parameters` files MUST be listed in `.gitignore`. NEVER commit real secrets to the repository.
- Providers MUST be registered as singletons in DI.
- The database connection secret MUST be stored as a JSON object with fields: `username`, `password`, `host`, `db`, `port`.
