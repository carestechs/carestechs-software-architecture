#!/usr/bin/env python3
"""Generate profile tier tables from profiles/profiles.toml.

The manifest is the single source of truth for which ADRs each profile lists
in its Required/Recommended/Optional tiers and for the row texts. Prose
sections of the profile documents stay handwritten; only the three tier
tables are generated.

Usage:
    python scripts/generate_profiles.py           # rewrite the tables in place
    python scripts/generate_profiles.py --check   # exit 1 if tables drift from the manifest (CI)

Stdlib only (tomllib), same as validate_adrs.py.
"""
from __future__ import annotations

import re
import sys
import tomllib
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PROFILE_DIR = ROOT / "profiles"
MANIFEST = PROFILE_DIR / "profiles.toml"

MARKER = ("<!-- generated from profiles/profiles.toml — edit the manifest and run "
          "scripts/generate_profiles.py -->")

TIERS = (
    ("Required", "required", "depends_on", "Depends On"),
    ("Recommended", "recommended", "alternative", "Alternative"),
    ("Optional", "optional", "when_to_include", "When to Include"),
)


def fail(message: str) -> None:
    print(f"ERROR   {message}")
    sys.exit(1)


def cell(manifest: dict, profile: str, path: str, field: str) -> str:
    override = manifest["profile"][profile].get("override", {}).get(path, {})
    if field in override:
        return override[field]
    entry = manifest.get("adr", {}).get(path)
    if entry is None or field not in entry:
        fail(f"{profile}: '{path}' has no '{field}' text — add it under [adr.\"{path}\"] "
             f"or [profile.{profile}.override.\"{path}\"]")
    return entry[field]


def render_table(manifest: dict, profile: str, tier_key: str, field: str, column: str) -> str:
    paths = manifest["profile"][profile].get(tier_key, [])
    lines = [MARKER, f"| ADR | Summary | {column} |", "|-----|---------|-------------|"]
    for path in paths:
        summary = cell(manifest, profile, path, "summary")
        lines.append(f"| `{path}` | {summary} | {cell(manifest, profile, path, field)} |")
    return "\n".join(lines)


def replace_tier_table(text: str, heading: str, table: str, profile: str) -> str:
    section = re.search(rf"^## {heading}\b[^\n]*\n", text, re.M)
    if section is None:
        fail(f"{profile}: missing '## {heading}' heading")
    start = section.end()
    end_match = re.compile(r"^## ", re.M).search(text, start)
    end = end_match.start() if end_match else len(text)

    block = re.compile(
        rf"(?:{re.escape(MARKER)}\n)?^\| ADR \|[^\n]*\n\|[-| ]+\|\n(?:^\|[^\n]*\n)*", re.M)
    found = block.search(text, start, end)
    if found is None:
        fail(f"{profile}: no tier table found under '## {heading}'")
    return text[: found.start()] + table + "\n" + text[found.end():]


def main() -> int:
    check = "--check" in sys.argv
    with open(MANIFEST, "rb") as handle:
        manifest = tomllib.load(handle)

    drifted = []
    for profile in sorted(manifest.get("profile", {})):
        doc = PROFILE_DIR / f"{profile}.md"
        if not doc.is_file():
            fail(f"manifest profile '{profile}' has no {doc.name}")
        original = doc.read_text(encoding="utf-8")
        updated = original
        for heading, tier_key, field, column in TIERS:
            table = render_table(manifest, profile, tier_key, field, column)
            updated = replace_tier_table(updated, heading, table, profile)
        if updated != original:
            if check:
                drifted.append(doc.name)
            else:
                doc.write_text(updated, encoding="utf-8", newline="")
                print(f"regenerated {doc.name}")

    if check and drifted:
        print("Tier tables drift from profiles/profiles.toml in: " + ", ".join(drifted))
        print("Run: python scripts/generate_profiles.py")
        return 1
    if check:
        print(f"{len(manifest.get('profile', {}))} profiles: tier tables match the manifest.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
