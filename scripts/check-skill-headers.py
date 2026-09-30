"""Read-only audit of shipped/authored SKILL.md encoding and frontmatter envelope.

This checks Ember's required header fields, not the complete YAML language.
Run before template Save/Bump and before release; no files are normalized.
"""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]


def validate(data, skill_id):
    text = data.decode('utf-8', errors='strict')
    if text.startswith('\ufeff'):
        raise ValueError('UTF-8 BOM is forbidden')
    header = re.match(r'\A---\r?\n([\s\S]*?)\r?\n---(?:\r?\n|$)', text)
    if not header:
        raise ValueError('YAML frontmatter must start at byte zero and have a closing ---')
    fields = header[1]
    if len(re.findall(r'^name:', fields, re.M)) != 1 or not re.search(
            r'^name:[ \t]*' + re.escape(skill_id) + r'[ \t]*\r?$', fields, re.M):
        raise ValueError('frontmatter requires one name matching the skill directory')
    if len(re.findall(r'^description:', fields, re.M)) != 1 or not re.search(
            r'^description:[ \t]*[^\s\r\n][^\r\n]*\r?$', fields, re.M):
        raise ValueError('frontmatter requires one nonempty description')


def main():
    roots = [ROOT / 'Assets/Game/Documentation/TemplateSkills',
             ROOT / 'Packages/com.ember/Templates~',
             ROOT / '.agents/skills', ROOT / 'Packages/com.ember/AISkills~/skills']
    paths = sorted({p for root in roots if root.exists() for p in root.rglob('SKILL.md')})
    errors = []
    for path in paths:
        try:
            validate(path.read_bytes(), path.parent.name)
        except (UnicodeError, ValueError) as error:
            errors.append(f'{path.relative_to(ROOT)}: {error}')
    print(f'Checked {len(paths)} skill headers; {len(errors)} errors.')
    for error in errors:
        print(error)
    return bool(errors)


if __name__ == '__main__':
    sys.exit(main())
