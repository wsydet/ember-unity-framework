"""自定义节点方案确认服务（本机回执服务，只用 Python 标准库）。

本脚本只做一件事：把代理准备的**方案草案**渲染成本机可交互页面，并把用户的
选择 / 草稿 / 明确确认 / 取消写成结构化回执。它**不写 Unity 资产、不调用 Unity、
不生成代码**；真正的实施由代理在 `verify` 通过之后完成。

用法：

    python confirmation.py serve --project <项目根> --plan <plan.json> [--work <批次目录>]
                                 [--suggestions <draft.json>] [--mcp-verified] [--port 0]
    python confirmation.py verify --work <批次目录>

批次目录固定位于 `<项目根>/.utmp/vn-custom-node/<批次>/`，内含：

    plan.json      代理提交的方案草案（原样留档）
    batch.json     批次基线：项目指纹、nonce、batchId、计划
    draft.json     用户保存的草稿（永远不授权实施）
    decision.json  只有页面明确确认或取消才生成，取消为终态
    session.json   服务地址与 PID，供保留日志用

退出码：`verify` 仅在回执为 `status=confirmed` 且计划、选项与项目指纹全部一致时返回 0。
"""
import argparse
import datetime
import hashlib
import json
import os
from pathlib import Path
import re
import secrets
import sys
from http.server import BaseHTTPRequestHandler, HTTPServer

BATCH_ROOT = '.utmp/vn-custom-node'
SKILL_ID = 'ember-vn-custom-node'
SCHEMA_VERSION = 1
# 指纹扫描上限：方案只覆盖少量目录，超过这个规模说明 roots 写错了。
MAX_FINGERPRINT_FILES = 20000
TIERS = ('preset', 'bridge', 'script')
ACTIONS = ('create', 'modify')
OWNERSHIP = ('new', 'project-owned', 'template-owned')
# 模板管理的五个目录：完整重新部署会覆盖，补丁增量只保留本地独有内容与单方修改。
# 修改其中的模板自带文件会在下次模板更新时冲突，因此在确认闸门直接阻断。
TEMPLATE_DIRS = ('Assets/Game', 'Assets/Resources', 'Assets/Ember/Editor',
                 'Assets/Settings', 'Assets/GameResource')


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


def write_json(path, value):
    temporary = path.with_suffix('.tmp')
    temporary.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')
    os.replace(temporary, path)


def safe_segment(value, label):
    value = str(value or '').strip()
    if (not value or len(value) > 100 or re.search(r'[<>:"/\\|?*\x00-\x1f]', value)
            or value.endswith(('.', ' ')) or value in {'.', '..'}
            or re.match(r'^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)', value, re.I)):
        raise ValueError('请填写合法的%s: %s' % (label, value))
    return value


def inside_project(project, raw, label):
    """把项目相对路径解析成绝对路径，并拒绝越界、`..`、盘符与链接路径。"""
    text = str(raw or '').strip().replace('\\', '/')
    if not text:
        raise ValueError('%s 不能为空' % label)
    if text.startswith('/') or re.match(r'^[A-Za-z]:', text):
        raise ValueError('%s 必须是项目相对路径: %s' % (label, raw))
    parts = [p for p in text.split('/') if p not in ('', '.')]
    if not parts or '..' in parts:
        raise ValueError('%s 不能包含 .. : %s' % (label, raw))
    resolved = no_links(project.joinpath(*parts))
    if resolved != project and not resolved.is_relative_to(project):
        raise ValueError('%s 越出项目范围: %s' % (label, raw))
    return resolved


class Batch:
    """一个确认批次：基线 + 方案 + 选择状态。构造时会生成批次目录。"""

    def __init__(self, project, plan, work=None, ready=False, suggestions=None):
        self.project = no_links(project)
        if not (self.project / 'Assets').is_dir():
            raise ValueError('项目根目录下没有 Assets：' + str(self.project))
        self.plan = self.normalize_plan(plan)
        plan_id = safe_segment(self.plan.get('planId') or 'plan', '方案 ID')
        if work is None:
            stamp = secrets.token_hex(2)
            work = self.project / BATCH_ROOT / (plan_id + '-' + stamp)
        self.work = no_links(work)
        if not self.work.is_relative_to(self.project / BATCH_ROOT):
            raise ValueError('批次目录必须位于项目 %s 下' % BATCH_ROOT)
        self.ready = bool(ready)
        self.roots = [inside_project(self.project, r, '指纹根') for r in self.plan.get('fingerprintRoots', [])]
        self.paths = [inside_project(self.project, f['path'], '计划文件') for f in self.plan['files']]
        self.baseline = self.snapshot()
        self.nonce = secrets.token_hex(16)
        self.batch_id = fingerprint({'baseline': self.baseline, 'nonce': self.nonce})
        self.state = 'pending'
        self.token = secrets.token_urlsafe(32)
        self.work.mkdir(parents=True, exist_ok=False)
        write_json(self.work / 'plan.json', self.plan)
        write_json(self.work / 'batch.json', {
            'schemaVersion': SCHEMA_VERSION, 'skillId': SKILL_ID, 'projectRoot': str(self.project),
            'plan': self.plan, 'roots': [str(r) for r in self.roots],
            'baseline': self.baseline, 'nonce': self.nonce, 'batchId': self.batch_id, 'ready': self.ready})
        if suggestions:
            write_json(self.work / 'draft.json', {'status': 'draft', 'batchId': self.batch_id,
                                                  'choices': suggestions})

    # ---------------------------------------------------------------- 方案

    def normalize_plan(self, plan):
        if not isinstance(plan, dict):
            raise ValueError('方案必须是 JSON 对象')
        if plan.get('schemaVersion') != SCHEMA_VERSION:
            raise ValueError('方案 schemaVersion 必须是 %d' % SCHEMA_VERSION)
        if plan.get('skillId') != SKILL_ID:
            raise ValueError('方案 skillId 必须是 %s' % SKILL_ID)
        tiers = plan.get('tiers')
        if not isinstance(tiers, list) or not tiers:
            raise ValueError('方案必须列出三级阶梯候选 tiers')
        seen = set()
        for tier in tiers:
            if not isinstance(tier, dict) or tier.get('id') not in TIERS:
                raise ValueError('tiers 只接受 %s' % ', '.join(TIERS))
            if tier['id'] in seen:
                raise ValueError('tiers 出现重复项: ' + tier['id'])
            seen.add(tier['id'])
            tier['applicable'] = bool(tier.get('applicable'))
            if tier['applicable'] and not str(tier.get('reason') or '').strip():
                raise ValueError('可选的阶梯必须写明 reason: ' + tier['id'])
        if plan.get('tier') not in seen:
            raise ValueError('方案选中的 tier 必须出现在 tiers 里')
        chosen = next(t for t in tiers if t['id'] == plan['tier'])
        if not chosen['applicable']:
            raise ValueError('选中的阶梯被标记为不可用，不能提交: ' + plan['tier'])
        if not str(plan.get('summary') or '').strip():
            raise ValueError('方案必须写 summary')
        files = plan.get('files')
        if not isinstance(files, list):
            raise ValueError('方案必须列出 files')
        for item in files:
            if not isinstance(item, dict):
                raise ValueError('files 的每一项都必须是对象')
            if item.get('action') not in ACTIONS:
                raise ValueError('files.action 只接受 %s' % ', '.join(ACTIONS))
            if item.get('ownership') not in OWNERSHIP:
                raise ValueError('files.ownership 只接受 %s：%s' % (', '.join(OWNERSHIP), item.get('path')))
            # 只新增文件、不改模板自带文件：这是本仓库的硬约束，在闸门处直接阻断。
            if item['ownership'] == 'template-owned' and item['action'] == 'modify':
                raise ValueError('不能修改模板自带文件（只新增、不修改）: ' + str(item.get('path')))
            if not str(item.get('purpose') or '').strip():
                raise ValueError('files 必须写 purpose: ' + str(item.get('path')))
            item['path'] = str(item['path']).strip().replace('\\', '/')
        if plan.get('tier') == 'preset' and files:
            raise ValueError('阶梯 preset 不需要新增文件；files 必须为空')
        if plan.get('tier') == 'bridge':
            bridge = plan.get('bridge') or {}
            if not str(bridge.get('requestKey') or '').strip():
                raise ValueError('阶梯 bridge 必须填 bridge.requestKey')
            timeout = bridge.get('timeoutSeconds')
            if not isinstance(timeout, (int, float)) or isinstance(timeout, bool) or timeout <= 0:
                raise ValueError('bridge.timeoutSeconds 必须大于 0：没有超时的节点会让剧情永久停在这里')
        if plan.get('tier') == 'script':
            if not str((plan.get('script') or {}).get('scriptId') or '').strip():
                raise ValueError('阶梯 script 必须填 script.scriptId')
        plan.setdefault('files', files)
        plan.setdefault('variables', [])
        plan.setdefault('risks', [])
        plan.setdefault('verification', [])
        plan.setdefault('fingerprintRoots', [])
        return plan

    # ---------------------------------------------------------------- 指纹

    def snapshot(self):
        candidates = [p for p in self.paths] + list(self.roots)
        for root in self.roots:
            if root.is_dir():
                candidates += [no_links(p) for p in root.rglob('*') if p.is_file()]
        # 只比对文件与「按计划尚未存在」这两种状态：目录本身不进指纹，
        # 计划里要新建的文件还不存在时记为 None，避免把目录当文件读取。
        unique = sorted({str(p) for p in candidates if p.is_file() or not p.exists()})
        if len(unique) > MAX_FINGERPRINT_FILES:
            raise ValueError('指纹文件数超过上限 %d，请缩小 fingerprintRoots' % MAX_FINGERPRINT_FILES)
        return {raw: digest(Path(raw)) for raw in unique}

    def check(self):
        if self.snapshot() != self.baseline:
            raise ValueError('计划文件或指纹目录已变化；请新建批次重新核对，旧确认无效')

    # ---------------------------------------------------------------- 回执

    def manifest(self):
        draft_path = self.work / 'draft.json'
        draft = json.loads(draft_path.read_text(encoding='utf-8')) if draft_path.exists() else None
        decision_path = self.work / 'decision.json'
        decision = json.loads(decision_path.read_text(encoding='utf-8')) if decision_path.exists() else None
        return {'batchId': self.batch_id, 'skillId': SKILL_ID, 'state': self.state,
                'ready': self.ready, 'work': str(self.work), 'token': self.token,
                'plan': self.plan, 'tiers': self.plan['tiers'], 'selectedTier': self.plan['tier'],
                'baseline': {'files': len(self.baseline), 'roots': [str(r) for r in self.roots]},
                'draft': draft['choices'] if draft and draft.get('batchId') == self.batch_id else None,
                'decision': decision}

    def effective_plan(self, choices):
        """把用户选择收窄成真正要执行的计划。"""
        if not isinstance(choices, dict):
            raise ValueError('选择必须是 JSON 对象')
        tier = choices.get('tier')
        tier_ids = [t['id'] for t in self.plan['tiers'] if t['applicable']]
        if tier not in tier_ids:
            raise ValueError('必须在本批可用的阶梯里选择一个: ' + ', '.join(tier_ids))
        allowed = {f['path'] for f in self.plan['files']}
        selected = choices.get('files', [])
        if not isinstance(selected, list):
            raise ValueError('choices.files 必须是数组')
        for path in selected:
            if path not in allowed:
                raise ValueError('选择了计划里没有的文件: ' + str(path))
        keep = [f for f in self.plan['files'] if f['path'] in set(selected)]
        include_step = bool(choices.get('includeStep'))
        include_ui = bool(choices.get('includeUi'))
        if self.plan.get('step') and not include_step:
            raise ValueError('本批方案包含剧情步骤，必须明确选择是否插入（includeStep）')
        if self.plan.get('ui') and include_ui and not self.plan['ui'].get('viaEuI'):
            raise ValueError('本批包含 UI，但方案未声明经 UI 中心制作（ui.viaEuI）')
        effective = dict(self.plan)
        effective['tier'] = tier
        effective['files'] = keep
        effective['includeStep'] = include_step
        effective['includeUi'] = include_ui
        effective['note'] = str(choices.get('note') or '')
        return effective

    def submit(self, action, payload):
        if self.state != 'pending':
            raise ValueError('本批已结束；请新建批次，不能重复提交或撤销已经提交的决定')
        if action == 'cancel':
            self.state = 'cancelled'
            write_json(self.work / 'decision.json', {
                'schemaVersion': SCHEMA_VERSION, 'skillId': SKILL_ID, 'batchId': self.batch_id,
                'projectRoot': str(self.project), 'status': self.state})
            return {'status': self.state}
        if action == 'draft':
            write_json(self.work / 'draft.json', {'status': 'draft', 'batchId': self.batch_id,
                                                  'choices': payload.get('choices', {})})
            return {'status': 'draft'}
        self.check()
        plan = self.effective_plan(payload.get('choices', {}))
        plan_hash = fingerprint(plan)
        review_hash = fingerprint({'batchId': self.batch_id, 'choices': payload.get('choices', {}), 'plan': plan})
        if action == 'preview':
            return {'plan': plan, 'planHash': plan_hash, 'reviewHash': review_hash}
        if action != 'confirm':
            raise ValueError('未知操作: ' + str(action))
        if not self.ready:
            raise ValueError('Unity MCP 未验证，实际实施尚不可执行；只能保存草稿')
        if payload.get('reviewHash') != review_hash:
            raise ValueError('选择已改变，请重新预览完整计划再确认')
        write_json(self.work / 'decision.json', {
            'schemaVersion': SCHEMA_VERSION, 'skillId': SKILL_ID, 'batchId': self.batch_id,
            'projectRoot': str(self.project), 'status': 'confirmed',
            'choices': payload.get('choices', {}), 'plan': plan,
            'planHash': plan_hash, 'reviewHash': review_hash,
            'decidedAt': datetime.datetime.now().astimezone().isoformat(timespec='seconds')})
        self.state = 'confirmed'
        return {'status': self.state, 'path': str(self.work / 'decision.json'), 'planHash': plan_hash}


def verify(work):
    """回读回执并复核项目指纹。只在 confirmed 且全部一致时返回。"""
    work = no_links(work)
    for name in ('batch.json', 'decision.json'):
        if not (work / name).is_file():
            raise ValueError('本批缺少 %s：没有明确确认，禁止实施' % name)
    batch = json.loads((work / 'batch.json').read_text(encoding='utf-8'))
    decision = json.loads((work / 'decision.json').read_text(encoding='utf-8'))
    if decision.get('status') != 'confirmed' or decision.get('batchId') != batch['batchId']:
        raise ValueError('没有本批明确确认；禁止实施')
    if decision.get('skillId') != SKILL_ID or batch.get('skillId') != SKILL_ID:
        raise ValueError('回执不属于本技能')
    if fingerprint({'baseline': batch['baseline'], 'nonce': batch['nonce']}) != batch['batchId']:
        raise ValueError('批次基线被改动，回执无效')
    obj = Batch.__new__(Batch)
    obj.project = no_links(Path(batch['projectRoot']))
    obj.plan = batch['plan']
    obj.roots = [no_links(Path(p)) for p in batch['roots']]
    obj.paths = [inside_project(obj.project, f['path'], '计划文件') for f in obj.plan['files']]
    obj.baseline = batch['baseline']
    obj.check()
    if fingerprint(decision.get('plan')) != decision.get('planHash'):
        raise ValueError('计划内容与 planHash 不一致，禁止实施')
    expected = fingerprint({'batchId': batch['batchId'], 'choices': decision.get('choices'),
                            'plan': decision.get('plan')})
    if expected != decision.get('reviewHash'):
        raise ValueError('回执内容不一致，禁止实施')
    # 二次校验：即使回执被改过，计划本身仍要过一遍路径与阶梯检查。
    obj.normalize_plan(dict(decision['plan']))
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
            self.send_header('Content-Length', str(len(raw)))
            self.end_headers()
            self.wfile.write(raw)

        def do_GET(self):
            try:
                prefix = '/' + batch.token
                if self.path in (prefix + '/', prefix):
                    self.reply((Path(__file__).parent.parent / 'assets/confirmation.html').read_bytes(),
                               mime='text/html; charset=utf-8')
                elif self.path == prefix + '/manifest':
                    self.reply(batch.manifest())
                else:
                    self.reply({'error': 'not found'}, 404)
            except (ValueError, OSError) as exc:
                self.reply({'error': str(exc)}, 400)

        def do_POST(self):
            try:
                origin = self.headers.get('Origin')
                if origin != 'http://127.0.0.1:%d' % self.server.server_port:
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
    url = 'http://127.0.0.1:%d/%s/' % (server.server_port, batch.token)
    write_json(batch.work / 'session.json', {'url': url, 'pid': os.getpid(), 'work': str(batch.work)})
    print(json.dumps({'url': url, 'work': str(batch.work)}, ensure_ascii=False), flush=True)
    server.serve_forever()


def main(argv=None):
    parser = argparse.ArgumentParser(description='自定义节点方案确认服务')
    sub = parser.add_subparsers(dest='command', required=True)
    start = sub.add_parser('serve')
    start.add_argument('--project', required=True, help='Unity 项目根目录')
    start.add_argument('--plan', required=True, help='方案草案 JSON（代理准备）')
    start.add_argument('--work', help='批次目录，默认 <项目>/.utmp/vn-custom-node/<planId>-<随机>')
    start.add_argument('--suggestions', help='预填选择 JSON；只是草稿，永远不构成确认')
    start.add_argument('--mcp-verified', action='store_true',
                       help='仅在实时 Unity MCP 身份检查通过后添加；这是代理的声明，不是自动连接器')
    start.add_argument('--port', type=int, default=0)
    check = sub.add_parser('verify')
    check.add_argument('--work', required=True)
    args = parser.parse_args(argv)
    if args.command == 'verify':
        decision = verify(args.work)
        print(json.dumps({'status': decision['status'], 'planHash': decision['planHash'],
                          'tier': decision['plan'].get('tier')}, ensure_ascii=False))
        return 0
    plan = json.loads(Path(args.plan).read_text(encoding='utf-8'))
    suggestions = json.loads(Path(args.suggestions).read_text(encoding='utf-8')) if args.suggestions else None
    batch = Batch(Path(args.project), plan, Path(args.work) if args.work else None,
                  args.mcp_verified, suggestions)
    serve(batch, args.port)
    return 0


if __name__ == '__main__':
    # 两个流都固定成 UTF-8：Windows 控制台默认用本地代码页，中文错误信息会写出
    # 非 UTF-8 字节，既可能让调用方解码失败，也可能直接抛 UnicodeEncodeError 掩盖真正原因。
    for _stream in (sys.stdout, sys.stderr):
        try:
            _stream.reconfigure(encoding='utf-8', errors='backslashreplace')
        except (AttributeError, ValueError, OSError):
            pass
    try:
        sys.exit(main())
    except (ValueError, OSError, KeyError) as error:
        print('error: %s' % error, file=sys.stderr)
        sys.exit(2)
