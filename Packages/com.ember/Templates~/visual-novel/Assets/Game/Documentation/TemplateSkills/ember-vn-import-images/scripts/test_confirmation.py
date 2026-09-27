"""Isolated regression tests; no Unity or project assets are modified."""
import copy
from pathlib import Path
import tempfile
import unittest
from PIL import Image
from confirmation import Batch, TABLES, verify


class ConfirmationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.source = self.root / 'input'
        self.source.mkdir()
        Image.new('RGBA', (32, 64), (10, 20, 30, 120)).save(self.source / 'a.png')
        Image.new('RGB', (64, 32), (40, 50, 60)).save(self.source / 'b.png')
        tables = self.root / 'Assets/GameResource/TableSources'
        tables.mkdir(parents=True)
        for key, fields in TABLES.items():
            (tables / f'novel_{key}.etable.csv').write_text(','.join(fields) + '\n', encoding='utf-8')
        self.batch = Batch(self.root, self.source, self.root / '.utmp/vn-image-import/test', ready=True)
        self.choices = [dict(index=0, kind='portrait', story='Story', characterId='hero',
                             displayName='主角,一', variant='neutral', conflict='block'),
                        dict(index=1, kind='background', story='Story', scene='room',
                             variant='day', conflict='block')]

    def confirm(self):
        preview = self.batch.submit('preview', {'choices': self.choices})
        return self.batch.submit('confirm', {'choices': self.choices, 'reviewHash': preview['reviewHash']})

    def test_review_receipt_and_zero_asset_writes(self):
        before = self.batch.snapshot()
        self.batch.submit('draft', {'choices': self.choices})
        self.assertFalse((self.batch.work / 'decision.json').exists())
        self.confirm()
        receipt = verify(self.batch.work)
        self.assertEqual(receipt['plan'][0]['tableRows'][0]['row']['displayName'], '主角,一')
        self.assertEqual(receipt['plan'][1]['resourcesPath'], 'UI/Module/Narrative/Atlas/Story/Backgrounds/room_day')
        self.assertEqual(before, self.batch.snapshot())
        self.assertFalse(Path(receipt['plan'][0]['target']).exists())

    def test_cancel_is_terminal(self):
        before = self.batch.snapshot()
        self.batch.submit('cancel', {})
        with self.assertRaises(ValueError):
            verify(self.batch.work)
        with self.assertRaises(ValueError):
            self.confirm()
        self.assertEqual(before, self.batch.snapshot())

    def test_offline_draft_only(self):
        self.batch.ready = False
        self.batch.submit('draft', {'choices': self.choices})
        self.assertEqual(self.batch.manifest()['draft'], self.choices)
        with self.assertRaises(ValueError):
            self.confirm()
        self.assertFalse((self.batch.work / 'decision.json').exists())

    def test_changed_choices_require_new_review(self):
        review = self.batch.submit('preview', {'choices': self.choices})
        self.choices[0]['story'] = 'Changed'
        with self.assertRaises(ValueError):
            self.batch.submit('confirm', {'choices': self.choices, 'reviewHash': review['reviewHash']})

    def test_source_changes_invalidate_confirmation(self):
        self.confirm()
        Image.new('RGB', (8, 8)).save(self.source / 'a.png')
        with self.assertRaises(ValueError):
            verify(self.batch.work)

    def test_table_changes_invalidate_preview(self):
        with self.batch.tables['characters'].open('a') as stream:
            stream.write('new,New\n')
        with self.assertRaises(ValueError):
            self.batch.submit('preview', {'choices': self.choices})

    def test_added_source_requires_new_batch(self):
        Image.new('RGB', (8, 8)).save(self.source / 'c.png')
        with self.assertRaises(ValueError):
            self.confirm()

    def test_missing_duplicate_and_unsafe_choices(self):
        for choices in [self.choices[:1], [self.choices[0], self.choices[0]]]:
            with self.assertRaises(ValueError):
                self.batch.plan(choices)
        self.choices[0]['story'] = '../escape'
        with self.assertRaises(ValueError):
            self.batch.plan(self.choices)

    def test_metadata_and_duplicate_destinations(self):
        images = self.batch.manifest()['images']
        self.assertTrue(images[0]['alpha'])
        self.assertFalse(images[1]['alpha'])
        self.assertEqual(images[0]['height'], 64)
        other = copy.deepcopy(self.choices[0])
        other['index'] = 1
        with self.assertRaises(ValueError):
            self.batch.plan([self.choices[0], other])

    def test_existing_target_requires_exact_reuse(self):
        target = Path(self.batch.plan(self.choices)[0]['target'])
        target.parent.mkdir(parents=True)
        target.write_bytes((self.source / 'a.png').read_bytes())
        batch = Batch(self.root, self.source, self.root / '.utmp/vn-image-import/reuse', ready=True)
        with self.assertRaises(ValueError):
            batch.plan(self.choices)
        self.choices[0]['conflict'] = 'reuse'
        self.assertEqual(batch.plan(self.choices)[0]['assetAction'], 'reuse')
        target.with_suffix('.jpg').write_bytes(target.read_bytes())
        batch2 = Batch(self.root, self.source, self.root / '.utmp/vn-image-import/extension', ready=True)
        with self.assertRaises(ValueError):
            batch2.plan(self.choices)

    def test_existing_table_binding_cannot_be_overwritten(self):
        with self.batch.tables['portraits'].open('a') as stream:
            stream.write('hero_neutral,someone,other,UI/old\n')
        batch = Batch(self.root, self.source, self.root / '.utmp/vn-image-import/row', ready=True)
        with self.assertRaises(ValueError):
            batch.plan(self.choices)

    def test_new_target_after_confirmation_invalidates_receipt(self):
        self.confirm()
        target = Path(verify(self.batch.work)['plan'][0]['target'])
        target.parent.mkdir(parents=True)
        target.write_bytes((self.source / 'b.png').read_bytes())
        with self.assertRaises(ValueError):
            verify(self.batch.work)

    def test_other_use_has_no_novel_table_mutations(self):
        self.choices[0].update(kind='other', otherUse='CG', scene='ending')
        item = self.batch.plan(self.choices)[0]
        self.assertEqual(item['tableRows'], [])
        self.assertIn('/CG/', item['resourcesPath'])

    def test_initial_suggestions_remain_draft(self):
        batch = Batch(self.root, self.source, self.root / '.utmp/vn-image-import/suggest',
                      suggestions=self.choices)
        self.assertEqual(batch.manifest()['draft'], self.choices)
        self.assertEqual(batch.state, 'pending')
        self.assertFalse((batch.work / 'decision.json').exists())
        self.assertNotEqual(batch.batch_id, self.batch.batch_id)


if __name__ == '__main__':
    unittest.main()
