# Phase 3: real-AWS smoke — one-time setup (deferred)

The workflow `.github/workflows/aws-smoke.yml` ships **dormant**: manual dispatch only,
and its preflight fails with instructions until the setup below is done. Nothing runs
and nothing costs until then. This document is the activation runbook.

## What it does once active

Weekly (or on dispatch): `sam deploy` both AWS skeletons into the sandbox account,
smoke the real API Gateway + Lambda health paths, tear everything down (`if: always()`),
with a janitor sweep for leftovers. v1 proves **deployability, IAM, API Gateway wiring,
and the dotnet10 Lambda runtime** — the rows the skeletons' tables mark "not proven".
Database-backed deep smoke (ephemeral RDS) is a documented later increment.

## One-time setup (~30-60 min, account owner only)

1. **Create a dedicated sandbox AWS account** — ideally a separate account in your
   AWS Organization. Account-level isolation is the real security boundary here.
2. **Budget alarm first**: AWS Budgets, $10/month, email alert. Steady-state cost is
   well under $5/month; the alarm exists to catch teardown failures, not spend.
3. **GitHub OIDC provider** in that account:
   provider URL `https://token.actions.githubusercontent.com`, audience `sts.amazonaws.com`.
4. **IAM role** (e.g. `github-skeleton-smoke`) with this trust policy:

   ```json
   {
     "Version": "2012-10-17",
     "Statement": [{
       "Effect": "Allow",
       "Principal": { "Federated": "arn:aws:iam::<ACCOUNT_ID>:oidc-provider/token.actions.githubusercontent.com" },
       "Action": "sts:AssumeRoleWithWebIdentity",
       "Condition": {
         "StringEquals": { "token.actions.githubusercontent.com:aud": "sts.amazonaws.com" },
         "StringLike": { "token.actions.githubusercontent.com:sub": "repo:carestechs/carestechs-software-architecture:ref:refs/heads/main" }
       }
     }]
   }
   ```

   Permissions: in a dedicated sandbox account, `PowerUserAccess` plus scoped IAM
   actions (`iam:CreateRole`, `iam:AttachRolePolicy`, `iam:PutRolePolicy`,
   `iam:PassRole`, `iam:DeleteRole`, `iam:DetachRolePolicy`, `iam:DeleteRolePolicy`,
   `iam:GetRole`, `iam:TagRole`) is acceptable — SAM creates Lambda execution roles,
   and the account boundary is the guardrail. Tighten later if the account ever
   hosts anything else.
5. **Repo variables** (Settings -> Secrets and variables -> Actions -> Variables):
   - `AWS_SMOKE_ROLE_ARN` = the role ARN (required — the preflight gate)
   - `AWS_SMOKE_REGION` = e.g. `us-east-1` (optional; defaults to us-east-1)
6. **Activate the schedule**: uncomment the `schedule:` block in
   `.github/workflows/aws-smoke.yml`. Until then, runs are manual-dispatch only.

## Cost model

Lambda/SQS/API GW/DDB at smoke volume: effectively $0 (free tiers measured in
millions). The only real line is a database if the deep-smoke increment is added
later (~$0.05/run ephemeral; ~$13/month if leaked — hence three independent guards:
the always-teardown step, the janitor job, and the budget alarm).

## First-run expectations

The first live runs will surface findings the same way the skeletons' CI did — treat
red runs as signal, not noise. Dispatch with `mode: janitor` any time to sweep leftovers.
