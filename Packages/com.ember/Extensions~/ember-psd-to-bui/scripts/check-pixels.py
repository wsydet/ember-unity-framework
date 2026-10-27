"""Read-only pixel checks for the S4 pilot. Requires Pillow; never edits images."""
import hashlib
import json
import sys
from pathlib import Path
from PIL import Image, ImageChops

root = Path(sys.argv[1]).resolve()
art = root / "art/ui/S4"
bui = json.loads((art / "EmberPilot.bui").read_text(encoding="utf-8"))
rows = []
for node in bui["nodes"]:
    r = node.get("resource")
    if not r:
        continue
    file = art / bui["assetsRoot"] / r["asset"]
    with Image.open(file) as image:
        image.load()
        assert image.size == (r["width"], r["height"])
        alpha = image.convert("RGBA").getchannel("A")
        rows.append({"nodeId": node["id"], "file": str(file.relative_to(root)),
                     "size": list(image.size), "alphaBounds": alpha.getbbox(),
                     "alphaExtrema": alpha.getextrema(), "designSize": node["rect"]["sizeDelta"],
                     "sha256": hashlib.sha256(file.read_bytes()).hexdigest()})
source = root / "Assets/GameResource/Resources/UI/Common/Atlas/DefaultLight/RoundedSurfaceHiDPI.png"
surface = next(n["resource"] for n in bui["nodes"] if n["id"] == "psd_layer_20")
with Image.open(source) as original, Image.open(art / bui["assetsRoot"] / surface["asset"]) as compact:
    original, compact = original.convert("RGBA"), compact.convert("RGBA")
    assert original.size == (256, 256) and compact.size == (130, 130)
    corners = []
    for x, y in [(0, 0), (1, 0), (0, 1), (1, 1)]:
        before = original.crop((x * 192, y * 192, x * 192 + 64, y * 192 + 64))
        after = compact.crop((x * 66, y * 66, x * 66 + 64, y * 66 + 64))
        diff = ImageChops.difference(before, after)
        extrema = diff.getextrema()
        corners.append({"corner": [x, y], "maxChannelDelta": max(v[1] for v in extrema)})
    assert all(c["maxChannelDelta"] == 0 for c in corners), corners
result = {"status": "passed", "resources": rows, "nineSliceCorners": corners,
          "limits": ["Pilot uses baked S2 rounded pixels, not Photoshop layer shadows/strokes.",
                     "Native layer effect and TMP rendering fidelity are not established by this pixel check."]}
print(json.dumps(result, ensure_ascii=False, indent=2))
