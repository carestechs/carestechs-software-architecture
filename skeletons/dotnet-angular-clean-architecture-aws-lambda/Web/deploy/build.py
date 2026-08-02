#!/usr/bin/env python3
"""ng build with production file replacement (profile convention)."""
import subprocess
import sys
from pathlib import Path

WEB = Path(__file__).resolve().parent.parent

if __name__ == "__main__":
    subprocess.run(["npm", "ci"], cwd=WEB, check=True, shell=sys.platform == "win32")
    subprocess.run(
        ["npm", "run", "build"], cwd=WEB, check=True, shell=sys.platform == "win32")
    print("bundle at Web/dist/client/browser (contains the __API_BASE_URL__ placeholder)")
