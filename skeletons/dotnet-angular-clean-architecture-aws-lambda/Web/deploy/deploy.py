#!/usr/bin/env python3
"""Inject the API base URL, upload to S3, invalidate CloudFront (profile
convention). Requires AWS credentials — this path is NOT CI-proven (see the
skeleton README's proven-vs-linted table).

Usage: python deploy.py <bucket> <api-base-url> [distribution-id]
"""
import subprocess
import sys
from pathlib import Path

DIST = Path(__file__).resolve().parent.parent / "dist" / "client" / "browser"
PLACEHOLDER = "__API_BASE_URL__"

def main() -> int:
    if len(sys.argv) < 3:
        print(__doc__)
        return 2
    bucket, api_base_url = sys.argv[1], sys.argv[2]
    distribution = sys.argv[3] if len(sys.argv) > 3 else None

    replaced = 0
    for bundle in DIST.rglob("*.js"):
        text = bundle.read_text(encoding="utf-8")
        if PLACEHOLDER in text:
            bundle.write_text(text.replace(PLACEHOLDER, api_base_url), encoding="utf-8")
            replaced += 1
    print(f"injected API base URL into {replaced} bundle file(s)")

    subprocess.run(["aws", "s3", "sync", str(DIST), f"s3://{bucket}", "--delete"], check=True)
    if distribution:
        subprocess.run(["aws", "cloudfront", "create-invalidation",
                        "--distribution-id", distribution, "--paths", "/*"], check=True)
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
