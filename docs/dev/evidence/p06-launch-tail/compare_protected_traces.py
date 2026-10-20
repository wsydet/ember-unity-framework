"""Compare before/after Unity test output; no Unity execution or source mutation."""
import argparse
import json
import math
from pathlib import Path
import xml.etree.ElementTree as ET


def traces(path):
    result = {}
    for case in ET.parse(path).getroot().iter("test-case"):
        for line in (case.findtext("output") or "").splitlines():
            if not line.startswith("PROTECTED\t"):
                continue
            fields = line.split("\t")
            scenario, index = fields[1], int(fields[2])
            samples = result.setdefault(scenario, {})
            if index in samples:
                raise ValueError(f"Duplicate {scenario} sample {index}")
            samples[index] = ([float(v) for v in fields[3:8]], fields[8:])
    if not result:
        raise ValueError(f"No PROTECTED samples in {path}")
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("before", type=Path)
    parser.add_argument("after", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    before, after = traces(args.before), traces(args.after)
    if set(before) != set(after) or len(before) != 15:
        raise ValueError("Expected the same 15 protected scenarios")
    rows = []
    for scenario in sorted(before):
        a, b = before[scenario], after[scenario]
        if set(a) != set(b) or len(a) != 200:
            raise ValueError(f"{scenario}: expected matching 200 fixed steps")
        max_delta = [0.0] * 5
        for i in a:
            if a[i][1] != b[i][1]:
                raise ValueError(f"{scenario}[{i}]: state / qualification / origin differs")
            for j, (x, y) in enumerate(zip(a[i][0], b[i][0])):
                if not math.isfinite(x) or not math.isfinite(y):
                    raise ValueError(f"{scenario}[{i}]: non-finite value")
                delta = abs(x - y)
                max_delta[j] = max(max_delta[j], delta)
                if delta > 1e-5:
                    raise ValueError(f"{scenario}[{i}] component {j}: {x} != {y}")
        rows.append({"scenario": scenario, "samples": len(a),
                     "maxAbsoluteDeltaPositionXYVelocityXYGravity": max_delta,
                     "stateQualificationOriginIdentical": True})
    report = {"before": args.before.as_posix(), "after": args.after.as_posix(),
              "fixedStep": 0.02, "input": "synthetic keyboard",
              "tolerance": 1e-5, "passed": True, "scenarios": rows,
              "boundary": "Maintenance contract fixtures, not Windward product acceptance."}
    text = json.dumps(report, ensure_ascii=False, indent=2) + "\n"
    if args.output:
        if args.output.exists():
            raise FileExistsError(args.output)
        args.output.write_text(text, encoding="utf-8")
    print(text)


if __name__ == "__main__":
    main()
