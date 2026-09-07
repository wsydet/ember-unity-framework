#!/usr/bin/env python3
"""Read-only inventory and local Markdown link check for an Ember checkout."""
import argparse
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys
from urllib.parse import unquote, urlsplit

DOC_EXTENSIONS = {'.md', '.mdx', '.rst', '.adoc', '.txt', '.pdf', '.docx'}
EXCLUDED = {'.git', '.utmp', '.tmp', '.vs', 'Library', 'Temp', 'Logs', 'obj',
            'Build', 'Builds', 'UserSettings', 'node_modules', 'upm-stage'}


def inventory(root):
    if shutil.which('rg'):
        args = ['rg', '--files', '--hidden', '-g', '!.git/**']
        for name in sorted(EXCLUDED - {'.git'}):
            args += ['-g', '!' + name + '/**']
        process = subprocess.run(args, cwd=root, capture_output=True)
        if process.returncode not in (0, 1):
            raise RuntimeError(process.stderr.decode('utf-8', errors='replace'))
        output = process.stdout
        names = output.decode('utf-8').splitlines()
    else:
        output = subprocess.run(['git', 'ls-files', '--cached', '--others',
                                 '--exclude-standard', '-z'], cwd=root, check=True,
                                capture_output=True).stdout
        names = output.decode('utf-8').split('\0')
    return sorted({Path(n.replace('\\', '/')).as_posix() for n in names if n
                   and not (set(Path(n).parts) & EXCLUDED)
                   and Path(n).suffix.lower() in DOC_EXTENSIONS
                   and (root / n).is_file()})


def category(name):
    parts = Path(name).parts
    if 'Templates~' in parts:
        return 'template_snapshot'
    if (name.startswith(('Assets/ThirdParty/', 'Assets/Plugins/', 'Assets/TextMesh Pro/',
                         'Assets/Art/Icons/', 'Packages/com.ember/UniTask/'))
            or (name.startswith('Packages/') and not name.startswith('Packages/com.ember/'))
            or re.search(r'(^|/)(license|copying|notice|changelog)(\.|$)', name, re.I)):
        return 'preserve_history_or_vendor'
    if Path(name).suffix.lower() == '.txt':
        return 'configuration_or_text_review'
    return 'project_document'


def without_fences(text):
    lines, fence, fence_length = [], None, 0
    for line in text.splitlines():
        marker = re.match(r'^\s{0,3}(`{3,}|~{3,})', line)
        if marker and fence is None:
            fence, fence_length = marker[1][0], len(marker[1])
            lines.append('')
        elif (marker and marker[1][0] == fence and len(marker[1]) >= fence_length
              and not line[marker.end():].strip()):
            fence, fence_length = None, 0
            lines.append('')
        else:
            lines.append(line if fence is None else '')
    return '\n'.join(lines)


def local_links(text):
    # Inline destinations (including <paths with spaces>) and reference definitions.
    text = without_fences(text)
    inline = r'!?\[[^\]\n]*\]\(\s*(<[^>\n]+>|[^\s)]+)(?:\s+["\'][^\n]*?["\'])?\s*\)'
    reference = r'^\s{0,3}\[[^\]\n]+\]:\s*(<[^>\n]+>|\S+)'
    for regex in (inline, reference):
        for match in re.finditer(regex, text, re.M):
            yield text[:match.start()].count('\n') + 1, match[1].strip('<>')


def audit(root):
    files = inventory(root)
    documents, issues = [], []
    for name in files:
        path = root / name
        kind = category(name)
        documents.append({'path': name, 'category': kind, 'bytes': path.stat().st_size})
        if kind != 'project_document' or path.suffix.lower() not in {'.md', '.mdx'}:
            continue
        content = path.read_text(encoding='utf-8-sig')
        for line, target in local_links(content):
            if target.startswith('#') or urlsplit(target).scheme or target.startswith('//'):
                continue
            local = unquote(target.split('#', 1)[0].split('?', 1)[0]).replace('\\', '/')
            if not local:
                continue
            destination = (root / local.lstrip('/') if local.startswith('/')
                           else path.parent / local).resolve()
            if not destination.exists():
                issues.append({'path': name, 'line': line, 'target': target,
                               'kind': 'missing_local_target'})
    return {'root': str(root), 'documents': documents, 'issues': issues,
            'limitations': ['No network requests; external URLs are not verified.',
                            'Checks local file destinations, not heading anchors or API semantics.',
                            'Inventory categories require human review; no automatic deletion.',
                            'Ignored caches, template snapshots and vendor/license/history content '
                            'are not treated as current project instructions.']}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path.cwd(), help='Unity repository root')
    parser.add_argument('--report', type=Path, help='Optional JSON report path')
    parser.add_argument('--strict', action='store_true', help='Exit 1 for missing local targets')
    args = parser.parse_args()
    root = args.root.resolve()
    if not (root / 'Packages/manifest.json').is_file():
        parser.error('--root must contain Packages/manifest.json')
    result = audit(root)
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'documents': len(result['documents']), 'issues': result['issues']},
                     ensure_ascii=True, indent=2))
    return 1 if args.strict and result['issues'] else 0


if __name__ == '__main__':
    sys.exit(main())
