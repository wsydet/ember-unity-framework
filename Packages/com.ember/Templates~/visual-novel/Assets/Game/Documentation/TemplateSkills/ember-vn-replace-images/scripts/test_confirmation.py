"""Isolated fixtures only: no real project images are replaced."""
import json
from pathlib import Path
import tempfile
import unittest
from PIL import Image
from confirmation import Batch, verify


class ConfirmationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / 'Assets/GameResource').mkdir(parents=True)
        self.source = self.root / 'new.png'
        self.target = self.root / 'Assets/GameResource/old.png'
        Image.new('RGBA', (40, 60), (200, 40, 50, 255)).save(self.source)
        Image.new('RGBA', (40, 60), (20, 40, 50, 255)).save(self.target)
        Path(str(self.target) + '.meta').write_text('guid: stable-guid')
        self.analysis = self.root / 'analysis.json'
        self.data = {'items': [{'source': str(self.source), 'candidates': [{
            'target': str(self.target), 'reason': '测试构图', 'warnings': ['微笑差分'],
            'references': ['portrait: hero_smile']}]}]}
        self.choices = [{'candidate': 0, 'acknowledged': True}]

    def batch(self, ready=True):
        self.analysis.write_text(json.dumps(self.data), encoding='utf-8')
        return Batch(self.root, self.analysis, self.root / '.utmp/vn-image-replace/test', ready)

    def confirm(self, batch):
        preview = batch.submit('preview', {'choices': self.choices})
        batch.submit('confirm', {'choices': self.choices, 'reviewHash': preview['reviewHash']})

    def test_confirm_no_asset_write(self):
        before = self.target.read_bytes()
        batch = self.batch()
        self.confirm(batch)
        self.assertEqual(verify(batch.work)['plan'][0]['action'], 'replace')
        self.assertEqual(before, self.target.read_bytes())

    def test_cancel_terminal(self):
        batch = self.batch()
        batch.submit('cancel', {})
        with self.assertRaises(ValueError):
            self.confirm(batch)
        with self.assertRaises(ValueError):
            verify(batch.work)

    def test_offline_draft_not_confirmation(self):
        batch = self.batch(False)
        batch.submit('draft', {'choices': self.choices})
        with self.assertRaises(ValueError):
            self.confirm(batch)
        self.assertFalse((batch.work / 'decision.json').exists())

    def test_warning_requires_ack(self):
        batch = self.batch()
        with self.assertRaises(ValueError):
            batch.plan([{'candidate': 0, 'acknowledged': False}])

    def test_changed_meta_invalidates(self):
        batch = self.batch()
        self.confirm(batch)
        Path(str(self.target) + '.meta').write_text('guid: changed')
        with self.assertRaises(ValueError):
            verify(batch.work)

    def test_new_reference_invalidates(self):
        batch = self.batch()
        self.confirm(batch)
        (self.root / 'Assets/reference.asset').write_text('new reference')
        with self.assertRaises(ValueError):
            verify(batch.work)

    def test_duplicate_target_rejected(self):
        second = self.root / 'second.png'
        Image.new('RGB', (40, 60)).save(second)
        self.data['items'].append({**self.data['items'][0], 'source': str(second)})
        batch = self.batch()
        with self.assertRaises(ValueError):
            batch.plan(self.choices * 2)

    def test_stale_preview_rejected(self):
        batch = self.batch()
        preview = batch.submit('preview', {'choices': self.choices})
        with self.assertRaises(ValueError):
            batch.submit('confirm', {'choices': [{'candidate': None}], 'reviewHash': preview['reviewHash']})

    def test_outside_target_rejected(self):
        self.data['items'][0]['candidates'][0]['target'] = str(self.source)
        with self.assertRaises(ValueError):
            self.batch()

    def test_no_match_is_skip(self):
        self.data['items'][0]['candidates'] = []
        batch = self.batch()
        self.assertEqual(batch.plan([{'candidate': None}])[0]['action'], 'skip')

    def test_format_mismatch_blocked(self):
        jpeg = self.root / 'new.jpg'
        Image.new('RGB', (40, 60)).save(jpeg)
        self.data['items'][0]['source'] = str(jpeg)
        batch = self.batch()
        with self.assertRaises(ValueError):
            batch.plan(self.choices)

    def test_tampered_plan_rejected(self):
        batch = self.batch()
        self.confirm(batch)
        decision = json.loads((batch.work / 'decision.json').read_text(encoding='utf-8'))
        decision['plan'][0]['target'] = str(self.source)
        (batch.work / 'decision.json').write_text(json.dumps(decision), encoding='utf-8')
        with self.assertRaises(ValueError):
            verify(batch.work)


if __name__ == '__main__':
    unittest.main()
