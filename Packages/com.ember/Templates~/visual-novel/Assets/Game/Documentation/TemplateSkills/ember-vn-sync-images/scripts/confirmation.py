"""One review for mixed image imports and replacements; never writes business assets."""
import argparse
import importlib.util
import io
import json
import os
from pathlib import Path
import secrets
from http.server import BaseHTTPRequestHandler, HTTPServer

ROOT = Path(__file__).resolve().parents[2]


def load(name):
    path = ROOT / name / 'scripts/confirmation.py'
    spec = importlib.util.spec_from_file_location(name.replace('-', '_'), path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


imp = load('ember-vn-import-images')
rep = load('ember-vn-replace-images')


class Batch:
    def __init__(self, project, source, analysis, work, ready=False, tables=None):
        self.project, self.source, self.analysis, self.work = map(rep.path_of, (project, source, analysis, work))
        if not self.work.is_relative_to(self.project / '.utmp/vn-image-sync'):
            raise ValueError('批次必须位于项目 .utmp/vn-image-sync 下')
        if self.work.exists():
            raise ValueError('批次已存在')
        self.ready, self.tables = ready, tables
        self.batch_id, self.token, self.state = secrets.token_hex(16), secrets.token_urlsafe(32), 'pending'
        self.prepare()
        self.baseline = self.snapshot()
        self.work.mkdir(parents=True)
        rep.write(self.work / 'batch.json', dict(project=str(self.project), source=str(self.source),
                  analysis=str(self.analysis), tables=self.tables, ready=ready,
                  batchId=self.batch_id, baseline=self.baseline))

    def prepare(self):
        if not (self.project / 'Assets').is_dir() or not self.source.exists():
            raise ValueError('缺少实际 Unity 项目或源路径')
        files = [self.source] if self.source.is_file() else self.source.rglob('*')
        self.files = sorted(rep.path_of(p) for p in files if p.is_file() and p.suffix.lower() in imp.EXTENSIONS)
        raw = rep.read(self.analysis)['items']
        by_source = {str(rep.path_of(self.project / x['source'])): x for x in raw}
        if not self.files or len(by_source) != len(raw) or set(by_source) != {str(p) for p in self.files}:
            raise ValueError('分析必须与指定源范围逐图一一对应，不能遗漏或重复')
        self.raw = [by_source[str(p)] for p in self.files]
        # Reuse lower-level planning without creating their individual confirmation sessions.
        self.importer = imp.Batch.__new__(imp.Batch)
        child = self.importer
        child.project, child.source, child.files = self.project, self.source, self.files
        child.tables = imp.resolve_table_paths(self.project, self.tables)
        child.rows = {}
        for key, path in child.tables.items():
            with path.open(encoding='utf-8-sig', newline='') as stream:
                reader = imp.csv.DictReader(stream, delimiter='\t' if path.suffix.lower() == '.tsv' else ',')
                if reader.fieldnames != imp.TABLES[key]:
                    raise ValueError('需适配实际表结构: ' + str(path))
                child.rows[key] = list(reader)
                keys = [r['id'].casefold() for r in child.rows[key]]
                if len(keys) != len(set(keys)):
                    raise ValueError('表 ID 重复')
        child.baseline = child.snapshot()
        self.replacer = rep.Batch.__new__(rep.Batch)
        self.replacer.project, self.replacer.analysis = self.project, self.analysis
        built = self.replacer.build_items()
        mapped = {x['source']: x for x in built}
        self.replacer.items = [mapped[str(p)] for p in self.files]

    def snapshot(self):
        paths = set(self.importer.snapshot())
        paths.update(str(rep.path_of(p)) for p in (self.project / 'Assets').rglob('*') if p.is_file())
        paths.add(str(self.analysis))
        paths.update(str(ROOT / name / 'scripts/confirmation.py') for name in
                     ('ember-vn-import-images', 'ember-vn-replace-images', 'ember-vn-sync-images'))
        return {p: imp.digest(Path(p)) for p in sorted(paths)}

    def check(self):
        if self.snapshot() != self.baseline:
            raise ValueError('源图、分析、项目或依赖已变化，需新批次重新核对')

    def plan(self, choices):
        if len(choices) != len(self.files) or [c.get('index') for c in choices] != list(range(len(self.files))):
            raise ValueError('必须逐图提交完整选择')
        imports, replacements = [], []
        for index, c in enumerate(choices):
            if c.get('route') not in ('import', 'replace', 'skip'):
                raise ValueError('未知分类')
            imports.append(dict(c, kind=c.get('kind', 'skip')) if c['route'] == 'import'
                           else dict(index=index, kind='skip'))
            if c['route'] == 'import' and c.get('kind') not in ('portrait', 'background', 'other'):
                raise ValueError('新增图片需选择用途')
            if c['route'] == 'replace' and c.get('candidate') is None:
                raise ValueError('替换图片需选择旧图')
            replacements.append(c if c['route'] == 'replace' else dict(candidate=None))
        a, b = self.importer.plan(imports), self.replacer.plan(replacements)
        targets = {x['target'].casefold() for x in a if 'target' in x}
        if targets & {x['target'].casefold() for x in b if 'target' in x}:
            raise ValueError('新增与替换目标冲突，请调整分类或资源 ID')
        return dict(imports=a, replacements=b)

    def manifest(self):
        draft = self.work / 'draft.json'
        choices = rep.read(draft)['choices'] if draft.exists() else [
            dict(x.get('suggestion', {}), index=i, acknowledged=False,
                 route=x.get('suggestion', {}).get('route', 'skip')) for i, x in enumerate(self.raw)]
        return dict(items=self.replacer.items, choices=choices, ready=self.ready,
                    state=self.state, characters=self.importer.rows['characters'])

    def submit(self, action, payload):
        if self.state != 'pending':
            raise ValueError('本批已结束')
        if action == 'cancel':
            self.state = 'cancelled'
            rep.write(self.work / 'decision.json', dict(status=self.state, batchId=self.batch_id))
            return dict(status=self.state)
        self.check()
        choices = payload.get('choices', [])
        if action == 'draft':
            rep.write(self.work / 'draft.json', dict(choices=choices))
            return dict(status='draft')
        plan = self.plan(choices)
        review = imp.fingerprint(dict(batchId=self.batch_id, choices=choices, plan=plan))
        if action == 'preview':
            return dict(plan=plan, reviewHash=review)
        if action != 'confirm' or not self.ready or payload.get('reviewHash') != review:
            raise ValueError('需通过 MCP 检查并重新预览后确认')
        decision = dict(status='confirmed', batchId=self.batch_id, choices=choices, plan=plan, reviewHash=review)
        rep.write(self.work / 'decision.json', decision)
        self.state = 'confirmed'
        return dict(status=self.state)


def verify(work):
    work = rep.path_of(work)
    data, decision = rep.read(work / 'batch.json'), rep.read(work / 'decision.json')
    if not data['ready'] or decision['status'] != 'confirmed' or decision['batchId'] != data['batchId']:
        raise ValueError('没有本批有效确认')
    batch = Batch.__new__(Batch)
    batch.project, batch.source, batch.analysis = map(rep.path_of, (data['project'], data['source'], data['analysis']))
    batch.tables, batch.baseline = data['tables'], data['baseline']
    batch.prepare()
    batch.check()
    if batch.plan(decision['choices']) != decision['plan']:
        raise ValueError('计划与回执不符')
    if imp.fingerprint({k: decision[k] for k in ('batchId', 'choices', 'plan')}) != decision['reviewHash']:
        raise ValueError('确认指纹不符')
    return decision


def serve(batch):
    class Handler(BaseHTTPRequestHandler):
        def log_message(self, *args):
            pass

        def reply(self, value, mime='application/json; charset=utf-8', code=200):
            raw = value if isinstance(value, bytes) else json.dumps(value, ensure_ascii=False).encode()
            self.send_response(code)
            self.send_header('Content-Type', mime)
            self.send_header('Cache-Control', 'no-store')
            self.send_header('X-Content-Type-Options', 'nosniff')
            self.end_headers()
            self.wfile.write(raw)

        def do_GET(self):
            try:
                prefix = '/' + batch.token + '/'
                if self.path == prefix:
                    self.reply((Path(__file__).parents[1] / 'assets/confirmation.html').read_bytes(), 'text/html; charset=utf-8')
                elif self.path == prefix + 'manifest':
                    self.reply(batch.manifest())
                elif self.path.startswith(prefix + 'image/'):
                    indexes = [int(x) for x in self.path[len(prefix + 'image/'):].split('/')]
                    if len(indexes) not in (1, 2) or not 0 <= indexes[0] < len(batch.files):
                        raise ValueError('图片索引无效')
                    item = batch.replacer.items[indexes[0]]
                    path = item['source']
                    if len(indexes) == 2:
                        if not 0 <= indexes[1] < len(item['candidates']):
                            raise ValueError('候选索引无效')
                        path = item['candidates'][indexes[1]]['target']
                    with rep.Image.open(path) as im:
                        im.thumbnail((768, 768))
                        stream = io.BytesIO()
                        im.convert('RGBA').save(stream, format='PNG')
                    self.reply(stream.getvalue(), 'image/png')
                else:
                    self.reply(dict(error='not found'), code=404)
            except (ValueError, OSError, KeyError, TypeError, IndexError) as exc:
                self.reply(dict(error=str(exc)), code=400)

        def do_POST(self):
            try:
                if self.headers.get('Origin') != f'http://127.0.0.1:{self.server.server_port}':
                    raise ValueError('只接受本地页面提交')
                if not self.path.startswith('/' + batch.token + '/'):
                    raise ValueError('批次令牌无效')
                length = int(self.headers.get('Content-Length', 0))
                if not 0 < length <= 2_000_000:
                    raise ValueError('请求大小无效')
                self.reply(batch.submit(self.path.rsplit('/', 1)[1], json.loads(self.rfile.read(length))))
            except (ValueError, OSError, KeyError, TypeError, IndexError) as exc:
                self.reply(dict(error=str(exc)), code=400)

    server = HTTPServer(('127.0.0.1', 0), Handler)
    url = f'http://127.0.0.1:{server.server_port}/{batch.token}/'
    rep.write(batch.work / 'session.json', dict(url=url, pid=os.getpid()))
    print(json.dumps(dict(url=url, work=str(batch.work))), flush=True)
    server.serve_forever()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='command', required=True)
    start = sub.add_parser('serve')
    for key in ('project', 'source', 'analysis', 'work'):
        start.add_argument('--' + key, required=True)
    start.add_argument('--tables')
    start.add_argument('--mcp-verified', action='store_true')
    check = sub.add_parser('verify')
    check.add_argument('--work', required=True)
    args = parser.parse_args()
    if args.command == 'verify':
        print(json.dumps(verify(args.work), ensure_ascii=True, indent=2))
    else:
        serve(Batch(args.project, args.source, args.analysis, args.work, args.mcp_verified,
                    rep.read(args.tables) if args.tables else None))
