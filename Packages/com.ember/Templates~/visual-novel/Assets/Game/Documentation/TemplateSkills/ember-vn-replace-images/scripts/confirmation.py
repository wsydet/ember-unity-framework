"""Review image replacement mappings; never writes project assets. Python 3.10+, Pillow."""
import argparse
import hashlib
import io
import json
import os
from pathlib import Path
import secrets
from http.server import BaseHTTPRequestHandler, HTTPServer
from PIL import Image


def fingerprint(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, ensure_ascii=False).encode()).hexdigest()


def path_of(value):
    path = Path(os.path.abspath(value))
    for part in (path, *path.parents):
        if part.is_symlink() or (getattr(part, 'is_junction', lambda: False)()):
            raise ValueError('不允许链接路径: ' + str(part))
        if part.exists() and getattr(part.stat(), 'st_file_attributes', 0) & 0x400:
            raise ValueError('不允许重解析路径: ' + str(part))
    return path


def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def write(path, value):
    temp = path.with_suffix('.tmp')
    temp.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')
    os.replace(temp, path)


def info(path):
    with Image.open(path) as im:
        im.load()
        if getattr(im, 'n_frames', 1) != 1:
            raise ValueError('暂不支持动画或多页图片: ' + str(path))
        return {'size': list(im.size), 'format': im.format,
                'alpha': im.convert('RGBA').getchannel('A').getextrema()[0] < 255,
                'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}


class Batch:
    def __init__(self, project, analysis, work, ready=False):
        self.project, self.analysis, self.work = map(path_of, (project, analysis, work))
        if not (self.project / 'Assets').is_dir():
            raise ValueError('项目缺少 Assets')
        if not self.work.is_relative_to(self.project / '.utmp/vn-image-replace'):
            raise ValueError('批次必须在项目 .utmp/vn-image-replace 下')
        self.ready = ready
        self.items = self.build_items()
        self.baseline = self.snapshot()
        self.batch_id = secrets.token_hex(16)
        self.state = 'pending'
        self.token = secrets.token_urlsafe(32)
        self.work.mkdir(parents=True, exist_ok=False)
        write(self.work / 'batch.json', {'project': str(self.project), 'analysis': str(self.analysis),
              'items': self.items, 'baseline': self.baseline, 'batchId': self.batch_id, 'ready': ready})

    def resolve(self, value):
        return path_of(self.project / value)

    def build_items(self):
        raw = read(self.analysis)['items']
        if not isinstance(raw, list) or not raw:
            raise ValueError('需要非空图片列表')
        sources = [self.resolve(item['source']) for item in raw]
        if len(set(sources)) != len(sources):
            raise ValueError('来源重复')
        result = []
        for item, source in zip(raw, sources):
            source_info = info(source)
            candidates = []
            for candidate in item['candidates']:
                target = self.resolve(candidate['target'])
                if not target.is_relative_to(self.project / 'Assets/GameResource') or target in sources:
                    raise ValueError('目标越界或与本批来源重叠: ' + str(target))
                target_info = info(target)
                warnings = list(candidate.get('warnings', []))
                blocked = candidate.get('blocked', '')
                if not Path(str(target) + '.meta').is_file():
                    blocked = '目标缺少 .meta，需先经 Unity 导入并核对'
                if source_info['format'] != target_info['format']:
                    blocked = '新旧实际编码不同，不支持隐式转码'
                if source_info['size'] != target_info['size']:
                    warnings.append('尺寸变化: %s → %s；核对裁切、pivot 和九宫格' %
                                    (target_info['size'], source_info['size']))
                if source_info['alpha'] != target_info['alpha']:
                    warnings.append('透明像素状态发生变化')
                same = source_info['sha256'] == target_info['sha256']
                if not same:
                    warnings.append('原路径覆盖将影响此旧图的全部引用；其他差分保持原样')
                if not candidate.get('reason') or not isinstance(candidate.get('references'), list):
                    raise ValueError('候选必须包含匹配理由和引用检查结果')
                candidates.append({**candidate, 'target': str(target), 'info': target_info,
                                   'warnings': warnings, 'blocked': blocked, 'same': same})
            result.append({'source': str(source), 'info': source_info, 'candidates': candidates})
        return result

    def snapshot(self):
        paths = [path_of(p) for p in (self.project / 'Assets').rglob('*') if p.is_file()]
        paths += [self.analysis] + [path_of(item['source']) for item in self.items]
        return {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(set(paths))}

    def check(self):
        if self.snapshot() != self.baseline:
            raise ValueError('图片、分析或项目文件已变化，请创建新批次重新确认')

    def plan(self, choices):
        if not isinstance(choices, list) or len(choices) != len(self.items):
            raise ValueError('必须逐图选择或跳过')
        plan, targets = [], set()
        for index, (item, choice) in enumerate(zip(self.items, choices)):
            selected = choice.get('candidate')
            if selected is None:
                plan.append({'index': index, 'source': item['source'], 'action': 'skip'})
                continue
            if type(selected) is not int or not 0 <= selected < len(item['candidates']):
                raise ValueError('候选索引无效')
            candidate = item['candidates'][selected]
            if candidate['blocked']:
                raise ValueError(candidate['blocked'])
            if candidate['warnings'] and choice.get('acknowledged') is not True:
                raise ValueError('请逐项确认差分及引用影响')
            key = candidate['target'].casefold()
            if key in targets:
                raise ValueError('多个来源不能替换同一目标')
            targets.add(key)
            plan.append({'index': index, 'source': item['source'], 'target': candidate['target'],
                         'action': 'unchanged' if candidate['same'] else 'replace',
                         'sourceSha256': item['info']['sha256'],
                         'targetSha256': candidate['info']['sha256'],
                         'preserveMeta': True, 'reason': candidate['reason'],
                         'warnings': candidate['warnings'], 'references': candidate['references']})
        return plan

    def submit(self, action, payload):
        if self.state != 'pending':
            raise ValueError('本批已结束')
        if action == 'cancel':
            write(self.work / 'decision.json', {'status': 'cancelled', 'batchId': self.batch_id})
            self.state = 'cancelled'
            return {'status': self.state}
        self.check()
        choices = payload.get('choices')
        if action == 'draft':
            write(self.work / 'draft.json', {'choices': choices, 'batchId': self.batch_id})
            return {'status': 'draft'}
        plan = self.plan(choices)
        review = fingerprint({'batchId': self.batch_id, 'choices': choices, 'plan': plan})
        if action == 'preview':
            return {'plan': plan, 'reviewHash': review}
        if action != 'confirm' or not self.ready or payload.get('reviewHash') != review:
            raise ValueError('需通过 MCP 前置检查并重新预览后确认')
        decision = {'status': 'confirmed', 'batchId': self.batch_id, 'choices': choices,
                    'plan': plan, 'reviewHash': review}
        write(self.work / 'decision.json', decision)
        self.state = 'confirmed'
        return {'status': self.state}


def verify(work):
    work = path_of(work)
    data, decision = read(work / 'batch.json'), read(work / 'decision.json')
    if not data['ready'] or decision['status'] != 'confirmed' or data['batchId'] != decision['batchId']:
        raise ValueError('没有本批有效确认')
    batch = Batch.__new__(Batch)
    batch.project, batch.analysis = path_of(data['project']), path_of(data['analysis'])
    batch.items, batch.baseline = data['items'], data['baseline']
    batch.check()
    if batch.build_items() != batch.items or batch.plan(decision['choices']) != decision['plan']:
        raise ValueError('回执计划与实际分析不一致')
    expected = fingerprint({key: decision[key] for key in ('batchId', 'choices', 'plan')})
    if expected != decision['reviewHash']:
        raise ValueError('回执指纹不匹配')
    return decision


def serve(batch):
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
                prefix = '/' + batch.token + '/'
                if self.path == prefix:
                    self.reply((Path(__file__).parent.parent / 'assets/confirmation.html').read_bytes(),
                               mime='text/html; charset=utf-8')
                elif self.path == prefix + 'manifest':
                    draft = batch.work / 'draft.json'
                    self.reply({'items': batch.items, 'ready': batch.ready, 'state': batch.state,
                                'draft': read(draft)['choices'] if draft.exists() else None})
                elif self.path.startswith(prefix + 'image/'):
                    indexes = self.path[len(prefix + 'image/'):].split('/')
                    i = int(indexes[0])
                    if not 0 <= i < len(batch.items):
                        raise ValueError('图片索引无效')
                    item = batch.items[i]
                    path = item['source']
                    if len(indexes) == 2:
                        c = int(indexes[1])
                        if not 0 <= c < len(item['candidates']):
                            raise ValueError('候选索引无效')
                        path = item['candidates'][c]['target']
                    with Image.open(path) as im:
                        im.thumbnail((768, 768))
                        stream = io.BytesIO()
                        im.convert('RGBA').save(stream, format='PNG')
                    self.reply(stream.getvalue(), mime='image/png')
                else:
                    self.reply({'error': 'not found'}, 404)
            except (ValueError, OSError, KeyError, IndexError, TypeError) as exc:
                self.reply({'error': str(exc)}, 400)

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
            except (ValueError, OSError, KeyError, IndexError, TypeError) as exc:
                self.reply({'error': str(exc)}, 400)

    server = HTTPServer(('127.0.0.1', 0), Handler)
    url = f'http://127.0.0.1:{server.server_port}/{batch.token}/'
    write(batch.work / 'session.json', {'url': url, 'pid': os.getpid()})
    print(json.dumps({'url': url, 'work': str(batch.work)}), flush=True)
    server.serve_forever()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='command', required=True)
    start = sub.add_parser('serve')
    for key in ('project', 'analysis', 'work'):
        start.add_argument('--' + key, required=True)
    start.add_argument('--mcp-verified', action='store_true')
    check = sub.add_parser('verify')
    check.add_argument('--work', required=True)
    args = parser.parse_args()
    if args.command == 'verify':
        print(json.dumps(verify(args.work), ensure_ascii=True, indent=2))
    else:
        serve(Batch(args.project, args.analysis, args.work, args.mcp_verified))
