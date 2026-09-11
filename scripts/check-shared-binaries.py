#!/usr/bin/env python3
"""Check release bytes without Unity or third-party Python dependencies.

python scripts/check-shared-binaries.py --revision HEAD --checkout
python scripts/check-shared-binaries.py --revision v0.12.3 --package-root <resolved-UPM-package>
Use --revision : to check the staged index before committing. Never regenerate
the inventory from an unverified checkout: hashes describe validated originals.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import struct
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = 'Packages/com.ember'
SHARED = PACKAGE + '/SharedAssets/'
INVENTORY = ROOT / 'scripts/shared-binaries.json'


def git(*args, env=None, data=None):
    return subprocess.check_output(['git', *args], cwd=ROOT, env=env, input=data)


def require(condition, message):
    if not condition:
        raise ValueError(message)


def checksum(data):
    data += b'\0' * (-len(data) % 4)
    return sum(struct.unpack('>' + 'I' * (len(data) // 4), data)) & 0xffffffff


def validate_ttf(data):
    """Validate sfnt directory, all table checksums, loca and Unicode cmap."""
    require(data[:4] == b'\0\1\0\0', 'Expected TrueType sfnt')
    count = struct.unpack_from('>H', data, 4)[0]
    tables = {}
    for i in range(count):
        tag, expected, offset, size = struct.unpack_from('>4sIII', data, 12 + 16*i)
        require(offset % 4 == 0 and offset + size <= len(data), f'Invalid table bounds: {tag}')
        require(tag not in tables, f'Duplicate table: {tag}')
        table = data[offset:offset+size]
        tables[tag] = table
        if tag == b'head':
            table = table[:8] + b'\0'*4 + table[12:]
        require(checksum(table) == expected, f'Table checksum mismatch: {tag}')
    require(checksum(data) == 0xB1B0AFBA, 'Font checksumAdjustment mismatch')
    glyphs = struct.unpack_from('>H', tables[b'maxp'], 4)[0]
    long_loca = struct.unpack_from('>h', tables[b'head'], 50)[0]
    require(long_loca in (0, 1), 'Invalid indexToLocFormat')
    offsets = struct.unpack('>' + ('I' if long_loca else 'H')*(glyphs+1), tables[b'loca'])
    if not long_loca:
        offsets = tuple(x*2 for x in offsets)
    require(list(offsets) == sorted(offsets) and offsets[-1] <= len(tables[b'glyf']), 'Invalid glyph offsets')
    cmap = tables[b'cmap']
    mapped = set()
    for i in range(struct.unpack_from('>H', cmap, 2)[0]):
        platform, encoding, off = struct.unpack_from('>HHI', cmap, 4+8*i)
        if platform != 0 and not (platform == 3 and encoding in (1,10)):
            continue
        fmt = struct.unpack_from('>H', cmap, off)[0]
        if fmt == 4:
            segs = struct.unpack_from('>H', cmap, off+6)[0]//2
            for seg in range(segs):
                end = struct.unpack_from('>H', cmap, off+14+2*seg)[0]
                start = struct.unpack_from('>H', cmap, off+16+2*segs+2*seg)[0]
                delta = struct.unpack_from('>h', cmap, off+16+4*segs+2*seg)[0]
                pos = off+16+6*segs+2*seg
                ro = struct.unpack_from('>H', cmap, pos)[0]
                for cp in range(start, end+1):
                    gid = struct.unpack_from('>H', cmap, pos+ro+2*(cp-start))[0] if ro else cp
                    gid = (gid+delta)&0xffff if gid or not ro else 0
                    require(gid < glyphs, 'cmap glyph out of bounds')
                    if gid: mapped.add(cp)
        elif fmt == 12:
            for group in range(struct.unpack_from('>I', cmap, off+12)[0]):
                start, end, first = struct.unpack_from('>III', cmap, off+16+12*group)
                require(start <= end <= 0x10ffff and first+end-start < glyphs, 'Invalid cmap group')
                mapped.update(range(start if first else start+1, end+1))
    required = set(map(ord, '主控中心无人机待命小麦胡萝卜成熟锁定田解锁水井不消耗水'))
    require(required <= mapped, 'Missing required Chinese codepoints: ' + str(sorted(required-mapped)))


def verify(data, entry, source):
    require(len(data) == entry['length'], f'{source}: length {len(data)} != {entry["length"]}')
    actual = hashlib.sha256(data).hexdigest().upper()
    require(actual == entry['sha256'], f'{source}: SHA256 {actual}')
    require(not data.startswith(b'version https://git-lfs.github.com/spec/'), f'{source}: LFS pointer')
    if entry['path'].endswith('.ttf'):
        validate_ttf(data)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--revision', default='HEAD')
    parser.add_argument('--checkout', action='store_true', help='Verify clean Git checkout with core.autocrlf=false and true')
    parser.add_argument('--package-root', type=Path, help='Read-only check of the actual resolved consumer package')
    args = parser.parse_args()
    entries = json.loads(INVENTORY.read_text(encoding='utf-8'))['files']
    paths = [PACKAGE+'/'+entry['path'] for entry in entries]
    require(len(paths) == len(set(paths)), 'Duplicate inventory paths')
    revision = args.revision
    all_paths = (git('ls-files', '-z', '--', SHARED) if revision == ':' else
                 git('ls-tree', '-r', '--name-only', '-z', revision, '--', SHARED)).decode('utf-8').split('\0')
    binaries = []
    for path in filter(None, all_paths):
        data = git('show', ':'+path if revision == ':' else revision+':'+path)
        # Text assets must really be UTF-8 without NULs; unknown suffixes need an inventory entry.
        is_text = Path(path).suffix in ('.txt', '.meta', '.asset') and b'\0' not in data
        if is_text:
            try: data.decode('utf-8-sig')
            except UnicodeDecodeError: is_text = False
        if not is_text: binaries.append(path)
    require(set(binaries) == set(paths), f'Binary inventory mismatch: {set(binaries)^set(paths)}')
    with tempfile.TemporaryDirectory(prefix='ember-binary-check-') as tmp:
        env = dict(os.environ, GIT_INDEX_FILE=str(Path(tmp)/'index'))
        if revision == ':':
            index = Path(git('rev-parse', '--git-path', 'index').decode().strip())
            shutil.copyfile(index if index.is_absolute() else ROOT/index, env['GIT_INDEX_FILE'])
        else:
            git('read-tree', revision, env=env)
        for entry, path in zip(entries, paths):
            for cached, attr_env in [(False,None),(True,env)]:
                cmd = ['check-attr', '-z'] + (['--cached'] if cached else [])
                attrs = git(*cmd, 'text','filter','diff','merge','--',path,env=attr_env).decode().split('\0')
                require(all(attrs[i] == 'unset' for i in range(2,len(attrs)-1,3)), f'{path}: unsafe attributes {attrs}')
            verify((ROOT/path).read_bytes(), entry, 'workspace '+path)
            verify(git('show', ':'+path if revision == ':' else revision+':'+path), entry, 'Git blob '+path)
            if args.package_root:
                verify((args.package_root/entry['path']).read_bytes(), entry, 'consumer '+path)
            print(f'PASS {path}: {entry["length"]} bytes SHA256 {entry["sha256"]}')
        if args.checkout:
            checkout_paths = ['.gitattributes'] + [p for p in all_paths if p]
            for autocrlf in ('false','true'):
                dest = Path(tmp)/autocrlf
                dest.mkdir()
                git('-c','core.autocrlf='+autocrlf,'checkout-index','--force',
                    '--prefix='+dest.as_posix()+'/', '-z','--stdin',env=env,
                    data=('\0'.join(checkout_paths)+'\0').encode('utf-8'))
                for entry,path in zip(entries,paths):
                    verify((dest/path).read_bytes(),entry,'clean checkout '+autocrlf+' '+path)
                print('PASS clean checkout core.autocrlf='+autocrlf)
    print('Static integrity passed; this does not validate Unity compilation, import or runtime rendering.')


if __name__ == '__main__':
    main()
