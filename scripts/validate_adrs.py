#!/usr/bin/env python3
"""Validate the ADR catalog: frontmatter metadata, Requires/Conflicts graph, profiles.

Run from anywhere:  python scripts/validate_adrs.py
Exit code 0 = no errors (warnings allowed), 1 = at least one error.
`--stale [months]` instead prints version-sensitive ADRs (those carrying a
`verify_against` list) whose `last_reviewed` is older than the given number
of months (default 6), then exits 0 — an informational freshness report.
Stdlib only — safe to run locally and in CI without installing anything.

Frontmatter format (strict subset of YAML, enforced here)
----------------------------------------------------------
Every ADR starts with a frontmatter block:

    ---
    category: dotnet          # must match the folder name
    stack: dotnet             # dotnet|python|typescript|angular|react|any
    status: Active            # Active|Deprecated|Superseded
    requires:                 # [] when empty; one AND-group per item;
      - adrs/a.md             # alternatives inside an item separated by " | "
      - adrs/b.md | adrs/c.md
    conflicts_with: []        # flat path list; must be symmetric; no "|"
    last_reviewed: 2026-07-29 # optional, YYYY-MM-DD
    superseded_by: adrs/x.md  # optional; requires status: Superseded
    ---

Checks
------
Per ADR file (adrs/**/*.md):
  A1  Frontmatter present, closed, no duplicate keys, required keys present
  A2  category matches the folder name; in language folders
      (dotnet/python/typescript/angular/react) stack must equal the folder
  A2b family (optional): kebab-case slug shared by sibling ADRs answering the
      same architectural question; two or more Active members required; members
      must be pairwise stack-separated or mutually conflicting; review dates
      spanning more than 90 days inside a family warn
  A3  status/stack values valid; last_reviewed is a YYYY-MM-DD date
  A4  requires/conflicts entries are adrs/<cat>/<file>.md paths;
      "|" alternatives only in requires
  A5  Referenced ADR files exist (including superseded_by targets)
  A6  No self-references
  A7  superseded_by present <=> status is Superseded
  A8  No legacy bold-line metadata (**Category:** etc.) left in the body

Graph:
  G1  Conflicts are symmetric: if A lists B, B must list A
  G2  A requires-group whose alternatives ALL conflict with the declaring
      ADR is an error (a partially conflicting group is a warning)
  G3  No cycles in the requires graph (conservative: follows every alternative)
  G4  An ADR's deterministic dependency closure (single-alternative groups)
      contains no conflicting pair

Per profile (profiles/*.md):
  P1  Every referenced ADR path exists
  P2  No ADR listed in more than one tier (warning)
  P3  No two selected ADRs conflict with each other
  P4  Every requires-group of a selected ADR (and of its transitively
      missing single dependencies) is satisfied by the profile. Missing
      dependency = warning; missing dependency whose every alternative
      conflicts with a selected ADR = error (unsatisfiable as declared).
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
ADR_DIR = ROOT / "adrs"
PROFILE_DIR = ROOT / "profiles"

REQUIRED_KEYS = ("category", "stack", "status", "requires", "conflicts_with")
OPTIONAL_KEYS = ("last_reviewed", "superseded_by", "verify_against", "family")
KNOWN_KEYS = set(REQUIRED_KEYS) | set(OPTIONAL_KEYS)
LIST_KEYS = {"requires", "conflicts_with"}

KEY_RE = re.compile(r"^([a-z_]+):\s*(.*?)\s*$")
ITEM_RE = re.compile(r"^  - (\S.*?)\s*$")
PATH_RE = re.compile(r"^adrs/[A-Za-z0-9._/-]+\.md$")
LOOSE_PATH_RE = re.compile(r"adrs/[A-Za-z0-9._/-]+\.md")
DATE_RE = re.compile(r"^\d{4}-\d{2}-\d{2}$")
FAMILY_RE = re.compile(r"^[a-z0-9][a-z0-9-]*$")
LEGACY_RE = re.compile(
    r"^\*\*(Category|Stack|Status|Requires|Conflicts with|Last reviewed|Superseded by):\*\*")

VALID_STATUS = {"Active", "Deprecated", "Superseded"}
VALID_STACKS = {"dotnet", "python", "typescript", "angular", "react", "any"}
LANGUAGE_FOLDERS = {"dotnet", "python", "typescript", "angular", "react"}

errors: list[str] = []
warnings: list[str] = []


def err(msg: str) -> None:
    errors.append(msg)


def warn(msg: str) -> None:
    warnings.append(msg)


def fmt_group(group: list[str]) -> str:
    return " | ".join(group)


def parse_frontmatter(lines: list[str], where: str) -> tuple[dict, int]:
    """Parse the strict frontmatter subset. Returns (meta, index-after-closing-fence)."""
    meta: dict = {}
    if not lines or lines[0].strip() != "---":
        err(f"{where}: file must start with a '---' frontmatter block")
        return meta, 0
    i = 1
    while i < len(lines):
        line = lines[i].rstrip()
        if line == "---":
            return meta, i + 1
        m = KEY_RE.match(line)
        if not m:
            err(f"{where}: unparseable frontmatter line {line!r}")
            i += 1
            continue
        key, value = m.group(1), m.group(2)
        if key in meta:
            err(f"{where}: duplicate frontmatter key '{key}'")
        if key not in KNOWN_KEYS:
            err(f"{where}: unknown frontmatter key '{key}'")
        if value == "":
            items: list[str] = []
            i += 1
            while i < len(lines):
                im = ITEM_RE.match(lines[i].rstrip())
                if not im:
                    break
                items.append(im.group(1))
                i += 1
            meta[key] = items
            continue
        meta[key] = [] if value == "[]" else value
        i += 1
    err(f"{where}: frontmatter block is not closed with '---'")
    return meta, len(lines)


def parse_path_items(items, where: str, allow_alternatives: bool) -> list[list[str]]:
    """Turn frontmatter list items into groups of ADR paths."""
    if not isinstance(items, list):
        err(f"{where}: expected a list (use [] when empty), got {items!r}")
        return []
    groups: list[list[str]] = []
    for item in items:
        members = [alt.strip() for alt in item.split("|")]
        if len(members) > 1 and not allow_alternatives:
            err(f"{where}: '|' alternatives are not allowed here ({item!r})")
        good: list[str] = []
        for alt in members:
            if not PATH_RE.match(alt):
                err(f"{where}: entry {alt!r} is not an adrs/<category>/<file>.md path")
                continue
            good.append(alt)
        if good:
            groups.append(good)
    return groups


def load_adrs() -> dict[str, dict]:
    adrs: dict[str, dict] = {}
    for path in sorted(ADR_DIR.rglob("*.md")):
        rel = path.relative_to(ROOT).as_posix()
        lines = path.read_text(encoding="utf-8").splitlines()
        meta, body_start = parse_frontmatter(lines, rel)
        for line in lines[body_start:]:
            if LEGACY_RE.match(line):
                err(f"{rel}: legacy bold-line metadata found in body: {line.strip()!r}")
        for key in REQUIRED_KEYS:
            if key not in meta:
                err(f"{rel}: missing frontmatter key '{key}'")
        folder = path.parent.name
        if meta.get("category") and meta["category"] != folder:
            err(f"{rel}: category '{meta['category']}' does not match folder '{folder}'")
        if meta.get("status") and meta["status"] not in VALID_STATUS:
            err(f"{rel}: unknown status '{meta['status']}'")
        stack = meta.get("stack", "")
        if stack and stack not in VALID_STACKS:
            err(f"{rel}: unknown stack '{stack}' (expected one of {', '.join(sorted(VALID_STACKS))})")
        if folder in LANGUAGE_FOLDERS and stack and stack != folder:
            err(f"{rel}: stack '{stack}' must equal the language folder '{folder}'")
        reviewed = meta.get("last_reviewed")
        if reviewed is not None and not DATE_RE.match(str(reviewed)):
            err(f"{rel}: last_reviewed '{reviewed}' is not a YYYY-MM-DD date")
        superseded_by = meta.get("superseded_by")
        if superseded_by is not None:
            if not isinstance(superseded_by, str) or not PATH_RE.match(superseded_by):
                err(f"{rel}: superseded_by must be a single adrs/... path")
                superseded_by = None
            if meta.get("status") != "Superseded":
                err(f"{rel}: has 'superseded_by' but status is '{meta.get('status')}' (must be Superseded)")
        elif meta.get("status") == "Superseded":
            err(f"{rel}: status is Superseded but no 'superseded_by' key points at the replacement")
        family = meta.get("family")
        if family is not None:
            if not isinstance(family, str) or not FAMILY_RE.match(family):
                err(f"{rel}: family '{family}' is not a kebab-case slug")
                family = None
            elif meta.get("status") == "Superseded":
                err(f"{rel}: family '{family}' on a Superseded ADR — tombstones leave their family")
                family = None
        verify_against = meta.get("verify_against")
        if verify_against is not None and not isinstance(verify_against, list):
            err(f"{rel}: verify_against must be a list of framework/version strings")
            verify_against = []
        requires = parse_path_items(meta.get("requires", []), f"{rel} [requires]",
                                    allow_alternatives=True)
        conflict_groups = parse_path_items(meta.get("conflicts_with", []), f"{rel} [conflicts_with]",
                                           allow_alternatives=False)
        conflicts = [g[0] for g in conflict_groups if g]
        adrs[rel] = {"requires": requires, "conflicts": conflicts,
                     "stack": stack, "superseded_by": superseded_by,
                     "last_reviewed": reviewed, "verify_against": verify_against or [],
                     "family": family}
    return adrs


def all_required_members(data: dict) -> list[str]:
    return [m for group in data["requires"] for m in group]


def check_families(adrs: dict[str, dict]) -> None:
    """Family = sibling ADRs answering the same architectural question with
    per-stack (or per-tool) variants. Members must be exclusive per system:
    different concrete stacks, or mutually declared conflicts."""
    import datetime

    families: dict[str, list[str]] = {}
    for rel in sorted(adrs):
        family = adrs[rel].get("family")
        if family:
            families.setdefault(family, []).append(rel)

    for family, members in sorted(families.items()):
        if len(members) < 2:
            err(f"family '{family}': only one member ({members[0]}) — "
                "a family links two or more sibling ADRs")
            continue

        for i in range(len(members)):
            for j in range(i + 1, len(members)):
                a, b = members[i], members[j]
                stack_a, stack_b = adrs[a]["stack"], adrs[b]["stack"]
                stack_split = stack_a != stack_b and "any" not in (stack_a, stack_b)
                mutual_conflict = (b in adrs[a]["conflicts"] and a in adrs[b]["conflicts"])
                if not (stack_split or mutual_conflict):
                    err(f"family '{family}': {a} and {b} are neither stack-separated "
                        "nor mutually conflicting — family members must be exclusive per system")

        dates = []
        for member in members:
            reviewed = adrs[member].get("last_reviewed")
            if reviewed and DATE_RE.match(str(reviewed)):
                dates.append(datetime.date.fromisoformat(str(reviewed)))
        if len(dates) == len(members) and (max(dates) - min(dates)).days > 90:
            warn(f"family '{family}': member review dates span more than 90 days — "
                 "siblings drift apart; re-verify them together")


def check_references(adrs: dict[str, dict]) -> None:
    for rel, data in adrs.items():
        for target in all_required_members(data):
            if target == rel:
                err(f"{rel}: requires references itself")
            elif target not in adrs:
                err(f"{rel}: requires references missing file '{target}'")
        for target in data["conflicts"]:
            if target == rel:
                err(f"{rel}: conflicts_with references itself")
            elif target not in adrs:
                err(f"{rel}: conflicts_with references missing file '{target}'")
        if data.get("superseded_by"):
            target = data["superseded_by"]
            if target == rel:
                err(f"{rel}: superseded_by references itself")
            elif target not in adrs:
                err(f"{rel}: superseded_by references missing file '{target}'")


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
                err("requires cycle: " + " -> ".join(cycle))
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
    """Transitive closure following only single-alternative requires groups."""
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


def report_stale(adrs: dict[str, dict], months: int) -> None:
    """List version-sensitive ADRs whose last review is older than `months`."""
    import datetime
    cutoff = datetime.date.today() - datetime.timedelta(days=months * 30)
    stale = []
    for rel in sorted(adrs):
        data = adrs[rel]
        if not data["verify_against"]:
            continue
        reviewed = data.get("last_reviewed")
        reviewed_date = (datetime.date.fromisoformat(str(reviewed))
                         if reviewed and DATE_RE.match(str(reviewed)) else None)
        if reviewed_date is None or reviewed_date < cutoff:
            stale.append((rel, reviewed or "never", ", ".join(data["verify_against"])))
    if not stale:
        print(f"No version-sensitive ADRs older than {months} months. All fresh.")
        return
    print(f"Version-sensitive ADRs not reviewed in the last {months} months:")
    for rel, reviewed, targets in stale:
        print(f"  {rel}  (last reviewed: {reviewed})  -> re-verify against: {targets}")


def main() -> int:
    try:  # keep output readable on Windows consoles that are not UTF-8
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass
    if not ADR_DIR.is_dir():
        print(f"error: {ADR_DIR} not found (run from the repo or keep the script in scripts/)")
        return 1
    stale_months = None
    if "--stale" in sys.argv:
        idx = sys.argv.index("--stale")
        stale_months = (int(sys.argv[idx + 1])
                        if idx + 1 < len(sys.argv) and sys.argv[idx + 1].isdigit() else 6)
    adrs = load_adrs()
    if stale_months is not None:
        report_stale(adrs, stale_months)
        return 0
    check_references(adrs)
    check_graph(adrs)
    check_families(adrs)
    if PROFILE_DIR.is_dir():
        check_profiles(adrs)
    enforcement_dir = ROOT / "enforcement"
    if enforcement_dir.is_dir():
        for path in sorted(enforcement_dir.rglob("*")):
            if not path.is_file():
                continue
            rel = path.relative_to(ROOT).as_posix()
            text = path.read_text(encoding="utf-8", errors="ignore")
            for ref in LOOSE_PATH_RE.findall(text):
                if ref not in adrs:
                    err(f"{rel}: references missing ADR '{ref}'")

    for msg in errors:
        print(f"ERROR   {msg}")
    for msg in warnings:
        print(f"WARNING {msg}")
    print(f"\n{len(adrs)} ADRs, {len(list(PROFILE_DIR.glob('*.md')))} profiles checked: "
          f"{len(errors)} error(s), {len(warnings)} warning(s)")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
