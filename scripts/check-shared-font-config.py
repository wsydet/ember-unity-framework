#!/usr/bin/env python3
"""Static shared SDF policy check; does not simulate TMP packing or Unity rendering."""
import argparse
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]
REL = 'SharedAssets/Fonts/钉钉进步体/DingTalk-JinBuTi SDF.asset'
PATH = 'Packages/com.ember/' + REL
SYMBOL_REL = 'SharedAssets/Fonts/NotoSansSymbols2/NotoSansSymbols2-Regular SDF.asset'
SYMBOL_GUID = 'c7a84069be03475d8bbf927ef431d010'
SYMBOL_SOURCE_GUID = '618503a8ab374047ae01bc9cbb865d64'


def verify(asset, meta, source):
    text = asset.decode('utf-8-sig')
    for field, value in {
        'm_AtlasPopulationMode': '1', 'm_IsMultiAtlasTexturesEnabled': '1',
        'm_AtlasWidth': '1024', 'm_AtlasHeight': '1024', 'm_AtlasPadding': '9',
        'm_PointSize': '90', 'm_ClearDynamicDataOnBuild': '1',
        'm_SourceFontFileGUID': '3990ff594a20c3e42a78428e8b921683',
    }.items():
        found = re.findall(r'^\s+' + field + r': (\S+)\s*$', text, re.M)
        if found != [value]:
            raise ValueError(f'{source}: {field} expected {value}, found {found}')
    if 'guid: a32ba8ab7d4aa814e8b5f0a267b29b42' not in meta.decode('utf-8-sig'):
        raise ValueError(f'{source}: SDF GUID changed')
    if 'm_SourceFontFile: {fileID: 12800000, guid: 3990ff594a20c3e42a78428e8b921683, type: 3}' not in text:
        raise ValueError(f'{source}: source font reference changed')
    for reference in [
        'm_Material: {fileID: 5312024000564916581}',
        '--- !u!21 &5312024000564916581',
        '--- !u!28 &3406119624682658590',
        'm_Texture: {fileID: 3406119624682658590}',
        'm_Shader: {fileID: 4800000, guid: 68e6db2ebdc24f95958faec2be5558d6, type: 3}',
    ]:
        if reference not in text:
            raise ValueError(f'{source}: missing preserved material/texture reference {reference}')
    print(f'PASS {source}: Dynamic + Multi Atlas; source, SDF GUID, dimensions and material references preserved')


def verify_symbol_fallback(read, source):
    primary = read(REL).decode('utf-8-sig')
    symbol = read(SYMBOL_REL).decode('utf-8-sig')
    for text, field, expected in [
        (primary, 'm_FallbackFontAssetTable',
         r'\s*\n  - \{fileID: 11400000, guid: ' + SYMBOL_GUID + r', type: 2\}'),
        (symbol, 'm_AtlasPopulationMode', r' 1'),
        (symbol, 'm_IsMultiAtlasTexturesEnabled', r' 1'),
        (symbol, 'm_ClearDynamicDataOnBuild', r' 1'),
        (symbol, 'm_AtlasWidth', r' 1024'),
        (symbol, 'm_AtlasHeight', r' 1024'),
        (symbol, 'm_AtlasPadding', r' 9'),
        (symbol, 'm_PointSize', r' 90'),
        (symbol, 'm_AtlasRenderMode', r' 4165'),
        (symbol, 'm_SourceFontFileGUID', ' ' + SYMBOL_SOURCE_GUID),
        (symbol, 'm_SourceFontFile',
         r' \{fileID: 12800000, guid: ' + SYMBOL_SOURCE_GUID + r', type: 3\}'),
        (symbol, 'm_FallbackFontAssetTable', r' \[\]'),
    ]:
        if not re.search(r'^\s+' + field + ':' + expected + r'\s*$', text, re.M):
            raise ValueError(f'{source}: invalid symbol fallback field {field}')
    if f'guid: {SYMBOL_GUID}' not in read(SYMBOL_REL + '.meta').decode('utf-8-sig'):
        raise ValueError(f'{source}: symbol SDF GUID changed')
    font_meta = read('SharedAssets/Fonts/NotoSansSymbols2/NotoSansSymbols2-Regular.ttf.meta').decode('utf-8-sig')
    if f'guid: {SYMBOL_SOURCE_GUID}' not in font_meta or 'includeFontData: 1' not in font_meta:
        raise ValueError(f'{source}: symbol source GUID or embedded font data changed')
    for reference in [
        'm_Material: {fileID: 5312024000564916581}',
        '--- !u!21 &5312024000564916581',
        '--- !u!28 &3406119624682658590',
        'm_Texture: {fileID: 3406119624682658590}',
        'm_Shader: {fileID: 4800000, guid: 68e6db2ebdc24f95958faec2be5558d6, type: 3}',
    ]:
        if reference not in symbol:
            raise ValueError(f'{source}: missing symbol material/texture reference {reference}')
    license_text = read('SharedAssets/Fonts/NotoSansSymbols2/OFL.txt').decode('utf-8-sig')
    if 'SIL OPEN FONT LICENSE Version 1.1' not in license_text or 'The Noto Project Authors' not in license_text:
        raise ValueError(f'{source}: missing symbol font license')
    print(f'PASS {source}: bundled symbol font, fallback link, Dynamic + Multi Atlas, material and license')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--revision', default='HEAD')
    parser.add_argument('--package-root', type=Path)
    args = parser.parse_args()
    verify((ROOT/PATH).read_bytes(), (ROOT/(PATH+'.meta')).read_bytes(), 'workspace')
    verify_symbol_fallback(lambda path: (ROOT/'Packages/com.ember'/path).read_bytes(), 'workspace')
    prefix = ':' if args.revision == ':' else args.revision + ':'
    def blob(path):
        return subprocess.check_output(['git', 'show', prefix+path], cwd=ROOT)
    verify(blob(PATH), blob(PATH+'.meta'), 'Git '+args.revision)
    verify_symbol_fallback(lambda path: blob('Packages/com.ember/' + path), 'Git '+args.revision)
    if args.package_root:
        verify((args.package_root/REL).read_bytes(), (args.package_root/(REL+'.meta')).read_bytes(), 'consumer')
        verify_symbol_fallback(lambda path: (args.package_root/path).read_bytes(), 'consumer')
    print('Static configuration only: execute SharedFontAtlasPlayModeTests and visually accept SceneUI in Unity.')


if __name__ == '__main__':
    main()
