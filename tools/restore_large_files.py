#!/usr/bin/env python3
"""Reconstruct the four large ZERO // RISE files after cloning this snapshot."""
from pathlib import Path

parts_root = Path(".zero-rise-parts")
if not parts_root.exists():
    print("No split files found; the project is already restored.")
    raise SystemExit(0)

restored = 0
for directory in sorted(p for p in parts_root.rglob("*") if p.is_dir()):
    chunks = sorted(directory.glob("part-*"))
    if not chunks:
        continue
    target = Path(*directory.parts[1:])
    target.parent.mkdir(parents=True, exist_ok=True)
    with target.open("wb") as output:
        for chunk in chunks:
            output.write(chunk.read_bytes())
    print(f"Restored {target}")
    restored += 1

print(f"Restored {restored} large files. The source tree is ready.")
