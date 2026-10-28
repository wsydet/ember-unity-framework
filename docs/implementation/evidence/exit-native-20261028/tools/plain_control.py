"""Existing idle framework player, paired UIA observation, normal WM_CLOSE only."""
import ctypes as C
from ctypes import wintypes as W
import json
from pathlib import Path
import subprocess
import time

u=C.WinDLL('user32',use_last_error=True)
Callback=C.WINFUNCTYPE(W.BOOL,W.HWND,C.c_ssize_t)
u.EnumWindows.argtypes=[Callback,C.c_ssize_t]
u.GetWindowThreadProcessId.argtypes=[W.HWND,C.POINTER(W.DWORD)]
u.PostMessageW.argtypes=[W.HWND,W.UINT,C.c_size_t,C.c_ssize_t]
def windows(pid):
    found=[]
    @Callback
    def cb(hwnd,data):
        owner=W.DWORD();u.GetWindowThreadProcessId(hwnd,C.byref(owner))
        if owner.value==pid:found.append(hwnd)
        return True
    u.EnumWindows(cb,0)
    return found

base=Path('docs/implementation/evidence/exit-native-20261028').resolve()
exe=Path('Builds/B03/Protected/EmberRegression.exe').resolve()
startup=subprocess.STARTUPINFO()
for name,query in [('idle-visible-no-uia-1',False),('idle-visible-uia-once-1',True),('idle-visible-no-uia-2',False),('idle-visible-uia-once-2',True)]:
    root=base/name;root.mkdir(exist_ok=False)
    cmd=[str(exe),'-force-d3d11','-screen-width','960','-screen-height','540','-screen-fullscreen','0','-logFile',str(root/'player.log')]
    p=subprocess.Popen(cmd,startupinfo=startup);start=time.monotonic()
    while not windows(p.pid) and p.poll() is None and time.monotonic()-start<75:time.sleep(.1)
    time.sleep(3)
    worker=None
    if query:
        worker=subprocess.Popen([r'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe','-NoProfile','-ExecutionPolicy','Bypass','-File',str(Path(__file__).with_name('uia_probe.ps1').resolve()),'-TargetPid',str(p.pid),'-OutputRoot',str(root),'-MaxQueries','1'],startupinfo=startup)
        worker.wait(timeout=45)
    else:time.sleep(3)
    owned=windows(p.pid)
    posts=[{'hwnd':hex(h),'success':bool(u.PostMessageW(h,0x10,0,0))} for h in owned]
    try:code=p.wait(timeout=20)
    except subprocess.TimeoutExpired:code=None
    result={'pid':p.pid,'command':cmd,'uia':query,'querySucceeded':(root/'uia-ready.txt').exists(),'uiaWorkerExitedBeforeClose':worker.poll() is not None if worker else None,'wmClose':posts,'exitCode':code,'exitHex':hex(code&0xffffffff) if code is not None else None,'seconds':time.monotonic()-start,'debugger':False,'forcedTermination':False}
    (root/'process.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps({'case':name,**result}),flush=True)
    if code is None:break
