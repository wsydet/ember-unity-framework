#!/usr/bin/env python3
"""Static shared SDF policy check; does not simulate TMP packing or Unity rendering."""
import argparse
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]
REL = 'SharedAssets/Fonts/钉钉进步体/DingTalk-JinBuTi SDF.asset'
PATH = 'Packages/com.ember/' + REL


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


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--revision', default='HEAD')
    parser.add_argument('--package-root', type=Path)
    args = parser.parse_args()
    verify((ROOT/PATH).read_bytes(), (ROOT/(PATH+'.meta')).read_bytes(), 'workspace')
    prefix = ':' if args.revision == ':' else args.revision + ':'
    def blob(path):
        return subprocess.check_output(['git', 'show', prefix+path], cwd=ROOT)
    verify(blob(PATH), blob(PATH+'.meta'), 'Git '+args.revision)
    if args.package_root:
        verify((args.package_root/REL).read_bytes(), (args.package_root/(REL+'.meta')).read_bytes(), 'consumer')
    print('Static configuration only: execute SharedFontAtlasPlayModeTests and visually accept SceneUI in Unity.')


if __name__ == '__main__':
    main()
