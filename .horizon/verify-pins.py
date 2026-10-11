#!/usr/bin/env python3
"""Checks that .horizon/Dockerfile pins the toolchain versions the repository's own
files name. Horizon writes this file; .horizon/pins.json says where each pin comes from.

Each entry of "pins" names a Dockerfile ARG and one source:
  {"arg": "DOTNET_SDK_VERSION", "file": "global.json", "json": "sdk.version"}
  {"arg": "RUST_VERSION", "file": "rust-toolchain.toml", "toml": "toolchain.channel"}
  {"arg": "MONGODB_VERSION", "file": "Directory.Build.props", "xml": "MongoDBVersion"}
  {"arg": "NODE_VERSION", "file": ".nvmrc", "regex": "^v?(\\S+)", "compare": "prefix"}
"compare": "prefix" accepts a Dockerfile pin that refines the source, such as 22.11.0
for a source that says 22. Run from anywhere: python3 .horizon/verify-pins.py"""
import json
import re
import sys
import xml.etree.ElementTree as ElementTree
from pathlib import Path

HORIZON = Path(__file__).resolve().parent
ROOT = HORIZON.parent


def source_value(pin):
    path = ROOT / pin["file"]
    text = path.read_text(encoding="utf-8")
    if "json" in pin or "toml" in pin:
        if "json" in pin:
            data, keys = json.loads(text), pin["json"]
        else:
            import tomllib

            data, keys = tomllib.loads(text), pin["toml"]
        for key in keys.split("."):
            data = data[key]
        return str(data)
    if "xml" in pin:
        values = [(node.text or "").strip() for node in ElementTree.fromstring(text).iter(pin["xml"])]
        if len(values) != 1:
            raise ValueError(f"expected exactly one <{pin['xml']}>, found {len(values)}")
        return values[0]
    if "regex" in pin:
        match = re.search(pin["regex"], text, re.MULTILINE)
        if not match:
            raise ValueError(f"no match for {pin['regex']!r}")
        return match.group(1) if match.groups() else match.group(0)
    raise ValueError("names no json, toml, xml or regex source")


def matches(pinned, expected, compare):
    if compare == "prefix":
        return pinned == expected or pinned.startswith(expected + ".") or pinned.startswith(expected + "-")
    return pinned == expected


def main():
    pins = json.loads((HORIZON / "pins.json").read_text(encoding="utf-8")).get("pins", [])
    dockerfile = (HORIZON / "Dockerfile").read_text(encoding="utf-8")
    arguments = {
        name: value.strip("\"'")
        for name, value in re.findall(r"^\s*ARG\s+([A-Za-z0-9_]+)=(\S+)", dockerfile, re.MULTILINE)
    }
    problems, checked = [], []
    for pin in pins:
        arg = pin.get("arg", "")
        try:
            expected = source_value(pin)
        except (OSError, KeyError, TypeError, ValueError, ElementTree.ParseError) as error:
            problems.append(f"{arg}: cannot read {pin.get('file')}: {error}")
            continue
        pinned = arguments.get(arg)
        if pinned is None:
            problems.append(f"{arg}: .horizon/Dockerfile has no ARG {arg}=...")
        elif not matches(pinned, expected, pin.get("compare", "exact")):
            problems.append(f"{arg}: .horizon/Dockerfile pins {pinned}, {pin['file']} says {expected}")
        else:
            checked.append(f"{arg}={pinned}")
    if problems:
        print(".horizon/Dockerfile pins differ from the repository:\n  " + "\n  ".join(problems), file=sys.stderr)
        print("Update the pins, or run Update setup from CI in Horizon's New cloud.", file=sys.stderr)
        return 1
    print("Horizon pins match: " + (", ".join(checked) or "no pins declared"))
    return 0


if __name__ == "__main__":
    sys.exit(main())
