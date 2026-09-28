"""Isolated filesystem regression checks; no Unity or real project writes."""
import argparse
import contextlib
import io
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import recovery_snapshot as recovery


class SnapshotTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.project = self.root / 'project'
        self.old = self.root / 'old'
        self.new = self.root / 'new'
        self.local = self.project / 'Assets'
        for path in (self.old, self.new, self.local):
            path.mkdir(parents=True)
        self.batch = self.project / '.utmp/recovery/batch'

    def put(self, side, path, content):
        target = side / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(content)

    def run_snapshot(self):
        with contextlib.redirect_stdout(io.StringIO()):
            return recovery.snapshot(argparse.Namespace(
                project=str(self.project), old_assets=str(self.old),
                new_assets=str(self.new), batch=str(self.batch)))

    def test_categories_and_binary_meta_and_local_deletion(self):
        cases = {
            'same': (b'a', b'b', b'b'),
            'template': (b'a', b'b', b'a'),
            'local': (b'a', b'a', b'b'),
            'conflict': (b'a', b'b', b'c'),
            'local-deleted': (b'a', b'a', None),
            'template-deleted': (b'a', None, b'a'),
            'local-added': (None, None, b'\x00\xff'),
            'add-collision': (None, b'a', b'b')}
        for name, values in cases.items():
            for side, value in zip((self.old, self.new, self.local), values):
                if value is not None:
                    self.put(side, 'Game/' + name, value)
        self.put(self.local, 'Game.meta', b'guid: original-root')
        self.put(self.local, 'Game/Empty.meta', b'guid: empty-dir')
        (self.local / 'Game/Empty').mkdir()
        self.run_snapshot()
        self.assertTrue(recovery.verify(self.batch, True)['verified'])
        result = recovery.listing(argparse.Namespace(batch=str(self.batch), category=None, limit=200, offset=0))
        categories = {item['path']: item['category'] for item in result['items']}
        for name, category in {'same': 'same', 'template': 'template', 'local': 'local',
                               'conflict': 'review', 'local-deleted': 'local',
                               'template-deleted': 'template', 'local-added': 'local',
                               'add-collision': 'review'}.items():
            self.assertEqual(categories['Game/' + name], category)
        self.assertEqual((self.batch / 'L/Game/local-added').read_bytes(), b'\x00\xff')
        self.assertEqual((self.batch / 'L/Game.meta').read_bytes(), b'guid: original-root')
        self.assertTrue((self.batch / 'L/Game/Empty').is_dir())

    def test_source_edits_and_backup_corruption_detected(self):
        self.put(self.local, 'Game/file', b'original')
        self.run_snapshot()
        self.put(self.local, 'Game/file', b'new user work')
        self.assertTrue(recovery.verify(self.batch)['verified'])
        with self.assertRaises(ValueError):
            recovery.verify(self.batch, True)
        self.put(self.batch / 'L', 'Game/file', b'corrupt')
        with self.assertRaises(ValueError):
            recovery.verify(self.batch)

    def test_source_addition_and_missing_directory_detected(self):
        self.run_snapshot()
        self.put(self.local, 'Game/new', b'new')
        with self.assertRaises(ValueError):
            recovery.verify(self.batch, True)
        (self.batch / 'O').rmdir()
        with self.assertRaises(ValueError):
            recovery.verify(self.batch)

    def test_refuses_existing_batch_and_overlapping_destination(self):
        self.run_snapshot()
        with self.assertRaises(FileExistsError):
            self.run_snapshot()
        self.batch = self.local / 'Game/backup'
        with self.assertRaises(ValueError):
            self.run_snapshot()
        self.assertFalse(self.batch.exists())

    def test_insufficient_space_leaves_incomplete_batch(self):
        self.put(self.local, 'Game/file', b'contents')
        with patch.object(recovery.shutil, 'disk_usage', return_value=argparse.Namespace(free=0)):
            with self.assertRaises(ValueError):
                self.run_snapshot()
        with self.assertRaises(ValueError):
            recovery.verify(self.batch)
        self.assertEqual((self.local / 'Game/file').read_bytes(), b'contents')

    def test_pagination_and_path_type_conflict(self):
        self.put(self.old, 'Game/path', b'old')
        self.put(self.new, 'Game/path/nested', b'new')
        self.put(self.local, 'Game/path', b'local')
        for i in range(250):
            self.put(self.local, f'Game/files/{i:04}', b'data')
        self.run_snapshot()
        result = recovery.listing(argparse.Namespace(batch=str(self.batch), category='review', limit=20, offset=0))
        self.assertEqual(result['items'][0]['path'], 'Game/path')
        a = recovery.listing(argparse.Namespace(batch=str(self.batch), category=None, limit=20, offset=0))
        b = recovery.listing(argparse.Namespace(batch=str(self.batch), category=None, limit=20, offset=20))
        self.assertEqual(len(a['items']), 20)
        self.assertFalse({v['path'] for v in a['items']} & {v['path'] for v in b['items']})

    def test_link_refused(self):
        link = self.local / 'Game'
        try:
            link.symlink_to(self.old, target_is_directory=True)
        except OSError:
            self.skipTest('Symlink creation unavailable on this host')
        with self.assertRaises(ValueError):
            self.run_snapshot()


if __name__ == '__main__':
    unittest.main()
