"""Read-only structural audit of complete lark-cli +csv-get snapshots.
Does not infer event boundaries, import Unity assets, or contact Feishu.
"""
import argparse
import csv
import hashlib
import io
import json
import re
from pathlib import Path


def read_sheet(path, metadata):
    raw = path.read_bytes()
    data = json.loads(raw.decode('utf-8-sig'))
    warning = data.get('warning_message', '')
    if data.get('has_more') or data.get('truncated'):
        raise ValueError(f'{path.name}: truncated read; {warning}')
    rows, columns = data['row_indices'], data['col_indices']
    if rows != list(range(1, metadata['row_count'] + 1)):
        raise ValueError(f'{path.name}: incomplete physical row coverage')
    def col(number):
        result = ''
        while number:
            number, digit = divmod(number - 1, 26)
            result = chr(65 + digit) + result
        return result
    expected_columns = [col(i) for i in range(1, metadata['column_count'] + 1)]
    if columns != expected_columns or data.get('actual_range') != f'A1:{columns[-1]}{rows[-1]}':
        raise ValueError(f'{path.name}: incomplete column/range coverage')
    source = io.StringIO(data['annotated_csv'], newline='')
    nonempty = []
    codes = {}
    for row in rows:
        prefix = f'[row={row}] '
        if source.read(len(prefix)) != prefix:
            raise ValueError(f'{path.name}: row prefix mismatch at {row}')
        # Parse one logical record at a time. Embedded newlines and [row=N] text stay in the cell.
        values = next(csv.reader(source, strict=True))
        if len(values) != len(columns):
            raise ValueError(f'{path.name}: column count mismatch at {row}')
        cells = {column: value for column, value in zip(columns, values) if value.strip()}
        if cells:
            nonempty.append({'row': row, 'cells': cells})
        candidate = values[0].strip()
        if re.fullmatch(r'[A-Z]\d{3}', candidate):
            codes.setdefault(candidate, []).append(row)
    if source.read().strip():
        raise ValueError(f'{path.name}: unaccounted trailing data')
    return dict(id=metadata['sheet_id'], name=metadata['sheet_name'], range=data['actual_range'],
                revision=data.get('revision'), sha256=hashlib.sha256(raw).hexdigest(),
                nonemptyCount=len(nonempty), lastNonempty=nonempty[-1]['row'] if nonempty else 0,
                duplicateCandidateCodes={key: value for key, value in codes.items() if len(value) > 1},
                isolatedCodeRows=[r['row'] for r in nonempty if len(r['cells']) == 1 and re.fullmatch(r'[A-Z]\d{3}', r['cells'].get('A','').strip())],
                rows=nonempty)


def audit(folder):
    book = json.loads((folder / 'workbook.json').read_text(encoding='utf-8-sig'))
    if book.get('ok') is not True:
        raise ValueError('workbook request failed')
    metadata = book['data']
    sheets = [read_sheet(folder / (sheet['sheet_id'] + '.json'), sheet) for sheet in metadata['sheets']]
    if any(sheet['revision'] != metadata['revision'] for sheet in sheets):
        raise ValueError('workbook changed during read; refresh snapshots')
    return dict(mode='audit-only', imported=False, title=metadata['title'], revision=metadata['revision'], sheets=sheets)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('snapshot_directory', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = audit(args.snapshot_directory)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps([{k: v for k, v in s.items() if k != 'rows'} for s in result['sheets']], ensure_ascii=False, indent=2))
