import json
from pathlib import Path
import tempfile
import unittest
from PIL import Image
from confirmation import Batch, TABLES, resolve_table_paths


class ExternalSourcesTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name) / '中文 仓库'
        self.project = self.root / 'Program'
        self.design = self.root / 'Design/TableSources'
        self.art = self.root / 'Art'
        for path in (self.project / 'Assets', self.project / 'ProjectSettings', self.design, self.art):
            path.mkdir(parents=True)
        Image.new('RGB', (8, 8)).save(self.art / 'test.png')
        self.config = self.project / 'ProjectSettings/EmberTableSources.json'
        self.settings = dict(allowedRoots=['../Design/TableSources'], defaultRoot='../Design/TableSources',
                             bindings=[dict(tableId='novel_' + key, path='../Design/TableSources/' + key + '.tsv')
                                       for key in TABLES])
        for key, fields in TABLES.items():
            (self.design / (key + '.tsv')).write_text('\t'.join(fields) + '\n', encoding='utf-8')
        self.write_config()

    def write_config(self):
        self.config.write_text(json.dumps(self.settings), encoding='utf-8')

    def test_external_tsv_review_and_config_change_invalidates_batch(self):
        batch = Batch(self.project, self.art, self.project / '.utmp/vn-image-import/external', ready=True)
        self.assertEqual(set(batch.tables.values()), set(self.design.glob('*.tsv')))
        self.config.write_text(self.config.read_text() + '\n', encoding='utf-8')
        with self.assertRaises(ValueError):
            batch.check()

    def test_missing_external_does_not_fall_back(self):
        (self.design / 'characters.tsv').unlink()
        with self.assertRaises(ValueError):
            resolve_table_paths(self.project)

    def test_escape_absolute_duplicate_and_mapping_conflict_rejected(self):
        for value in ('../../escape.csv', str(self.design / 'characters.tsv'), '../Design/TableSources/../escape.csv'):
            self.settings['bindings'][0]['path'] = value
            self.write_config()
            with self.assertRaises(ValueError):
                resolve_table_paths(self.project)
        self.settings['bindings'][0]['path'] = '../Design/TableSources/characters.tsv'
        self.settings['bindings'].append(self.settings['bindings'][0])
        self.write_config()
        with self.assertRaises(ValueError):
            resolve_table_paths(self.project)


if __name__ == '__main__':
    unittest.main()
