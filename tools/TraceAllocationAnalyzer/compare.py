"""Side-by-side comparison of the 4 TraceAllocationAnalyzer outputs.

Usage: python compare.py <dir-with-alloc-*.txt>
Expects files: alloc-h1-0.txt, alloc-h1-8k.txt, alloc-h2-0.txt, alloc-h2-8k.txt
"""

import re
import sys
from pathlib import Path

CASES = [("h1-0", "H1/0"), ("h1-8k", "H1/8k"), ("h2-0", "H2/0"), ("h2-8k", "H2/8k")]
SECTIONS = ["Top allocated types", "Top Fluxzy frames (first Fluxzy.* on stack)"]

UNIT_MULT = {"B": 1, "KB": 1024, "MB": 1024 ** 2, "GB": 1024 ** 3}


def to_bytes(s: str) -> int:
    m = re.match(r"([\d.]+)\s*(B|KB|MB|GB)", s)
    if not m:
        return 0
    return int(float(m.group(1)) * UNIT_MULT[m.group(2)])


def fmt_bytes(n: int) -> str:
    v = float(n)
    for u in ("B", "KB", "MB", "GB"):
        if v < 1024 or u == "GB":
            return f"{v:.2f} {u}"
        v /= 1024
    return f"{v:.2f} GB"


def parse(path: Path) -> dict:
    """Return {section_title: {name: bytes}} + header metadata."""
    text = path.read_text(encoding="utf-8", errors="replace")
    result = {"meta": {}, "sections": {}}

    m = re.search(r"Estimated bytes \(×100K\)\s*:\s*([\d.]+\s*[A-Z]+)", text)
    if m:
        result["meta"]["total"] = to_bytes(m.group(1))
    m = re.search(r"AllocationTick events\s*:\s*([\d ,]+)", text)
    if m:
        result["meta"]["events"] = int(m.group(1).replace(" ", "").replace(",", ""))

    for section in SECTIONS:
        # Each section is: "--- <title> ---" then header line "Bytes   %   Name" then rows
        pat = re.compile(
            r"---\s*" + re.escape(section) + r"\s*---\s*\n"
            r"\s*Bytes\s+%\s+Name\s*\n"
            r"((?:.+\n)+?)(?=\n|---)",
            re.MULTILINE,
        )
        m = pat.search(text)
        if not m:
            continue
        rows = {}
        for line in m.group(1).splitlines():
            line = line.rstrip()
            if not line.strip():
                break
            row_m = re.match(r"\s*([\d.]+\s+(?:B|KB|MB|GB))\s+([\d.]+%|-)?\s+(.*)", line)
            if not row_m:
                continue
            bytes_val = to_bytes(row_m.group(1))
            name = row_m.group(3).strip()
            rows[name] = bytes_val
        result["sections"][section] = rows

    return result


def shorten(name: str) -> str:
    # Strip common prefixes and signatures
    name = re.sub(r"\([^)]*\)", "()", name)
    name = name.replace("System.Runtime.CompilerServices.", "SRC.")
    name = name.replace("System.Threading.Tasks.", "STT.")
    name = name.replace("System.Collections.Generic.", "SCG.")
    name = name.replace("System.Net.Http.", "SNH.")
    name = name.replace("Fluxzy.", "F.")
    return name


def print_section(section: str, parsed: dict, top_n: int = 20):
    print(f"\n### {section}")
    print()
    # Collect union of top-N names across all cases
    names_union = set()
    for _, label in CASES:
        rows = parsed[label]["sections"].get(section, {})
        for name, _ in sorted(rows.items(), key=lambda x: -x[1])[:top_n]:
            names_union.add(name)

    # Rank by max value across cases
    def max_across(name):
        return max(parsed[label]["sections"].get(section, {}).get(name, 0) for _, label in CASES)

    ranked = sorted(names_union, key=max_across, reverse=True)

    totals = {label: parsed[label]["meta"].get("total", 0) for _, label in CASES}
    header = f"| {'name':<80} |"
    for _, label in CASES:
        header += f" {label:>10} |"
    print(header)
    print("|" + "-" * 82 + "|" + ("|".join(["-" * 12] * 4)) + "|")

    for name in ranked:
        short = shorten(name)[:80]
        row = f"| {short:<80} |"
        for _, label in CASES:
            b = parsed[label]["sections"].get(section, {}).get(name, 0)
            t = totals[label]
            pct = (100 * b / t) if t else 0
            cell = f"{fmt_bytes(b)} ({pct:.1f}%)"
            row += f" {cell:>10} |"
        print(row)


def main():
    if len(sys.argv) != 2:
        print(__doc__)
        sys.exit(2)
    directory = Path(sys.argv[1])

    parsed = {}
    for key, label in CASES:
        f = directory / f"alloc-{key}.txt"
        if not f.exists():
            print(f"Missing: {f}")
            sys.exit(1)
        parsed[label] = parse(f)

    print("# Allocation comparison across 4 benchmark cases\n")
    print("## Totals (estimated, AllocationTick × 100KB)\n")
    print("| case | total bytes | events |")
    print("|------|-------------|--------|")
    for _, label in CASES:
        print(f"| {label} | {fmt_bytes(parsed[label]['meta']['total'])} | {parsed[label]['meta']['events']:,} |")

    for section in SECTIONS:
        print_section(section, parsed, top_n=20)


if __name__ == "__main__":
    main()
