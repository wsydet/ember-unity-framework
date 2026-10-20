"""Summarize saved Unity results without executing Unity or replacing raw evidence."""
import argparse
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path


def summarize(path):
    root = ET.parse(path).getroot()
    cases = list(root.iter("test-case"))
    fps = []
    transitions = []
    for case in cases:
        output = case.findtext("output") or ""
        for match in re.finditer(
            r"P06 targetFPS=(\d+) actualMeanFPS=([\d.E+-]+) dt=([\d.E+-]+) "
            r"frames=(\d+) seconds=([\d.E+-]+)", output
        ):
            rate, actual, step, frames, seconds = match.groups()
            fps.append(dict(targetFPS=int(rate), actualMeanFPS=float(actual),
                            fixedStep=float(step), frames=int(frames), seconds=float(seconds)))
        if "LaunchTail_FullPhysicsTrajectory" in case.get("fullname", ""):
            lines = [line for line in output.splitlines() if line.startswith("P06 input=")]
            first_tail = next((i for i, line in enumerate(lines) if "hold=False" in line), None)
            if first_tail is not None:
                transitions.append(dict(test=case.get("fullname"),
                                        finalHold=lines[first_tail - 1],
                                        firstTail=lines[first_tail],
                                        lastSample=lines[-1], sampleCount=len(lines)))
    return dict(file=path.as_posix(), result=root.get("result"),
                total=int(root.get("total")), passed=int(root.get("passed")),
                failed=int(root.get("failed")), skipped=int(root.get("skipped")),
                durationSeconds=float(root.get("duration")),
                failures=[dict(test=c.get("fullname"), message=c.findtext("failure/message"),
                               stackTrace=c.findtext("failure/stack-trace"))
                          for c in cases if c.get("result") == "Failed"],
                actualFrameRates=fps, holdTailTransitions=transitions)


parser = argparse.ArgumentParser()
parser.add_argument("results", type=Path, nargs="+")
parser.add_argument("--output", type=Path, required=True)
args = parser.parse_args()
report = dict(input="synthetic keyboard", physics="Unity local PhysicsScene2D",
              warning="Target FPS is not measured FPS. These are maintenance fixtures, not product acceptance.",
              runs=[summarize(path) for path in args.results])
with args.output.open("x", encoding="utf-8") as stream:
    json.dump(report, stream, ensure_ascii=False, indent=2)
    stream.write("\n")
print(json.dumps({r["file"]: {k: r[k] for k in ("total", "passed", "failed", "skipped")}
                  for r in report["runs"]}, indent=2))
