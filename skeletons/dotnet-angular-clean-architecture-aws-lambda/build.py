#!/usr/bin/env python3
"""Root build orchestrator (profile convention): sam build per module stack +
the Web bundle. Everything here runs without AWS credentials."""
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
STACKS = ["Catalog.Infra/template.yaml", "Orders.Infra/template.yaml"]

if __name__ == "__main__":
    for stack in STACKS:
        subprocess.run(["sam", "build", "-t", stack], cwd=ROOT, check=True,
                       shell=sys.platform == "win32")
    subprocess.run([sys.executable, "Web/deploy/build.py"], cwd=ROOT, check=True)
    print("all module stacks and the Web bundle built")
