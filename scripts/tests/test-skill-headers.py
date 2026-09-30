import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('headers', Path(__file__).resolve().parents[1] / 'check-skill-headers.py')
headers = importlib.util.module_from_spec(spec)
spec.loader.exec_module(headers)


class SkillHeaderTests(unittest.TestCase):
    def test_valid_line_endings_are_not_modified(self):
        for newline in ('\n', '\r\n'):
            data = newline.join(['---', 'name: fixture', 'description: 中文说明', '---', '正文', '']).encode()
            headers.validate(data, 'fixture')

    def test_invalid_bytes_and_headers(self):
        good = b'---\nname: fixture\ndescription: test\n---\nbody'
        for data in (b'\xef\xbb\xbf' + good, b'\xff' + good, b'\n' + good,
                     good.replace(b'\n---', b'\n--'),
                     good.replace(b'name: fixture', b'name: other'),
                     good.replace(b'name: fixture', b'name: fixture\nname: other'),
                     good.replace(b'description: test', b'description: '),
                     good.replace(b'description: test\n', b'')):
            with self.subTest(data=data):
                with self.assertRaises((ValueError, UnicodeError)):
                    headers.validate(data, 'fixture')


if __name__ == '__main__':
    unittest.main()
