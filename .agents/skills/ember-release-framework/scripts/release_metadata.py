"""Generate release metadata; dry-run by default. Does not run Git or Unity."""
import argparse
import copy
import json
from pathlib import Path
import re
import sys

FIELDS = ('id', 'version', 'frameworkVersion', 'channel', 'parentId',
          'parentVersion', 'parentContentHash', 'contentHash', 'versionedContentHash')


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def version(value):
    if not re.fullmatch(r'(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)', value):
        raise ValueError('Expected numeric major.minor.patch: ' + value)
    return tuple(map(int, value.split('.')))


def generate(repo, baseline, target, coverage, validation, status):
    repo = Path(repo).resolve()
    pkg = repo / 'Packages/com.ember'
    for marker in ('CLAUDE.md', 'scripts/upm-bump-version.ps1', 'docs/dev/upm-migration-plan.md'):
        if not (repo / marker).is_file():
            raise ValueError('Not an Ember source checkout; missing ' + marker)
    if not pkg.resolve().is_relative_to(repo):
        raise ValueError('Package must be inside source checkout')
    if version(target) <= version(baseline):
        raise ValueError('Target must be newer than baseline')
    package = read(pkg / 'package.json')
    if package.get('name') != 'com.ember' or package.get('version') != target:
        raise ValueError('Update the actual com.ember package version first')
    if not all(s.strip() for s in (coverage, validation, status)):
        raise ValueError('Current coverage, validation and status are required')
    directory = pkg / 'Dependencies~'
    release = copy.deepcopy(read(directory / f'release-{baseline}.json'))
    manifest = copy.deepcopy(read(directory / f'manifest-{baseline}.json'))
    if release.get('frameworkVersion') != baseline or release.get('frameworkTag') != 'v' + baseline:
        raise ValueError('Baseline release identity mismatch')
    if release.get('schemaVersion') != 1:
        raise ValueError('Unknown release schema; review before generation')
    url = manifest['dependencies']['com.ember']
    prefix, sep, ref = url.rpartition('#')
    if not sep or ref != 'v' + baseline:
        raise ValueError('Baseline manifest tag mismatch')
    templates = {}
    for path in sorted((pkg / 'Templates~').glob('*/template.json')):
        if path.parent.name.startswith('.'):
            continue
        item = read(path)
        key = item['id']
        if key != path.parent.name or key in templates:
            raise ValueError('Template identity mismatch: ' + key)
        if not item.get('contentHash') or item['contentHash'] != item.get('versionedContentHash'):
            raise ValueError('Unsealed template: ' + key)
        if version(item['frameworkVersion'])[:2] != version(target)[:2]:
            raise ValueError('Template compatibility mismatch: ' + key)
        version(item['version'])
        templates[key] = {field: item.get(field, '') for field in FIELDS}
    if not templates:
        raise ValueError('No templates found')
    for key, item in templates.items():
        visited = {key}
        cursor = item
        while cursor['parentId']:
            parent_id = cursor['parentId']
            if parent_id in visited or parent_id not in templates:
                raise ValueError('Missing or cyclic template parent: ' + key)
            visited.add(parent_id)
            parent = templates[parent_id]
            if (cursor['parentVersion'], cursor['parentContentHash'], cursor['frameworkVersion']) != (
                    parent['version'], parent['contentHash'], parent['frameworkVersion']):
                raise ValueError('Stale template parent metadata: ' + key)
            cursor = parent
    catalog = read(repo / '.agents/skills/catalog.json')
    ids = [item['id'] for item in catalog['skills']]
    if set(ids) != set(release['aiSkills']['skills']):
        raise ValueError('Skill catalog changed; reconcile release schema manually')
    source_commit = read(pkg / 'AISkills~/bundle.json')['sourceCommit']
    if not re.fullmatch('[0-9a-f]{40}', source_commit):
        raise ValueError('Invalid bundle sourceCommit')
    manifest['dependencies']['com.ember'] = prefix + '#v' + target
    release.update(frameworkVersion=target, frameworkTag='v' + target,
                   manifest=f'manifest-{target}.json', coverage=coverage.strip(),
                   validation=validation.strip(), status=status.strip(),
                   templates=list(templates.values()))
    release['aiSkills'].update(revision='v' + target, bundleSourceCommit=source_commit)
    return {directory / f'release-{target}.json': release,
            directory / f'manifest-{target}.json': manifest}


def emit(outputs, write=False):
    encoded = {path: json.dumps(data, ensure_ascii=False, indent=2) + '\n'
               for path, data in outputs.items()}
    # Check both files before creating either. Existing identical JSON is resumable.
    for path, data in outputs.items():
        if path.exists() and read(path) != data:
            raise ValueError('Refusing to overwrite existing different file: ' + str(path))
    if not write:
        print(json.dumps({str(p): d for p, d in outputs.items()}, ensure_ascii=False, indent=2))
        return
    created = []
    try:
        for path, content in encoded.items():
            if path.exists():
                continue
            with path.open('x', encoding='utf-8', newline='\n') as stream:
                created.append(path)
                stream.write(content)
    except Exception:
        for path in created:
            path.unlink()
        raise
    print('Created or already identical: ' + ', '.join(str(p) for p in outputs))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repo', type=Path, default=Path.cwd())
    for arg in ('baseline', 'version', 'coverage-file', 'validation-file', 'status'):
        parser.add_argument('--' + arg, required=True)
    parser.add_argument('--write', action='store_true')
    args = parser.parse_args()
    try:
        outputs = generate(args.repo, args.baseline, args.version,
                           Path(args.coverage_file).read_text(encoding='utf-8-sig'),
                           Path(args.validation_file).read_text(encoding='utf-8-sig'), args.status)
        emit(outputs, args.write)
    except (ValueError, KeyError, OSError) as error:
        print('Release metadata error: ' + str(error), file=sys.stderr)
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
