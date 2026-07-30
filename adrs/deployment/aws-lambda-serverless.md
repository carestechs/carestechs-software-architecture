---
category: deployment
stack: dotnet
status: Active
requires: []
conflicts_with:
  - adrs/deployment/docker-multi-stage-builds.md
  - adrs/deployment/container-per-process.md
  - adrs/deployment/local-dev-compose.md
last_reviewed: 2026-07-29
---

# AWS Lambda Serverless Deployment

## Decision

Each module's API is deployed as an AWS Lambda function behind API Gateway using `Amazon.Lambda.AspNetCoreServer.Hosting`. The full ASP.NET Core application runs inside Lambda with API Gateway proxy integration. In development, the application runs as a standard Kestrel server. The Lambda hosting is conditionally added only in production via environment detection.

## Rationale

- Lambda eliminates server management, has no idle cost (compute is provisioned per invocation), and scales automatically under load. For APIs with variable traffic patterns, this reduces costs compared to always-on EC2 or ECS instances.
- Alternatives considered: ECS Fargate (viable for consistent-traffic APIs but requires capacity planning and costs more at low traffic), EC2 with ASP.NET (rejected — operational overhead of patching, scaling, load balancing), Azure Functions (rejected — AWS ecosystem is the target platform).
- `Amazon.Lambda.AspNetCoreServer.Hosting` allows the same `Program.cs` to run both locally as Kestrel and in Lambda. No code changes needed between environments — only `AddAWSLambdaHosting(LambdaEventSource.RestApi)` is conditionally registered.
- Each module deploys as its own Lambda function with its own API Gateway, enabling independent scaling and deployment pipelines.
- The declared conflicts with the Docker ADRs concern application packaging: APIs deploy as zip-based Lambda functions and are never containerized. Running local infrastructure in a container during development (e.g., a PostgreSQL container), or Docker images for AWS Batch workers (`aws-batch-workers.md`), does not contradict this.

## Constraints (non-negotiable for AI)

- Each module's API MUST be deployed as its own Lambda function. NEVER combine multiple modules into a single Lambda.
- `AddAWSLambdaHosting(LambdaEventSource.RestApi)` MUST be registered only in the production code path, NEVER in development or testing.
- Development MUST use standard Kestrel hosting (`dotnet run`) with no Lambda dependencies active.
- The Lambda handler MUST be the assembly name (e.g., `SiteManagement.Api`), configured as the `Handler` property in CloudFormation.
- Lambda functions MUST be deployed within a VPC private subnet with access to the database via security groups.
- Environment variables (`ASPNETCORE_ENVIRONMENT: Production`) MUST be set on the Lambda function to activate production configuration.
- Lambda memory and timeout MUST be explicitly configured in the infrastructure template. Default: 512 MB memory, 30 seconds timeout for API functions.
