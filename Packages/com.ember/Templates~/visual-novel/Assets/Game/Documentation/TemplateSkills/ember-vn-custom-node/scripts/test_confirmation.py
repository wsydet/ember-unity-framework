"""confirmation.py 的自测：只在临时目录里造一个假项目，不接触真实项目或 Unity。

覆盖确认服务的核心契约：
  1. 未确认零写入      —— 只有草稿 / 从未提交 / 超时都不产生任何可执行回执
  2. 确认回读          —— confirmed 回执可通过 verify，且带完整计划与两个 hash
  3. 指纹失效          —— 确认后项目文件变化，verify 必须失败
  4. 取消为终态        —— cancelled 之后不能再提交，verify 拒绝
  5. 回执被改          —— 改动 decision.json 的计划或选项，verify 必须失败
  6. 闸门              —— 不适用阶梯 / 修改模板自带文件 / 路径越界 一律拒绝

运行：python -B test_confirmation.py -v

根目录默认取系统临时目录。若运行环境的系统临时目录不可写（受限沙箱），
用 `VN_CUSTOM_NODE_TEST_TMP` 指向一个可写的临时位置；测试仍然只在自己的
独立子目录里造/删文件，不会读写真实项目。
"""
import json
import os
from pathlib import Path
import shutil
import sys
import tempfile
import threading
import time
import unittest
import urllib.error
import urllib.parse
import urllib.request
import uuid

sys.path.insert(0, str(Path(__file__).resolve().parent))
import confirmation  # noqa: E402


def scratch_root():
    """测试根目录：默认系统临时目录，可用 VN_CUSTOM_NODE_TEST_TMP 指到可写位置。

    这里刻意不用 `tempfile.mkdtemp`：部分受限环境会把 mkdtemp 建出的 0o700 目录设为
    不可写，测试自身反而无法在其中造项目。改用带随机后缀的 makedirs，语义相同但更可移植。
    """
    override = os.environ.get('VN_CUSTOM_NODE_TEST_TMP')
    base = Path(override) if override else Path(tempfile.gettempdir())
    base.mkdir(parents=True, exist_ok=True)
    root = base / ('vn-custom-node-' + uuid.uuid4().hex[:10])
    root.mkdir(parents=True, exist_ok=False)
    return root


def plan(**overrides):
    value = {
        'schemaVersion': 1,
        'skillId': confirmation.SKILL_ID,
        'planId': 'name-input',
        'request': '开局在黑幕里让玩家输入名字，之后别人用这个名字称呼他',
        'tier': 'bridge',
        'tiers': [
            {'id': 'preset', 'label': '二级步骤预设', 'applicable': False,
             'reason': '只能组合已有指令，无法接收玩家输入'},
            {'id': 'bridge', 'label': '模块桥接节点', 'applicable': True,
             'reason': '输入流程与结果写回都可由业务 Module 承担'},
            {'id': 'script', 'label': '自定义节点脚本', 'applicable': True,
             'reason': '需要输入页与剧情层特殊交互时使用'},
        ],
        'summary': '用模块桥接节点插入名字输入，结果写进全局变量 playerName',
        'step': {'storyPath': 'Assets/GameResource/Resources/Config/Narrative/LastLight/Story.asset',
                 'chapterId': 'CH00', 'nodeId': 'CH00_开场', 'insertAfter': 3,
                 'commandKind': 'CustomStep'},
        'bridge': {'requestKey': 'name_input', 'payload': '', 'resultVariableId': 'playerName',
                   'variableScope': 'Global', 'timeoutSeconds': 120},
        'variables': [{'id': 'playerName', 'type': 'String', 'scope': 'Global',
                       'declaredIn': 'story', 'purpose': '后续对白与说话人栏显示'}],
        'files': [
            {'path': 'Assets/Game/Module/MyGame/Narrative/NameInputServiceModule.cs',
             'action': 'create', 'ownership': 'new', 'purpose': '输入流程与结果回写'},
            {'path': 'Assets/GameResource/Authoring/Narrative/Scripts/MyGame/NameInputBridge.asset',
             'action': 'create', 'ownership': 'new', 'purpose': '请求键与结果变量配置'},
        ],
        'registration': {'storyPath': 'Assets/GameResource/Resources/Config/Narrative/LastLight/Story.asset',
                         'action': 'append-custom-step', 'scriptId': 'MyGame.NameInputBridge'},
        'ui': {'page': 'EUINameInputPage', 'pageType': 'Popup', 'viaEuI': True},
        'fingerprint': {'customStepIncluded': True, 'speakerVariableIncluded': False},
        'risks': ['改桥接参数会让旧档判为不兼容'],
        'verification': ['剧情校验通过', 'Unity MCP 编译 0 错误'],
        'fingerprintRoots': ['Assets/Game/Module/Narrative'],
    }
    value.update(overrides)
    return value


class Project:
    """一个最小的假项目：只有 Assets 与前缀目录，够确认服务做路径与指纹检查。"""

    def __init__(self):
        self.root = scratch_root()
        (self.root / 'Assets/Game/Module/Narrative').mkdir(parents=True)
        (self.root / 'Assets/Game/Module/Narrative/NovelSession.cs').write_text('// 模板自带文件\n', encoding='utf-8')
        (self.root / 'Assets/GameResource').mkdir(parents=True)
        self.servers = []

    def write(self, relative, text='x'):
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding='utf-8')

    def start(self, batch_plan=None, ready=True, suggestions=None):
        batch = confirmation.Batch(self.root, batch_plan or plan(), None, ready, suggestions)
        threading.Thread(target=confirmation.serve, args=(batch, 0), daemon=True).start()
        url = None
        for _ in range(400):
            session = batch.work / 'session.json'
            if session.exists():
                url = json.loads(session.read_text(encoding='utf-8'))['url']
                break
            time.sleep(0.05)
        if url is None:
            raise RuntimeError('确认服务未启动')
        return batch, url.rstrip('/')

    def call(self, url, action, payload=None):
        parsed = urllib.parse.urlsplit(url)
        request = urllib.request.Request(
            url + '/' + action,
            data=json.dumps(payload or {}).encode('utf-8'),
            headers={'Content-Type': 'application/json',
                     'Origin': '%s://%s' % (parsed.scheme, parsed.netloc)},
            method='POST')
        try:
            with urllib.request.urlopen(request, timeout=10) as response:
                return response.status, json.loads(response.read().decode('utf-8'))
        except urllib.error.HTTPError as error:
            return error.code, json.loads(error.read().decode('utf-8'))

    def close(self):
        shutil.rmtree(self.root, ignore_errors=True)


class ConfirmationTests(unittest.TestCase):
    def setUp(self):
        self.project = Project()
        self.addCleanup(self.project.close)

    # 1 -----------------------------------------------------------------
    def test_draft_and_silence_never_authorize(self):
        batch, url = self.project.start(ready=False)
        status, body = self.project.call(url, 'draft', {'choices': {'tier': 'bridge'}})
        self.assertEqual(200, status, body)
        self.assertEqual('draft', body['status'])
        self.assertTrue((batch.work / 'draft.json').exists())
        # 草稿不生成回执；verify 必须拒绝。
        self.assertFalse((batch.work / 'decision.json').exists())
        with self.assertRaises(ValueError):
            confirmation.verify(batch.work)
        # 只保存草稿不产生任何计划文件。
        self.assertFalse((self.project.root / 'Assets/Game/Module/MyGame').exists())

    def test_confirm_requires_mcp_verified(self):
        batch, url = self.project.start(ready=False)
        status, body = self.project.call(url, 'confirm', {'choices': {'tier': 'bridge', 'includeStep': True}})
        self.assertEqual(400, status)
        self.assertIn('MCP 未验证', body['error'])
        self.assertFalse((batch.work / 'decision.json').exists())

    # 2 -----------------------------------------------------------------
    def test_confirmed_receipt_round_trips(self):
        batch, url = self.project.start()
        choices = {'tier': 'bridge', 'files': [plan()['files'][0]['path']], 'includeStep': True,
                   'includeUi': False, 'note': '先只做服务模块'}
        status, preview = self.project.call(url, 'preview', {'choices': choices})
        self.assertEqual(200, status, preview)
        self.assertIn('planHash', preview)
        status, body = self.project.call(url, 'confirm',
                                        {'choices': choices, 'reviewHash': preview['reviewHash']})
        self.assertEqual(200, status, body)
        self.assertEqual('confirmed', body['status'])

        decision = confirmation.verify(batch.work)
        self.assertEqual('confirmed', decision['status'])
        self.assertEqual('bridge', decision['plan']['tier'])
        self.assertEqual(confirmation.fingerprint(decision['plan']), decision['planHash'])
        self.assertEqual(1, len(decision['plan']['files']), '只保留用户勾选的文件')
        self.assertTrue(decision['plan']['includeStep'])
        self.assertFalse(decision['plan']['includeUi'])
        self.assertEqual('先只做服务模块', decision['plan']['note'])
        # 回执必须带源指纹与批次身份。
        self.assertEqual(batch.batch_id, decision['batchId'])
        self.assertEqual(confirmation.SKILL_ID, decision['skillId'])
        self.assertEqual(str(self.project.root), decision['projectRoot'])

    def test_confirm_needs_a_fresh_review_hash(self):
        batch, url = self.project.start()
        status, body = self.project.call(url, 'confirm',
                                        {'choices': {'tier': 'bridge', 'includeStep': True}, 'reviewHash': 'stale'})
        self.assertEqual(400, status)
        self.assertIn('重新预览', body['error'])
        self.assertFalse((batch.work / 'decision.json').exists())

    # 3 -----------------------------------------------------------------
    def test_project_change_invalidates_the_receipt(self):
        batch, url = self.project.start()
        choices = {'tier': 'bridge', 'includeStep': True}
        _, preview = self.project.call(url, 'preview', {'choices': choices})
        self.project.call(url, 'confirm', {'choices': choices, 'reviewHash': preview['reviewHash']})
        confirmation.verify(batch.work)  # 确认后立刻可执行

        self.project.write('Assets/Game/Module/Narrative/NovelSession.cs', '// 被别的改动改过了\n')
        with self.assertRaises(ValueError) as error:
            confirmation.verify(batch.work)
        self.assertIn('已变化', str(error.exception))

    # 4 -----------------------------------------------------------------
    def test_cancel_is_terminal(self):
        batch, url = self.project.start()
        status, body = self.project.call(url, 'cancel', {})
        self.assertEqual(200, status, body)
        self.assertEqual('cancelled', body['status'])
        decision = json.loads((batch.work / 'decision.json').read_text(encoding='utf-8'))
        self.assertEqual('cancelled', decision['status'])
        with self.assertRaises(ValueError):
            confirmation.verify(batch.work)
        # 取消后不能再提交或确认。
        for action in ('draft', 'preview', 'confirm'):
            status, body = self.project.call(url, action, {'choices': {'tier': 'bridge', 'includeStep': True}})
            self.assertEqual(400, status, action)
            self.assertIn('已结束', body['error'])

    # 5 -----------------------------------------------------------------
    def test_tampered_receipt_is_rejected(self):
        batch, url = self.project.start()
        choices = {'tier': 'bridge', 'includeStep': True}
        _, preview = self.project.call(url, 'preview', {'choices': choices})
        self.project.call(url, 'confirm', {'choices': choices, 'reviewHash': preview['reviewHash']})

        path = batch.work / 'decision.json'
        decision = json.loads(path.read_text(encoding='utf-8'))
        decision['choices']['tier'] = 'script'
        path.write_text(json.dumps(decision, ensure_ascii=False), encoding='utf-8')
        with self.assertRaises(ValueError) as error:
            confirmation.verify(batch.work)
        self.assertIn('不一致', str(error.exception))

    # 6 -----------------------------------------------------------------
    def test_gates_reject_unsafe_plans(self):
        with self.assertRaises(ValueError) as error:
            confirmation.Batch(self.project.root, plan(tier='preset'))
        self.assertIn('不可用', str(error.exception))

        bad = plan()
        bad['files'][0]['ownership'] = 'template-owned'
        bad['files'][0]['action'] = 'modify'
        with self.assertRaises(ValueError) as error:
            confirmation.Batch(self.project.root, bad)
        self.assertIn('模板自带文件', str(error.exception))

        escaped = plan()
        escaped['files'][0]['path'] = '../../outside/Evil.cs'
        with self.assertRaises(ValueError):
            confirmation.Batch(self.project.root, escaped)

        no_timeout = plan()
        no_timeout['bridge']['timeoutSeconds'] = 0
        with self.assertRaises(ValueError) as error:
            confirmation.Batch(self.project.root, no_timeout)
        self.assertIn('超时', str(error.exception))

        # preset 阶梯本批可用，但仍然列了要新增的文件 → 拒绝（预设不需要写代码）。
        preset_with_files = plan(tier='preset')
        preset_with_files['tiers'][0]['applicable'] = True
        preset_with_files['tiers'][0]['reason'] = '本批可直接复用既有预设资产'
        with self.assertRaises(ValueError) as error:
            confirmation.Batch(self.project.root, preset_with_files)
        self.assertIn('preset', str(error.exception))

    def test_batch_directory_must_live_under_the_batch_root(self):
        with self.assertRaises(ValueError) as error:
            confirmation.Batch(self.project.root, plan(), self.project.root / 'Assets/elsewhere')
        self.assertIn('.utmp/vn-custom-node', str(error.exception))

    def test_default_batch_directory_is_inside_the_batch_root(self):
        batch = confirmation.Batch(self.project.root, plan())
        self.assertTrue(batch.work.is_relative_to(self.project.root / confirmation.BATCH_ROOT))

    def test_step_selection_must_be_explicit(self):
        batch, url = self.project.start()
        status, body = self.project.call(url, 'preview', {'choices': {'tier': 'bridge'}})
        self.assertEqual(400, status)
        self.assertIn('includeStep', body['error'])


if __name__ == '__main__':
    unittest.main(verbosity=2)
