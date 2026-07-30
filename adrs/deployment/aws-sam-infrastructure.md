# AWS SAM/CloudFormation Infrastructure as Code

**Category:** deployment
**Status:** Active
**Requires:** `adrs/deployment/aws-lambda-serverless.md`
**Conflicts with:** —

## Decision

All AWS infrastructure is defined as code using AWS SAM (Serverless Application Model) templates extending CloudFormation. Each module has its own `<Module>.Infra/resources.yml` template defining its Lambda functions, API Gateways, IAM roles, SQS queues, and SSM parameters. A shared `Common.Infra/resources.yml` defines cross-cutting infrastructure (VPC, database instances, Route53 hosted zones). Templates are deployed via `sam deploy` with stack-per-module isolation.

## Rationale

- Infrastructure as code ensures reproducibility, version control, and peer review of infrastructure changes. The state of production infrastructure is always derivable from the repository.
- Alternatives considered: Terraform (viable and more provider-agnostic, but SAM has first-class Lambda support with local testing via `sam local invoke`), AWS CDK (viable but adds a build step and programming language dependency to infrastructure definitions), manual AWS Console (rejected — not reproducible, no audit trail, configuration drift).
- Stack-per-module isolation means a failed deployment of one module does not affect other modules. Each stack can be rolled back independently.
- SSM Parameters store cross-stack references (queue URLs, API endpoints, hosted zone IDs), enabling loose coupling between stacks without hardcoded ARNs.

## Constraints (non-negotiable for AI)

- Every module with AWS resources MUST have a `<Module>.Infra/resources.yml` SAM template.
- Templates MUST use `AWS::Serverless::Function` for Lambda functions, not raw `AWS::Lambda::Function`, to leverage SAM's simplified event source mapping.
- IAM roles MUST follow least privilege — each Lambda gets its own role with only the permissions it needs (SSM, Secrets Manager, SQS, S3, etc.).
- Cross-stack references MUST use SSM Parameters, not CloudFormation exports. SSM parameters are more flexible and don't create deletion-order dependencies.
- Deploy scripts MUST live in `<Module>.Infra/deploy/` with `build.py` (prepare artifacts) and `deploy.py` (execute `sam deploy`).
- Stack names MUST follow the convention `stack-services-<module>` (e.g., `stack-services-sitemanagement`).
- Secrets (database credentials, API keys) MUST be stored in AWS Secrets Manager, NEVER in SSM Parameters or environment variables.
- Lambda code MUST be referenced via S3 (`CodeUri` with Bucket/Key), not inline or local paths, for production deployments.
