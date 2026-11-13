from pathlib import Path
import json

evidence = Path(__file__).resolve().parent
destination = evidence.parents[4] / 'Windward/docs/design/planning/editor-performance-20261113/implementation.md'
original = (evidence / 'windward-implementation-before.md').read_bytes()
current = destination.read_bytes()
old_append = (evidence / 'consumer-append-before-final.md').read_text(encoding='utf-8').strip()
new_append = (evidence.parent / 'consumer-receipt-append.md').read_bytes()
assert current.startswith(original), 'Consumer original receipt changed; preserve it and stop.'
assert current[len(original):].decode('utf-8').strip() == old_append, 'Consumer suffix has unrelated changes; stop.'
updated = original + b'\n\n' + new_append.lstrip() + b'\n'
destination.write_bytes(updated)
assert destination.read_bytes().startswith(original)
(evidence / 'consumer-receipt-linked.json').write_text(json.dumps({
    'original_prefix_bytes_unchanged': True,
    'original_bytes': len(original),
    'appended_bytes': len(updated) - len(original),
    'destination': 'Windward/docs/design/planning/editor-performance-20261113/implementation.md'
}, indent=2), encoding='utf-8')
print('Updated only the task-owned receipt appendix; original bytes preserved.')
