#!/usr/bin/env python3
"""Root deploy orchestrator: sam deploy per stack, then the Web deploy.
Requires AWS credentials and sam deploy parameters — NOT CI-proven (the
phase-3 real-AWS smoke is the plan of record for proving deployment).

Usage: python deploy.py <env> <region> <database-url> <web-bucket> <api-base-url>
"""
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
STACKS = [
    ("skeleton-catalog", "Catalog.Infra/template.yaml"),
    ("skeleton-orders", "Orders.Infra/template.yaml"),
]

def main() -> int:
    if len(sys.argv) != 6:
        print(__doc__)
        return 2
    env, region, database_url, bucket, api_base_url = sys.argv[1:6]
    for name, template in STACKS:
        subprocess.run([
            "sam", "deploy", "-t", template,
            "--stack-name", f"{name}-{env}", "--region", region,
            "--capabilities", "CAPABILITY_IAM", "--resolve-s3",
            "--parameter-overrides", f"DatabaseUrl={database_url}",
        ], cwd=ROOT, check=True, shell=sys.platform == "win32")
    subprocess.run([sys.executable, "Web/deploy/deploy.py", bucket, api_base_url],
                   cwd=ROOT, check=True)
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
