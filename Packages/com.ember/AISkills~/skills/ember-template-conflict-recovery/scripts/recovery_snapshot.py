"""Immutable, streamed O/N/L snapshots. Never writes source assets or restores them."""
import argparse
from contextlib import closing
import hashlib
import json
import os
from pathlib import Path
import shutil
import sqlite3
import stat

ROOTS = ('Game', 'Resources', 'Ember/Editor', 'Settings', 'GameResource')
EVIDENCE = ('Assets/Editor/EmberDeployedTemplates.json',
            'ProjectSettings/EmberTemplateBaseline~/template.json')
CHUNK = 1024 * 1024


def safe(path):
    path = Path(os.path.abspath(path))
    for part in (path, *path.parents):
        if part.is_symlink() or (hasattr(part, 'is_junction') and part.is_junction()):
            raise ValueError(f'Linked path refused: {part}')
        if part.exists() and getattr(part.lstat(), 'st_file_attributes', 0) & 0x400:
            raise ValueError(f'Reparse point refused: {part}')
    return path


def digest(path):
    h = hashlib.sha256()
    with safe(path).open('rb') as f:
        for block in iter(lambda: f.read(CHUNK), b''):
            h.update(block)
    return h.hexdigest()


def entries(root, names):
    def walk(path):
        safe(path)
        mode = path.stat().st_mode
        if stat.S_ISREG(mode):
            yield path, 'file'
        elif stat.S_ISDIR(mode):
            yield path, 'directory'
            with os.scandir(path) as children:
                for item in children:
                    yield from walk(Path(item.path))
        else:
            raise ValueError(f'Unsupported filesystem entry: {path}')
    for name in names:
        path = safe(root / name)
        if path.exists():
            yield from walk(path)


def names(side):
    return EVIDENCE if side == 'evidence' else tuple(
        name for root in ROOTS for name in (root, root + '.meta'))


def state(batch, value):
    temp = safe(batch / 'state.tmp')
    temp.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')
    os.replace(temp, safe(batch / 'state.json'))


def connect(batch):
    # Do not create a database accidentally when listing/verifying an invalid batch.
    path = safe(batch / 'inventory.sqlite')
    if not path.is_file():
        raise ValueError('Missing inventory.sqlite')
    return sqlite3.connect(path.as_uri() + '?mode=ro', uri=True)


def verify_tree(db, side, root):
    if not safe(root).is_dir():
        raise ValueError(f'Missing snapshot/source root: {root}')
    count = 0
    for path, kind in entries(root, names(side)):
        rel = path.relative_to(root).as_posix()
        row = db.execute('SELECT kind, sha, size FROM files WHERE side=? AND path=?',
                         (side, rel)).fetchone()
        actual = (kind, digest(path), path.stat().st_size) if kind == 'file' else (kind, '', 0)
        if row != actual:
            raise ValueError(f'Changed or unexpected entry: {side}/{rel}')
        count += 1
    expected = db.execute('SELECT count(*) FROM files WHERE side=?', (side,)).fetchone()[0]
    if count != expected:
        raise ValueError(f'Missing entries: {side}: {count} != {expected}')


def verify(batch, sources=False):
    metadata = json.loads(safe(batch / 'state.json').read_text(encoding='utf-8'))
    if metadata['status'] != 'verified':
        raise ValueError('Incomplete snapshot; create a new batch before deployment')
    with closing(connect(batch)) as db:
        for side, source in metadata['sources'].items():
            verify_tree(db, side, safe(batch / side))
            if sources:
                verify_tree(db, side, safe(source))
    return {'verified': True, 'sources_checked': sources}


def snapshot(args):
    project = safe(args.project)
    sources = {'O': safe(args.old_assets), 'N': safe(args.new_assets),
               'L': safe(project / 'Assets'), 'evidence': project}
    for source in sources.values():
        if not source.is_dir():
            raise ValueError(f'Missing source directory: {source}')
    batch = safe(args.batch)
    # Project-local .utmp is allowed, but never source Assets or baseline trees.
    for source in (sources['O'], sources['N'], sources['L'],
                   project / 'ProjectSettings/EmberTemplateBaseline~'):
        if batch == source or source in batch.parents or batch in source.parents:
            raise ValueError(f'Snapshot overlaps source: {source}')
    batch.mkdir(parents=True, exist_ok=False)
    metadata = {'schema': 1, 'status': 'incomplete', 'project': str(project),
                'sources': {side: str(path) for side, path in sources.items()}}
    state(batch, metadata)
    with closing(sqlite3.connect(batch / 'inventory.sqlite')) as db:
        db.execute('CREATE TABLE files (side TEXT, path TEXT, kind TEXT, sha TEXT, '
                   'size INTEGER, PRIMARY KEY(side,path))')
        required = 0
        for side, source in sources.items():
            for path, kind in entries(source, names(side)):
                size = path.stat().st_size if kind == 'file' else 0
                required += size
                db.execute('INSERT INTO files VALUES (?,?,?,?,?)',
                           (side, path.relative_to(source).as_posix(), kind, '', size))
            db.commit()
        if shutil.disk_usage(batch).free < required + max(required // 20, 16 * CHUNK):
            raise ValueError(f'Insufficient space for snapshot: requires at least {required} bytes plus margin')
        copied = 0
        for side, source in sources.items():
            (batch / side).mkdir()
            for rel, kind, size in db.execute(
                    'SELECT path,kind,size FROM files WHERE side=? ORDER BY path', (side,)):
                src = safe(source / rel)
                dest = safe(batch / side / rel)
                if kind == 'directory':
                    dest.mkdir(parents=True, exist_ok=True)
                    continue
                dest.parent.mkdir(parents=True, exist_ok=True)
                h = hashlib.sha256()
                with src.open('rb') as fin, dest.open('xb') as fout:
                    for block in iter(lambda: fin.read(CHUNK), b''):
                        h.update(block)
                        fout.write(block)
                sha = h.hexdigest()
                if dest.stat().st_size != size or digest(dest) != sha or digest(src) != sha:
                    raise ValueError(f'Copy verification failed: {side}/{rel}')
                db.execute('UPDATE files SET sha=? WHERE side=? AND path=?', (sha, side, rel))
                copied += 1
                if copied % 200 == 0:
                    db.commit()
                    print(json.dumps({'copied_files': copied}), flush=True)
            db.commit()
        for side, source in sources.items():
            verify_tree(db, side, source)
            verify_tree(db, side, batch / side)
        metadata.update(status='verified', bytes=required, copied_files=copied)
        state(batch, metadata)
    return {'batch': str(batch), 'status': 'verified', 'bytes': required, 'copied_files': copied}


CLASSIFY = '''WITH paths AS (SELECT DISTINCT path FROM files WHERE side IN ('O','N','L')),
values_by_path AS (
 SELECT p.path,
 coalesce(o.kind || ':' || o.sha, 'missing') AS old,
 coalesce(n.kind || ':' || n.sha, 'missing') AS new,
 coalesce(l.kind || ':' || l.sha, 'missing') AS local
 FROM paths p
 LEFT JOIN files o ON o.path=p.path AND o.side='O'
 LEFT JOIN files n ON n.path=p.path AND n.side='N'
 LEFT JOIN files l ON l.path=p.path AND l.side='L'),
classified AS (SELECT *, CASE WHEN local=new THEN 'same'
 WHEN local=old THEN 'template' WHEN new=old THEN 'local'
 ELSE 'review' END AS category FROM values_by_path)
'''


def listing(args):
    batch = safe(args.batch)
    metadata = json.loads(safe(batch / 'state.json').read_text(encoding='utf-8'))
    if metadata['status'] != 'verified':
        raise ValueError('Incomplete snapshot')
    with closing(connect(batch)) as db:
        counts = dict(db.execute(CLASSIFY + 'SELECT category,count(*) FROM classified GROUP BY category'))
        db.row_factory = sqlite3.Row
        rows = db.execute(CLASSIFY + 'SELECT * FROM classified '
                          'WHERE (? IS NULL OR category=?) ORDER BY path LIMIT ? OFFSET ?',
                          (args.category, args.category, args.limit, args.offset))
        return {'counts_by_path_not_resource_unit': counts,
                'offset': args.offset, 'items': [dict(row) for row in rows]}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest='command', required=True)
    make = commands.add_parser('snapshot')
    for option in ('project', 'old-assets', 'new-assets', 'batch'):
        make.add_argument('--' + option, required=True)
    check = commands.add_parser('verify')
    check.add_argument('--batch', required=True)
    check.add_argument('--sources', action='store_true')
    page = commands.add_parser('list')
    page.add_argument('--batch', required=True)
    page.add_argument('--category', choices=('same', 'template', 'local', 'review'))
    page.add_argument('--offset', type=int, default=0)
    page.add_argument('--limit', type=int, default=20)
    args = parser.parse_args()
    if args.command == 'list' and (args.offset < 0 or not 1 <= args.limit <= 200):
        parser.error('offset must be nonnegative; limit must be 1..200')
    try:
        result = snapshot(args) if args.command == 'snapshot' else (
            verify(safe(args.batch), args.sources) if args.command == 'verify' else listing(args))
        print(json.dumps(result, ensure_ascii=False, indent=2))
    except (OSError, ValueError, KeyError, sqlite3.Error) as error:
        parser.exit(1, f'{error}\n')


if __name__ == '__main__':
    main()
