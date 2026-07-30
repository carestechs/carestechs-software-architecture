#!/usr/bin/env python3
"""Validate the ADR catalog: metadata format, Requires/Conflicts graph, and profiles.

Run from anywhere:  python scripts/validate_adrs.py
Exit code 0 = no errors (warnings allowed), 1 = at least one error.

Metadata syntax
---------------
  **Stack:** dotnet | python | typescript | angular | react | any
      Which technology stack the ADR's constraints assume; `any` = cross-stack.
  **Requires:** `adrs/a.md`, `adrs/b.md` | `adrs/c.md`
      Comma separates AND-groups; `|` inside a group separates alternatives —
      the group is satisfied when ANY one alternative is selected.
  **Conflicts with:** `adrs/x.md`, `adrs/y.md`
      Flat comma-separated list; `|` is not allowed. Conflicts MUST be
      declared symmetrically (if A lists B, B must list A).
  **Last reviewed:** YYYY-MM-DD          (optional)
  **Superseded by:** `adrs/<path>.md`    (optional; requires Status: Superseded)
  Use the em dash (—) for an empty value; the non-optional lines are mandatory.

Checks
------
Per ADR file (adrs/**/*.md):
  A1  Category / Stack / Status / Requires / Conflicts-with lines present,
      no duplicates
  A2  Category field matches the folder name; in language folders
      (dotnet/python/typescript/angular/react) Stack must equal the folder name
  A3  Status is one of: Active, Deprecated, Superseded; Stack is a valid value;
      Last reviewed (if present) is a YYYY-MM-DD date
  A4  Requires / Conflicts entries are backticked `adrs/<cat>/<file>.md` paths
      (semicolons and un-backticked paths are errors; `|` only in Requires)
  A5  Referenced ADR files exist (including Superseded by targets)
  A6  No self-references
  A7  Superseded by present <=> Status is Superseded

Graph:
  G1  Conflicts are symmetric: if A lists B, B must list A
  G2  A Requires group whose alternatives ALL conflict with the declaring ADR
      is an error (a partially conflicting group is a warning)
  G3  No cycles in the Requires graph (conservative: follows every alternative)
  G4  An ADR's deterministic dependency closure (single-alternative groups)
      contains no conflicting pair

Per profile (profiles/*.md):
  P1  Every referenced ADR path exists
  P2  No ADR listed in more than one tier (warning)
  P3  No two selected ADRs conflict with each other
  P4  Every Requires group of a selected ADR (and of its transitively missing
      single dependencies) is satisfied by the profile. Missing dependency =
      warning; missing dependency whose every alternative conflicts with a
      selected ADR = error (the profile set is unsatisfiable as declared).
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ADR_DIR = ROOT / "adrs"
PROFILE_DIR = ROOT / "profiles"

META_KEYS = ("Category", "Stack", "Status", "Requires", "Conflicts with")
OPTIONAL_KEYS = ("Last reviewed", "Superseded by")
META_RE = re.compile(
    r"^\*\*(Category|Stack|Status|Requires|Conflicts with|Last reviewed|Superseded by):\*\*\s*(.*?)\s*$")
BACKTICK_PATH_RE = re.compile(r"`(adrs/[A-Za-z0-9._/-]+\.md)`")
LOOSE_PATH_RE = re.compile(r"adrs/[A-Za-z0-9._/-]+\.md")
DATE_RE = re.compile(r"^\d{4}-\d{2}-\d{2}$")
VALID_STATUS = {"Active", "Deprecated", "Superseded"}
VALID_STACKS = {"dotnet", "python", "typescript", "angular", "react", "any"}
LANGUAGE_FOLDERS = {"dotnet", "python", "typescript", "angular", "react"}
EMPTY_MARKERS = {"—", "-", "–", ""}  # em dash, hyphen, en dash

errors: list[str] = []
warnings: list[str] = []


def err(msg: str) -> None:
    errors.append(msg)


def warn(msg: str) -> None:
    warnings.append(msg)


def fmt_group(group: list[str]) -> str:
    return " | ".join(group)


def parse_groups(raw: str, where: str, allow_alternatives: bool) -> list[list[str]]:
    """Parse a metadata value into AND-groups of alternative ADR paths."""
    if raw in EMPTY_MARKERS:
        return []
    if ";" in raw:
        err(f"{where}: uses ';' as separator — groups must be comma-separated")
        raw = raw.replace(";", ",")
    groups: list[list[str]] = []
    for part in raw.split(","):
        part = part.strip()
        if not part:
            continue
        members: list[str] = []
        for alt in part.split("|"):
            alt = alt.strip()
            paths = BACKTICK_PATH_RE.findall(alt)
            loose = LOOSE_PATH_RE.findall(alt)
            if len(loose) != len(paths):
                err(f"{where}: ADR path(s) not wrapped in backticks in {alt!r}")
            if len(paths) != 1:
                err(f"{where}: unparseable entry {alt!r} (expected exactly one backticked adrs/... path)")
                continue
            members.append(paths[0])
        if len(members) > 1 and not allow_alternatives:
            err(f"{where}: '|' alternatives are not allowed here ({fmt_group(members)})")
        if members:
            groups.append(members)
    return groups


def load_adrs() -> dict[str, dict]:
    adrs: dict[str, dict] = {}
    for path in sorted(ADR_DIR.rglob("*.md")):
        rel = path.relative_to(ROOT).as_posix()
        meta: dict[str, str] = {}
        for line in path.read_text(encoding="utf-8").splitlines():
            m = META_RE.match(line)
            if m:
                key, value = m.group(1), m.group(2)
                if key in meta:
                    err(f"{rel}: duplicate metadata line '**{key}:**'")
                meta[key] = value
        for key in META_KEYS:
            if key not in meta:
                err(f"{rel}: missing metadata line '**{key}:**' (use — if none)")
        folder = path.parent.name
        if meta.get("Category") and meta["Category"] != folder:
            err(f"{rel}: Category '{meta['Category']}' does not match folder '{folder}'")
        if meta.get("Status") and meta["Status"] not in VALID_STATUS:
            err(f"{rel}: unknown Status '{meta['Status']}'")
        stack = meta.get("Stack", "")
        if stack and stack not in VALID_STACKS:
            err(f"{rel}: unknown Stack '{stack}' (expected one of {', '.join(sorted(VALID_STACKS))})")
        if folder in LANGUAGE_FOLDERS and stack and stack != folder:
            err(f"{rel}: Stack '{stack}' must equal the language folder '{folder}'")
        reviewed = meta.get("Last reviewed")
        if reviewed is not None and not DATE_RE.match(reviewed):
            err(f"{rel}: Last reviewed '{reviewed}' is not a YYYY-MM-DD date")
        superseded_by = None
        if "Superseded by" in meta:
            targets = BACKTICK_PATH_RE.findall(meta["Superseded by"])
            if len(targets) != 1:
                err(f"{rel}: Superseded by must contain exactly one backticked adrs/... path")
            else:
                superseded_by = targets[0]
            if meta.get("Status") != "Superseded":
                err(f"{rel}: has 'Superseded by' but Status is '{meta.get('Status')}' (must be Superseded)")
        elif meta.get("Status") == "Superseded":
            err(f"{rel}: Status is Superseded but no 'Superseded by' line points at the replacement")
        requires = parse_groups(meta.get("Requires", "—"), f"{rel} [Requires]", allow_alternatives=True)
        conflict_groups = parse_groups(meta.get("Conflicts with", "—"), f"{rel} [Conflicts with]",
                                       allow_alternatives=False)
        conflicts = [g[0] for g in conflict_groups if g]
        adrs[rel] = {"requires": requires, "conflicts": conflicts,
                     "stack": stack, "superseded_by": superseded_by}
    return adrs


def all_required_members(data: dict) -> list[str]:
    return [m for group in data["requires"] for m in group]


def check_references(adrs: dict[str, dict]) -> None:
    for rel, data in adrs.items():
        for target in all_required_members(data):
            if target == rel:
                err(f"{rel}: Requires references itself")
            elif target not in adrs:
                err(f"{rel}: Requires references missing file '{target}'")
        for target in data["conflicts"]:
            if target == rel:
                err(f"{rel}: Conflicts with references itself")
            elif target not in adrs:
                err(f"{rel}: Conflicts with references missing file '{target}'")
        if data.get("superseded_by"):
            target = data["superseded_by"]
            if target == rel:
                err(f"{rel}: Superseded by references itself")
            elif target not in adrs:
                err(f"{rel}: Superseded by references missing file '{target}'")


def conflicts_between(a: str, b: str, adrs: dict[str, dict]) -> bool:
    return (b in adrs.get(a, {}).get("conflicts", [])
            or a in adrs.get(b, {}).get("conflicts", []))


def check_graph(adrs: dict[str, dict]) -> None:
    # G1 symmetry
    for rel, data in adrs.items():
        for target in data["conflicts"]:
            if target in adrs and rel not in adrs[target]["conflicts"]:
                err(f"asymmetric conflict: {rel} lists {target}, but {target} does not list {rel}")
    # G2 requires-group vs conflicts
    for rel, data in adrs.items():
        for group in data["requires"]:
            clashing = [m for m in group if conflicts_between(rel, m, adrs)]
            if clashing and len(clashing) == len(group):
                err(f"{rel}: requires {fmt_group(group)} but every alternative conflicts with it")
            elif clashing:
                warn(f"{rel}: requires alternative(s) {fmt_group(clashing)} that conflict with it "
                     f"(other alternatives remain usable)")
    # G3 cycles (conservative: follow every alternative)
    WHITE, GRAY, BLACK = 0, 1, 2
    color = {rel: WHITE for rel in adrs}

    def visit(node: str, stack: list[str]) -> None:
        color[node] = GRAY
        for nxt in all_required_members(adrs[node]):
            if nxt not in adrs:
                continue
            if color[nxt] == GRAY:
                cycle = stack[stack.index(nxt):] + [nxt] if nxt in stack else [node, nxt]
                err("Requires cycle: " + " -> ".join(cycle))
            elif color[nxt] == WHITE:
                visit(nxt, stack + [nxt])
        color[node] = BLACK

    for rel in adrs:
        if color[rel] == WHITE:
            visit(rel, [rel])

    # G4 deterministic-closure self-consistency
    for rel in adrs:
        closure = deterministic_closure({rel}, adrs)
        items = sorted(closure)
        for i, a in enumerate(items):
            for b in items[i + 1:]:
                if b in adrs.get(a, {}).get("conflicts", []):
                    err(f"{rel}: its dependency closure contains conflicting pair {a} <-> {b}")


def deterministic_closure(seed: set[str], adrs: dict[str, dict]) -> set[str]:
    """Transitive closure following only single-alternative Requires groups."""
    closure = set(seed)
    frontier = list(seed)
    while frontier:
        node = frontier.pop()
        for group in adrs.get(node, {}).get("requires", []):
            if len(group) == 1 and group[0] not in closure:
                closure.add(group[0])
                frontier.append(group[0])
    return closure


TIER_HEADINGS = ("## Required", "## Recommended", "## Optional")


def parse_profile(path: Path) -> dict[str, list[str]]:
    tiers: dict[str, list[str]] = {"Required": [], "Recommended": [], "Optional": []}
    current: str | None = None
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("## "):
            current = None
            for heading in TIER_HEADINGS:
                if line.startswith(heading):
                    current = heading.removeprefix("## ")
        elif current:
            tiers[current].extend(LOOSE_PATH_RE.findall(line))
    return tiers


def check_profiles(adrs: dict[str, dict]) -> None:
    for path in sorted(PROFILE_DIR.glob("*.md")):
        rel = path.relative_to(ROOT).as_posix()
        tiers = parse_profile(path)
        selected: dict[str, str] = {}
        for tier, entries in tiers.items():
            for adr in entries:
                if adr not in adrs:
                    err(f"{rel}: references missing ADR '{adr}' ({tier})")
                    continue
                if adr in selected and selected[adr] != tier:
                    warn(f"{rel}: '{adr}' appears in both {selected[adr]} and {tier}")
                selected[adr] = tier
        sel = set(selected) & set(adrs)
        # P3 direct conflicts within the selected set
        for a in sorted(sel):
            for b in adrs[a]["conflicts"]:
                if b in sel and a < b:
                    err(f"{rel}: selected ADRs conflict: {a} <-> {b}")
        # P4 requires satisfaction (group-aware; traverses missing single deps)
        to_check = sorted(sel)
        checked: set[str] = set()
        missing_single: dict[str, set[str]] = {}
        missing_groups: list[tuple[str, list[str]]] = []
        while to_check:
            a = to_check.pop()
            if a in checked or a not in adrs:
                continue
            checked.add(a)
            for group in adrs[a]["requires"]:
                if any(m in sel for m in group):
                    continue
                if len(group) == 1:
                    missing_single.setdefault(group[0], set()).add(a)
                    to_check.append(group[0])
                else:
                    missing_groups.append((a, group))
        for dep in sorted(missing_single):
            needed_by = ", ".join(sorted(missing_single[dep]))
            blockers = sorted(s for s in sel if conflicts_between(dep, s, adrs))
            if blockers:
                err(f"{rel}: unsatisfiable — '{dep}' is required (by {needed_by}) "
                    f"but conflicts with selected {', '.join(blockers)}")
            else:
                warn(f"{rel}: '{dep}' is required (by {needed_by}) but not listed in any tier")
        for needer, group in missing_groups:
            usable = [m for m in group
                      if not any(conflicts_between(m, s, adrs) for s in sel)]
            if not usable:
                err(f"{rel}: unsatisfiable — {needer} requires one of {fmt_group(group)}, "
                    f"and every alternative conflicts with the selected set")
            else:
                warn(f"{rel}: {needer} requires one of {fmt_group(group)} — none listed "
                     f"(addable: {fmt_group(usable)})")


def main() -> int:
    try:  # keep output readable on Windows consoles that are not UTF-8
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    if not ADR_DIR.is_dir():
        print(f"error: {ADR_DIR} not found (run from the repo or keep the script in scripts/)")
        return 1
    adrs = load_adrs()
    check_references(adrs)
    check_graph(adrs)
    if PROFILE_DIR.is_dir():
        check_profiles(adrs)

    for msg in errors:
        print(f"ERROR   {msg}")
    for msg in warnings:
        print(f"WARNING {msg}")
    print(f"\n{len(adrs)} ADRs, {len(list(PROFILE_DIR.glob('*.md')))} profiles checked: "
          f"{len(errors)} error(s), {len(warnings)} warning(s)")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
