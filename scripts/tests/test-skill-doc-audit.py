"""Exercise the shipped document audit in isolated Unity consumer/framework fixtures."""
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / '.agents/skills/ember-doc-maintenance/scripts/audit_docs.py'
spec = importlib.util.spec_from_file_location('audit_docs', SCRIPT)
audit_docs = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit_docs)


class DocumentOwnershipTests(unittest.TestCase):
    def setUp(self):
        (ROOT / '.utmp').mkdir(exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(prefix='skill-audit-', dir=ROOT / '.utmp')
        self.root = Path(self.temp.name).resolve()
        subprocess.run(['git', 'init', '-q', str(self.root)], check=True)
        self.write('Packages/manifest.json', '{"dependencies":{}}')
        self.write('Packages/com.ember/package.json', '{"name":"com.ember"}')
        self.write('Packages/com.ember/README.md', '[package error](missing-package.md)')
        self.write('Packages/com.vendor/README.md', '[vendor](missing-vendor.md)')
        self.write('Packages/com.ember/UniTask/README.md', '[vendor](missing.md)')
        self.write('Packages/com.ember/Templates~/base/README.md', '[snapshot](missing.md)')
        self.write('docs/中文 空格.md', 'Existing destination')
        self.write('docs/README.md', '[good](<中文 空格.md>)\n[encoded](%E4%B8%AD%E6%96%87%20%E7%A9%BA%E6%A0%BC.md)\n'
                   '[bad](missing-project.md)\n```md\n[example](not-real.md)\n```\n')
        self.write('Assets/VendorCustom/README.md', '[external](missing.md)')

    def tearDown(self):
        self.temp.cleanup()

    def write(self, name, content):
        path = self.root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding='utf-8-sig')

    def test_consumer_preserves_dependency_and_snapshot_documents(self):
        result = audit_docs.audit(self.root, exclude_dirs=('Assets/VendorCustom',))
        self.assertEqual([x['target'] for x in result['issues']], ['missing-project.md'])
        categories = {x['path']: x['category'] for x in result['documents']}
        self.assertEqual(categories['Packages/com.ember/README.md'], 'preserve_history_or_vendor')
        self.assertEqual(categories['Packages/com.ember/Templates~/base/README.md'], 'template_snapshot')

    def test_framework_checks_own_package_but_preserves_vendors(self):
        result = audit_docs.audit(self.root, 'framework', ('Assets/VendorCustom',))
        self.assertEqual({x['target'] for x in result['issues']}, {'missing-project.md', 'missing-package.md'})

    def test_git_fallback_matches_rg_inventory(self):
        expected = audit_docs.audit(self.root)
        with patch.object(audit_docs.shutil, 'which', return_value=None):
            actual = audit_docs.audit(self.root)
        self.assertEqual(actual, expected)

    def test_strict_cli_and_default_mode(self):
        report = self.root / '.utmp/report.json'
        command = [sys.executable, str(SCRIPT), '--root', str(self.root), '--strict',
                   '--exclude-dir', 'Assets/VendorCustom', '--report', str(report)]
        result = subprocess.run(command, capture_output=True)
        self.assertEqual(result.returncode, 1, result.stderr)
        self.assertEqual(json.loads(report.read_text(encoding='utf-8'))['mode'], 'consumer')
        self.write('docs/missing-project.md', 'Fixed target')
        self.assertEqual(subprocess.run(command, capture_output=True).returncode, 0)

    def test_framework_mode_requires_owned_package(self):
        (self.root / 'Packages/com.ember/package.json').unlink()
        result = subprocess.run([sys.executable, str(SCRIPT), '--root', str(self.root),
                                 '--mode', 'framework'], capture_output=True)
        self.assertEqual(result.returncode, 2)

    def test_rejects_exclusion_outside_project(self):
        result = subprocess.run([sys.executable, str(SCRIPT), '--root', str(self.root),
                                 '--exclude-dir', '../outside'], capture_output=True)
        self.assertEqual(result.returncode, 2)


if __name__ == '__main__':
    unittest.main()
