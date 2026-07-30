# AWS Batch Workers for Compute-Heavy Jobs

**Category:** deployment
**Status:** Active
**Requires:** `adrs/deployment/aws-sam-infrastructure.md`, `adrs/deployment/queue-based-decoupling.md`
**Conflicts with:** —

## Decision

Long-running or compute-heavy workloads (image processing, data transformation, ML inference) run as AWS Batch jobs on Fargate, triggered by enqueueing a job payload from a Lambda or reactor. The same .NET project supports both a development mode (BackgroundService polling a local HTTP queue) and a production mode (single-shot execution reading `JOB_PAYLOAD` from environment).

## Rationale

- Lambda has a 15-minute timeout and limited memory/CPU — batch image processing, file conversion, and ML workloads routinely exceed these constraints
- AWS Batch on Fargate provides auto-scaling compute without managing EC2 instances. Note: Fargate is CPU-only — GPU job definitions require an EC2 compute environment, which is outside this ADR's scope
- Using a `JOB_PAYLOAD` environment variable for the job input keeps the production path simple: deserialize, process, exit — no queue polling, no visibility timeout management
- The dual-mode `Program.cs` (BackgroundService for dev, single-shot for prod) lets developers test the full pipeline locally with the HTTP queue server, while production uses the managed Batch service

## Constraints (non-negotiable for AI)

- Worker projects MUST support two execution modes in `Program.cs`: (1) Development — register a `BackgroundService` that polls the local HTTP queue via `HttpQueueProvider` and processes jobs in a loop; (2) Production — read the `JOB_PAYLOAD` environment variable, deserialize the job record, execute the orchestrator/handler, then exit (no host, no loop)
- The `IQueueProvider` abstraction MUST have a `BatchJobQueueProvider` implementation that calls `AmazonBatchClient.SubmitJobAsync`, passing the serialized job as the `JOB_PAYLOAD` environment variable override on the container
- AWS Batch infrastructure (ComputeEnvironment, JobQueue, JobDefinition) MUST be defined in the module's `resources.yml` CloudFormation template using Fargate platform — NEVER provision or manage EC2 instances directly
- Job definitions MUST specify resource requirements (vCPU, memory) explicitly — NEVER rely on defaults
- The worker Docker image MUST use multi-stage builds: SDK stage for `dotnet publish`, runtime stage for the final image
- Reactors or command handlers trigger batch jobs by calling `IQueueProvider.EnqueueAsync` with a typed job record — NEVER call `AmazonBatchClient` directly from application code
