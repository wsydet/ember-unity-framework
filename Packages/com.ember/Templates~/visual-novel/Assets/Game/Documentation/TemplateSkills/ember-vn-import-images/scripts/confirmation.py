"""Local review server. Never writes assets/tables or calls Unity. Requires Pillow."""
import argparse
import csv
import hashlib
import io
import json
import os
from pathlib import Path
import re
import secrets
import sys
from http.server import BaseHTTPRequestHandler, HTTPServer
from PIL import Image

TABLES = {
    'characters': ['id', 'displayName'],
    'portraits': ['id', 'characterId', 'expression', 'resourcePath'],
    'backgrounds': ['id', 'resourcePath'],
}
RESOURCE_ROOT = 'Assets/GameResource/Resources/'
ATLAS = 'UI/Module/Narrative/Atlas/'
EXTENSIONS = {'.png', '.jpg', '.jpeg', '.webp', '.bmp', '.tga', '.tif', '.tiff'}


def digest(path):
    if not path.exists():
        return None
    return hashlib.sha256(path.read_bytes()).hexdigest()


def fingerprint(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, ensure_ascii=False).encode()).hexdigest()


def no_links(path):
    path = Path(os.path.abspath(path))
    for part in [path, *path.parents]:
        if part.is_symlink() or (hasattr(part, 'is_junction') and part.is_junction()):
            raise ValueError('不允许链接路径: ' + str(part))
    return path


def segment(value):
    value = str(value).strip()
    if (not value or len(value) > 100 or re.search(r'[<>:"/\\|?*\x00-\x1f]', value)
            or value.endswith(('.', ' ')) or value in {'.', '..'}
            or re.match(r'^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)', value, re.I)):
        raise ValueError('请填写合法名称/ID: ' + value)
    return value


def write_json(path, value):
    temporary = path.with_suffix('.tmp')
    temporary.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')
    os.replace(temporary, path)


class Batch:
    def __init__(self, project, source, work, table_paths=None, ready=False, suggestions=None):
        self.project, self.source, self.work = map(no_links, (project, source, work))
        if not self.work.is_relative_to(self.project / '.utmp/vn-image-import'):
            raise ValueError('批次目录必须位于项目 .utmp/vn-image-import 下')
        self.tables = {key: no_links(self.project / (table_paths or {}).get(
            key, f'Assets/GameResource/TableSources/novel_{key}.etable.csv')) for key in TABLES}
        for path in self.tables.values():
            if not path.is_relative_to(self.project):
                raise ValueError('表路径必须在项目内')
        self.ready = ready
        self.rows = {}
        for key, path in self.tables.items():
            with path.open(encoding='utf-8-sig', newline='') as stream:
                reader = csv.DictReader(stream)
                if reader.fieldnames != TABLES[key]:
                    raise ValueError(f'{path}: 表结构不匹配，需适配映射后重试')
                self.rows[key] = list(reader)
                ids = [row['id'].casefold() for row in self.rows[key]]
                if len(ids) != len(set(ids)):
                    raise ValueError(f'{path}: 存在重复 ID')
        self.files = self.scan()
        if not self.files:
            raise ValueError('指定范围没有支持的图片')
        self.baseline = self.snapshot()
        self.nonce = secrets.token_hex(16)
        self.batch_id = fingerprint({'baseline': self.baseline, 'nonce': self.nonce})
        self.state = 'pending'
        self.token = secrets.token_urlsafe(32)
        self.work.mkdir(parents=True, exist_ok=False)
        write_json(self.work / 'batch.json', {
            'project': str(self.project), 'source': str(self.source),
            'tables': {k: str(v) for k, v in self.tables.items()},
            'baseline': self.baseline, 'nonce': self.nonce, 'batchId': self.batch_id})
        if suggestions:
            write_json(self.work / 'draft.json', {'status': 'draft', 'batchId': self.batch_id,
                                                 'choices': suggestions})

    def scan(self):
        files = [self.source] if self.source.is_file() else sorted(self.source.rglob('*'))
        return [no_links(p) for p in files if p.is_file() and p.suffix.lower() in EXTENSIONS]

    def snapshot(self):
        # Include complete resource namespace, definitions and source tables, so collisions,
        # existing references and mapping edits invalidate review even before submission.
        paths = self.scan() + list(self.tables.values())
        roots = [self.project / 'Assets/Game/Table/Definitions']
        roots += [p for p in (self.project / 'Assets').rglob('*')
                  if p.is_dir() and p.name.casefold() == 'resources']
        for root in roots:
            paths += [no_links(p) for p in root.rglob('*') if p.is_file()]
        return {str(p): digest(p) for p in sorted(set(paths))}

    def check(self):
        if self.snapshot() != self.baseline:
            raise ValueError('源文件、资源或配表指纹已变化；请创建新批次重新核对，旧确认无效')

    def manifest(self):
        images = []
        for index, path in enumerate(self.files):
            with Image.open(path) as im:
                alpha = im.convert('RGBA').getchannel('A').getextrema()[0] < 255
                images.append({'index': index, 'name': path.name, 'source': str(path),
                               'width': im.width, 'height': im.height, 'alpha': alpha,
                               'sha256': self.baseline[str(path)]})
        for item in images:
            item['duplicates'] = [x['name'] for x in images if x['index'] != item['index']
                                  and x['sha256'] == item['sha256']]
        draft_path = self.work / 'draft.json'
        draft = json.loads(draft_path.read_text(encoding='utf-8')) if draft_path.exists() else None
        return {'batchId': self.batch_id, 'ready': self.ready, 'images': images,
                'characters': self.rows['characters'], 'state': self.state,
                'draft': draft['choices'] if draft and draft['batchId'] == self.batch_id else None}

    def plan(self, choices):
        if len(choices) != len(self.files) or [c.get('index') for c in choices] != list(range(len(self.files))):
            raise ValueError('必须逐图提交本批完整选择，不能遗漏或重复图片')
        result, targets, keys, new_characters = [], set(), set(), {}
        existing_characters = {r['id']: r for r in self.rows['characters']}
        for choice, source in zip(choices, self.files):
            kind = choice.get('kind', 'skip')
            if kind not in {'portrait', 'background', 'other', 'skip'}:
                raise ValueError('未知用途')
            item = {'index': choice['index'], 'source': str(source), 'kind': kind}
            if kind == 'skip':
                result.append(item)
                continue
            story = segment(choice.get('story', ''))
            variant = segment(choice.get('variant', '') or 'default')
            updates = []
            if kind == 'portrait':
                character = segment(choice.get('characterId', ''))
                display = choice.get('displayName', '').strip()
                if character in existing_characters:
                    display = existing_characters[character]['displayName']
                else:
                    if not display:
                        raise ValueError('新角色必须填写显示名')
                    if character in new_characters and new_characters[character] != display:
                        raise ValueError('同一新角色 ID 的显示名不一致')
                    if any(c != character and c.casefold() == character.casefold() for c in new_characters):
                        raise ValueError('本批新角色 ID 大小写冲突')
                    if character.casefold() in {x.casefold() for x in existing_characters}:
                        raise ValueError('角色 ID 大小写冲突')
                    new_characters[character] = display
                    updates.append({'table': str(self.tables['characters']), 'action': 'insert-or-reuse',
                                    'row': {'id': character, 'displayName': display}})
                base, folder, table = character, 'Portraits', 'portraits'
            elif kind == 'background':
                base, folder, table = segment(choice.get('scene', '')), 'Backgrounds', 'backgrounds'
            else:
                subtype = choice.get('otherUse', '')
                if subtype not in {'TitleUI', 'CG', 'Icon', 'Mask', 'Other'}:
                    raise ValueError('请选择其他用途类别')
                base, folder, table = segment(choice.get('scene', '') or source.stem), subtype, None
            resource_id = segment(choice.get('resourceId', '') or f'{base}_{variant}')
            resource = f'{ATLAS}{story}/{folder}/{resource_id}'
            target = no_links(self.project / f'{RESOURCE_ROOT}{resource}{source.suffix.lower()}')
            if resource.casefold() in targets:
                raise ValueError('本批目标 Resources 路径重复，请修改 ID 或跳过重复图')
            targets.add(resource.casefold())
            conflicts = []
            for raw, sha in self.baseline.items():
                path = Path(raw)
                relative = path.relative_to(self.project / 'Assets') if path.is_relative_to(self.project / 'Assets') else None
                parts = relative.parts if relative else ()
                resource_indexes = [i for i, part in enumerate(parts[:-1]) if part.casefold() == 'resources']
                if resource_indexes and path.suffix != '.meta':
                    key = Path(*parts[resource_indexes[-1] + 1:]).with_suffix('').as_posix()
                    if key.casefold() == resource.casefold():
                        conflicts.append({'path': raw, 'sha256': sha})
            policy = choice.get('conflict', 'block')
            if policy not in {'block', 'reuse'}:
                raise ValueError('本版本只支持阻止冲突或同内容复用；替换请另起经审查批次')
            if conflicts and not (policy == 'reuse' and len(conflicts) == 1
                                  and conflicts[0]['path'] == str(target)
                                  and conflicts[0]['sha256'] == self.baseline[str(source)]):
                raise ValueError('目标冲突：请更换资源 ID、同内容复用或跳过: ' + str(target))
            if table:
                unique = (table, resource_id.casefold())
                if unique in keys:
                    raise ValueError('本批表 ID 重复')
                keys.add(unique)
                row = {'id': resource_id, 'resourcePath': resource}
                if kind == 'portrait':
                    row.update(characterId=character, expression=variant)
                old = next((r for r in self.rows[table] if r['id'].casefold() == resource_id.casefold()), None)
                if old and old != row:
                    raise ValueError('表 ID 已有不同绑定，请更换资源 ID 或跳过: ' + resource_id)
                updates.append({'table': str(self.tables[table]), 'action': 'reuse' if old else 'insert',
                                'row': row, 'previous': old})
            references = [{'table': str(self.tables[k]), 'row': row} for k in ('portraits', 'backgrounds')
                          for row in self.rows[k] if row['resourcePath'].casefold() == resource.casefold()]
            item.update(story=story, resourceId=resource_id, target=str(target), resourcesPath=resource,
                        tableRows=updates, conflictPolicy=policy, existingFiles=conflicts,
                        existingReferences=references, assetAction='reuse' if conflicts else 'copy',
                        note='仅复制资源，不自动接入 UI/CG 播放或新增配表' if kind == 'other' else '')
            result.append(item)
        return result

    def submit(self, action, payload):
        if self.state != 'pending':
            raise ValueError('本批已结束；请新建批次，不能重复提交或撤销已经提交的决定')
        if action == 'cancel':
            self.state = 'cancelled'
            write_json(self.work / 'decision.json', {'status': self.state, 'batchId': self.batch_id})
            return {'status': self.state}
        self.check()
        choices = payload.get('choices', [])
        if action == 'draft':
            write_json(self.work / 'draft.json', {'status': 'draft', 'batchId': self.batch_id, 'choices': choices})
            return {'status': 'draft'}
        plan = self.plan(choices)
        review_hash = fingerprint({'batchId': self.batch_id, 'choices': choices, 'plan': plan})
        if action == 'preview':
            return {'plan': plan, 'reviewHash': review_hash}
        if action != 'confirm' or not self.ready:
            raise ValueError('Unity MCP 未验证，实际导入尚不可执行；只能保存草稿')
        if payload.get('reviewHash') != review_hash:
            raise ValueError('选择已改变，请重新预览完整计划再确认')
        decision = {'status': 'confirmed', 'batchId': self.batch_id, 'choices': choices,
                    'plan': plan, 'reviewHash': review_hash}
        write_json(self.work / 'decision.json', decision)
        self.state = 'confirmed'
        return {'status': self.state, 'path': str(self.work / 'decision.json')}


def verify(work):
    work = no_links(work)
    batch = json.loads((work / 'batch.json').read_text(encoding='utf-8'))
    decision = json.loads((work / 'decision.json').read_text(encoding='utf-8'))
    if decision['status'] != 'confirmed' or decision['batchId'] != batch['batchId']:
        raise ValueError('没有本批明确确认；禁止导入')
    obj = Batch.__new__(Batch)
    obj.project, obj.source = no_links(batch['project']), no_links(batch['source'])
    obj.tables = {k: no_links(v) for k, v in batch['tables'].items()}
    obj.baseline = batch['baseline']
    obj.check()
    expected = fingerprint({'batchId': batch['batchId'], 'choices': decision['choices'], 'plan': decision['plan']})
    if fingerprint({'baseline': batch['baseline'], 'nonce': batch['nonce']}) != batch['batchId'] or expected != decision['reviewHash']:
        raise ValueError('回执内容不一致，禁止导入')
    return decision


def serve(batch, port):
    class Handler(BaseHTTPRequestHandler):
        def log_message(self, *args):
            pass

        def reply(self, value, code=200, mime='application/json; charset=utf-8'):
            raw = value if isinstance(value, bytes) else json.dumps(value, ensure_ascii=False).encode()
            self.send_response(code)
            self.send_header('Content-Type', mime)
            self.send_header('Cache-Control', 'no-store')
            self.send_header('X-Content-Type-Options', 'nosniff')
            self.end_headers()
            self.wfile.write(raw)

        def do_GET(self):
            try:
                prefix = '/' + batch.token
                if self.path == prefix + '/':
                    self.reply((Path(__file__).parent.parent / 'assets/confirmation.html').read_bytes(),
                               mime='text/html; charset=utf-8')
                elif self.path == prefix + '/manifest':
                    self.reply(batch.manifest())
                elif self.path.startswith(prefix + '/image/'):
                    index = int(self.path.rsplit('/', 1)[1])
                    if index < 0 or index >= len(batch.files):
                        raise ValueError('图片索引无效')
                    with Image.open(batch.files[index]) as im:
                        im.thumbnail((512, 512))
                        stream = io.BytesIO()
                        im.convert('RGBA').save(stream, format='PNG')
                    self.reply(stream.getvalue(), mime='image/png')
                else:
                    self.reply({'error': 'not found'}, 404)
            except (ValueError, OSError) as exc:
                self.reply({'error': str(exc)}, 400)

        def do_POST(self):
            try:
                origin = self.headers.get('Origin')
                if origin != f'http://127.0.0.1:{self.server.server_port}':
                    raise ValueError('只接受本机确认页面提交')
                if not self.path.startswith('/' + batch.token + '/'):
                    raise ValueError('批次令牌无效')
                length = int(self.headers.get('Content-Length', '0'))
                if not 0 < length <= 2_000_000:
                    raise ValueError('请求大小无效')
                payload = json.loads(self.rfile.read(length))
                self.reply(batch.submit(self.path.rsplit('/', 1)[1], payload))
            except (ValueError, OSError, KeyError, TypeError) as exc:
                self.reply({'error': str(exc)}, 400)

    server = HTTPServer(('127.0.0.1', port), Handler)
    url = f'http://127.0.0.1:{server.server_port}/{batch.token}/'
    write_json(batch.work / 'session.json', {'url': url, 'pid': os.getpid()})
    print(json.dumps({'url': url, 'work': str(batch.work)}, ensure_ascii=False), flush=True)
    server.serve_forever()


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='command', required=True)
    start = sub.add_parser('serve')
    for name in ('project', 'source', 'work'):
        start.add_argument('--' + name, required=True)
    start.add_argument('--tables', help='JSON file mapping characters/portraits/backgrounds to actual CSV paths')
    start.add_argument('--mcp-verified', action='store_true', help='Only after live MCP identity and mapping checks')
    start.add_argument('--suggestions', help='JSON choices array; suggestions are drafts, never approval')
    start.add_argument('--port', type=int, default=0)
    check = sub.add_parser('verify')
    check.add_argument('--work', required=True)
    args = parser.parse_args()
    if args.command == 'verify':
        print(json.dumps(verify(args.work), ensure_ascii=False, indent=2))
    else:
        mapping = json.loads(Path(args.tables).read_text(encoding='utf-8')) if args.tables else None
        suggestions = json.loads(Path(args.suggestions).read_text(encoding='utf-8')) if args.suggestions else None
        serve(Batch(args.project, args.source, args.work, mapping, args.mcp_verified, suggestions), args.port)
