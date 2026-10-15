"""Isolated mixed-batch regressions. No real project assets are changed."""
from pathlib import Path
import tempfile
import unittest
from confirmation import Batch, imp, rep, verify


class MixedTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.source = self.root / 'input'
        self.source.mkdir()
        for name, color in [('a', 'red'), ('b', 'blue')]:
            rep.Image.new('RGB', (32, 32), color).save(self.source / (name + '.png'))
        tables = self.root / 'Assets/GameResource/TableSources'
        tables.mkdir(parents=True)
        for key, fields in imp.TABLES.items():
            (tables / f'novel_{key}.etable.csv').write_text(','.join(fields) + '\n', encoding='utf-8')
        self.old = self.root / 'Assets/GameResource/old.png'
        rep.Image.new('RGB', (32, 32), 'green').save(self.old)
        Path(str(self.old) + '.meta').write_text('guid: old', encoding='utf-8')
        self.analysis = self.root / 'analysis.json'
        rep.write(self.analysis, dict(items=[dict(source=str(self.source / 'b.png'), candidates=[
            dict(target=str(self.old), reason='same scene', warnings=[], references=['background: room'])]),
            dict(source=str(self.source / 'a.png'), candidates=[])]))
        self.work = self.root / '.utmp/vn-image-sync/test'
        self.batch = Batch(self.root, self.source, self.analysis, self.work, ready=True)
        self.choices = [dict(index=0, route='import', kind='background', story='Story', scene='room', variant='day'),
                        dict(index=1, route='replace', candidate=0, acknowledged=True)]

    def confirm(self):
        p = self.batch.submit('preview', dict(choices=self.choices))
        return self.batch.submit('confirm', dict(choices=self.choices, reviewHash=p['reviewHash']))

    def test_one_receipt_two_plans_no_asset_writes(self):
        before = self.batch.snapshot()
        self.confirm()
        result = verify(self.work)
        self.assertEqual(result['plan']['imports'][0]['assetAction'], 'copy')
        self.assertEqual(result['plan']['replacements'][1]['action'], 'replace')
        self.assertEqual(before, self.batch.snapshot())

    def test_cancel_terminal(self):
        self.batch.submit('cancel', {})
        with self.assertRaises(ValueError):
            self.confirm()
        with self.assertRaises(ValueError):
            verify(self.work)

    def test_offline_cannot_confirm(self):
        self.batch.ready = False
        with self.assertRaises(ValueError):
            self.confirm()
        self.assertFalse((self.work / 'decision.json').exists())

    def test_changed_choice_invalidates_review(self):
        preview = self.batch.submit('preview', dict(choices=self.choices))
        self.choices[0]['scene'] = 'different'
        with self.assertRaises(ValueError):
            self.batch.submit('confirm', dict(choices=self.choices, reviewHash=preview['reviewHash']))

    def test_external_changes_invalidate_receipt(self):
        self.confirm()
        (self.root / 'Assets/new-reference.txt').write_text('changed', encoding='utf-8')
        with self.assertRaises(ValueError):
            verify(self.work)

    def test_cross_group_target_collision(self):
        target = self.batch.importer.plan([dict(self.choices[0], conflict='block'), dict(index=1, kind='skip')])[0]['target']
        self.batch.replacer.items[1]['candidates'][0]['target'] = target
        with self.assertRaisesRegex(ValueError, '冲突'):
            self.batch.plan(self.choices)

    def test_missing_source_rejected(self):
        data = rep.read(self.analysis)
        data['items'].pop()
        rep.write(self.analysis, data)
        with self.assertRaisesRegex(ValueError, '一一对应'):
            Batch(self.root, self.source, self.analysis, self.root / '.utmp/vn-image-sync/missing')

    def test_warning_requires_acknowledgement(self):
        self.choices[1]['acknowledged'] = False
        with self.assertRaises(ValueError):
            self.batch.plan(self.choices)


if __name__ == '__main__':
    unittest.main()
