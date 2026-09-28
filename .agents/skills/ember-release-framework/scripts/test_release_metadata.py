import contextlib
import io
import json
from pathlib import Path
import tempfile
import unittest
from release_metadata import generate, emit


class ReleaseMetadataTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.pkg = self.root / 'Packages/com.ember'
        for marker in ('CLAUDE.md', 'scripts/upm-bump-version.ps1', 'docs/dev/upm-migration-plan.md'):
            path = self.root / marker
            path.parent.mkdir(parents=True, exist_ok=True)
            path.touch()
        self.put(self.pkg / 'package.json', {'name': 'com.ember', 'version': '1.2.1'})
        self.put(self.root / '.agents/skills/catalog.json', {'skills': [{'id': 'skill'}]})
        self.put(self.pkg / 'AISkills~/bundle.json', {'sourceCommit': 'a' * 40})
        self.put(self.pkg / 'Dependencies~/release-1.2.0.json', {
            'schemaVersion': 1, 'frameworkVersion': '1.2.0', 'frameworkTag': 'v1.2.0',
            'validation': 'OLD TESTS PASSED', 'aiSkills': {'skills': ['skill']}})
        self.put(self.pkg / 'Dependencies~/manifest-1.2.0.json', {
            'dependencies': {'com.ember': 'https://example/repo.git?path=/Packages/com.ember#v1.2.0',
                             'third-party': 'unchanged'}})
        self.base = {'id': 'base', 'version': '2.0.0', 'frameworkVersion': '1.2.0',
                     'contentHash': 'hash', 'versionedContentHash': 'hash'}
        self.put(self.pkg / 'Templates~/base/template.json', self.base)

    def put(self, path, data):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(data), encoding='utf-8')

    def generate(self, validation='Current checks pending'):
        return generate(self.root, '1.2.0', '1.2.1', 'Current changes', validation, 'prepared')

    def test_preview_preserves_files_and_requires_current_evidence(self):
        before = set(self.root.rglob('*'))
        outputs = self.generate()
        with contextlib.redirect_stdout(io.StringIO()):
            emit(outputs)
        self.assertEqual(before, set(self.root.rglob('*')))
        release, manifest = outputs.values()
        self.assertEqual(release['validation'], 'Current checks pending')
        self.assertEqual(manifest['dependencies']['third-party'], 'unchanged')
        self.assertTrue(manifest['dependencies']['com.ember'].endswith('#v1.2.1'))
        with self.assertRaises(ValueError):
            self.generate('  ')

    def test_write_is_resumable_and_checks_both_before_writing(self):
        outputs = self.generate()
        paths = list(outputs)
        self.put(paths[1], {'conflict': True})
        with self.assertRaises(ValueError):
            emit(outputs, True)
        self.assertFalse(paths[0].exists())
        paths[1].unlink()
        with contextlib.redirect_stdout(io.StringIO()):
            emit(outputs, True)
            emit(outputs, True)
        self.assertEqual(json.loads(paths[0].read_text()), outputs[paths[0]])

    def test_unsealed_template_rejected(self):
        self.base['contentHash'] = 'dirty'
        self.put(self.pkg / 'Templates~/base/template.json', self.base)
        with self.assertRaisesRegex(ValueError, 'Unsealed'):
            self.generate()

    def test_parent_missing_stale_and_cycle_rejected(self):
        child = dict(self.base, id='child', parentId='absent', parentVersion='2.0.0', parentContentHash='hash')
        path = self.pkg / 'Templates~/child/template.json'
        self.put(path, child)
        with self.assertRaisesRegex(ValueError, 'Missing'):
            self.generate()
        child.update(parentId='base', parentContentHash='old')
        self.put(path, child)
        with self.assertRaisesRegex(ValueError, 'Stale'):
            self.generate()
        child.update(parentId='child', parentContentHash='hash')
        self.put(path, child)
        with self.assertRaisesRegex(ValueError, 'cyclic'):
            self.generate()

    def test_wrong_checkout_and_package_version_rejected(self):
        self.put(self.pkg / 'package.json', {'name': 'com.ember', 'version': '1.2.0'})
        with self.assertRaisesRegex(ValueError, 'package version'):
            self.generate()
        (self.root / 'CLAUDE.md').unlink()
        with self.assertRaisesRegex(ValueError, 'source checkout'):
            self.generate()

    def test_minor_compatibility_and_catalog_changes_rejected(self):
        self.base['frameworkVersion'] = '1.1.0'
        self.put(self.pkg / 'Templates~/base/template.json', self.base)
        with self.assertRaisesRegex(ValueError, 'compatibility'):
            self.generate()
        self.base['frameworkVersion'] = '1.2.0'
        self.put(self.pkg / 'Templates~/base/template.json', self.base)
        self.put(self.root / '.agents/skills/catalog.json', {'skills': []})
        with self.assertRaisesRegex(ValueError, 'catalog changed'):
            self.generate()


if __name__ == '__main__':
    unittest.main()
