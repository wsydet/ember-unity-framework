from pathlib import Path
root = Path('Packages/com.ember')
s = Path(__file__).with_name('implement_io.py').read_text(encoding='utf-8')
exec(s[s.index("p=root/'UPMManager/Editor/EmberAISkillInstaller.cs'"):])
