"""Bounded external debugger. Observes exceptions; never forces a normal exit."""
import argparse
import ctypes as C
from ctypes import wintypes as W
import json
import msvcrt
from pathlib import Path
import struct
import subprocess
import time
from win_debug import *

class ExceptionRecord(C.Structure):
    _fields_=[('code',W.DWORD),('flags',W.DWORD),('record',PTR),('address',PTR),
              ('count',W.DWORD),('information',C.c_size_t*15)]
class ExceptionInfo(C.Structure):
    _fields_=[('record',ExceptionRecord),('first',W.DWORD)]
class CreateProcessInfo(C.Structure):
    _fields_=[('file',W.HANDLE),('process',W.HANDLE),('thread',W.HANDLE),('base',PTR),
              ('debugOffset',W.DWORD),('debugSize',W.DWORD),('tls',PTR),('start',PTR),('name',PTR),('unicode',W.WORD)]
class LoadDllInfo(C.Structure):
    _fields_=[('file',W.HANDLE),('base',PTR),('debugOffset',W.DWORD),('debugSize',W.DWORD),('name',PTR),('unicode',W.WORD)]
class EventData(C.Union):
    _fields_=[('exception',ExceptionInfo),('create',CreateProcessInfo),('dll',LoadDllInfo),('exitCode',W.DWORD),('raw',C.c_byte*160)]
class DebugEvent(C.Structure):
    _fields_=[('code',W.DWORD),('pid',W.DWORD),('tid',W.DWORD),('data',EventData)]
class ExceptionPointers(C.Structure):
    _fields_=[('record',PTR),('context',PTR)]
class DumpException(C.Structure):
    _pack_=4  # minidumpapiset.h is under pshpack4, including this API structure.
    _fields_=[('thread',W.DWORD),('pointers',PTR),('clientPointers',W.BOOL)]

WaitEvent=bind(K,'WaitForDebugEvent',W.BOOL,C.POINTER(DebugEvent),W.DWORD)
ContinueEvent=bind(K,'ContinueDebugEvent',W.BOOL,W.DWORD,W.DWORD,W.DWORD)
SetKill=bind(K,'DebugSetProcessKillOnExit',W.BOOL,W.BOOL)
Detach=bind(K,'DebugActiveProcessStop',W.BOOL,W.DWORD)
Dump=bind(D,'MiniDumpWriteDump',W.BOOL,W.HANDLE,W.DWORD,W.HANDLE,W.DWORD,PTR,PTR,PTR)

parser=argparse.ArgumentParser()
parser.add_argument('--name',required=True)
parser.add_argument('--exe',default=r'C:\Users\wuyu\My\Windward\Builds\Windows64\B02-0242\Windward.exe')
parser.add_argument('--timeout',type=float,default=100)
parser.add_argument('--delay',type=float,default=0)
parser.add_argument('--trace-window',action='store_true')
parser.add_argument('--uia',action='store_true')
parser.add_argument('--uia-once',action='store_true')
parser.add_argument('--idle-framework',action='store_true',help='Existing Boot, without regression output opt-in; close with normal WM_CLOSE')
parser.add_argument('--second-close-after',type=float,default=15)
args=parser.parse_args()
if args.idle_framework:args.exe=str(Path('Builds/B03/Protected/EmberRegression.exe').resolve())
root=(Path('docs/implementation/evidence/exit-native-20261028')/args.name).resolve()
root.mkdir(parents=True,exist_ok=False)
search=r'C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\win64_player_development_mono'
command=[args.exe,'-force-d3d11','-screen-width','960','-screen-height','540','-screen-fullscreen','0',
         '-windward-authoring-probe','-windward-s8-root',str(root),'-windward-s8-driver','-windward-b02-observe','-logFile',str(root/'player.log')]
if args.idle_framework:command=command[:8]+['-logFile',str(root/'player.log')]
startup=subprocess.STARTUPINFO()
startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW
startup.wShowWindow=0
p=subprocess.Popen(command,creationflags=2,startupinfo=startup)
SetKill(False)
process=W.HANDLE(int(p._handle))
symbols=Symbols(process,search)
start=time.monotonic()
unity=0
initial_break=False
sent=False
ready_at=None
sent_at=None
close_retried=False
exceptions={}
captures=[]
breakpoints={}
pending_steps={}
hits={}
worker=None
main_tid=None
events_file=(root/'native-events.jsonl').open('w',encoding='utf-8',buffering=1)

def emit(kind,**data):
    event={'kind':kind,'seconds':time.monotonic()-start,'unixTime':time.time(),**data}
    events_file.write(json.dumps(event,ensure_ascii=False)+'\n')
    if kind in ['capture','exit','command-sent','timeout','uia-start','trace-hit']:
        print(json.dumps(event,ensure_ascii=False),flush=True)

def owned_windows():
    user=C.WinDLL('user32')
    callback_type=C.WINFUNCTYPE(W.BOOL,W.HWND,PTR)
    owner_fn=bind(user,'GetWindowThreadProcessId',W.DWORD,W.HWND,C.POINTER(W.DWORD))
    found=[]
    @callback_type
    def cb(hwnd,data):
        owner=W.DWORD();owner_fn(hwnd,C.byref(owner))
        if owner.value==p.pid:found.append(hwnd)
        return True
    bind(user,'EnumWindows',W.BOOL,callback_type,PTR)(cb,None)
    return found

def screen_state():
    raw=read(process,unity+0x52185c0,8) if unity else None
    manager=struct.unpack('<Q',raw)[0] if raw and len(raw)==8 else None
    window=read(process,manager+0x138,8) if manager else None
    return {'screenManager':hex(manager) if manager is not None else None,
            'window':hex(struct.unpack('<Q',window)[0]) if window and len(window)==8 else None}

def phase():
    path=root/'player.log'
    if not path.exists():return {}
    with path.open('rb') as f:
        f.seek(max(0,path.stat().st_size-40000));tail=f.read().decode(errors='replace')
    return {'quitRequested':'Quit: Application.Quit() requested' in tail,
            'inputStopped':'Input System module state changed to: Shutdown.' in tail,
            'playerConnectionCleanup':'PlayerConnection::Cleanup' in tail,'logTail':tail[-2500:]}

def capture(event,kind,with_dump=False):
    thread=OpenThread(0x48,False,event.tid)
    if not thread:
        emit('capture-error',error=C.get_last_error(),tid=event.tid);return
    try:
        buffer,ctx=context(thread)
        regs=registers(ctx)
        frames=stack(symbols,thread,ctx)
        record=event.data.exception.record
        item={'kind':kind,'tid':event.tid,'firstChance':bool(event.data.exception.first),
              'code':hex(record.code),'exceptionAddress':hex(record.address or 0),
              'information':[hex(record.information[i]) for i in range(min(record.count,15))],
              'registers':{k:hex(v) for k,v in regs.items()},'screen':screen_state(),'frames':frames,'phase':phase()}
        number=len(captures)
        if with_dump:
            pointers=ExceptionPointers(C.addressof(record),ctx)
            info=DumpException(event.tid,C.addressof(pointers),False)
            dump_path=root/f'exception-{number}.dmp'
            with dump_path.open('w+b') as target:
                ok=Dump(process,event.pid,W.HANDLE(msvcrt.get_osfhandle(target.fileno())),0x1861,C.byref(info),None,None)
                item['dump']={'path':dump_path.name,'success':bool(ok),'error':0 if ok else C.get_last_error()}
        captures.append(item)
        (root/f'capture-{number}.json').write_text(json.dumps(item,indent=2,ensure_ascii=False),encoding='utf-8')
        emit('capture',index=number,tid=event.tid,firstChance=item['firstChance'],code=item['code'],
             address=item['exceptionAddress'],screen=item['screen'],top=frames[:8],phase={k:v for k,v in item['phase'].items() if k!='logTail'})
    finally:CloseHandle(thread)

try:
    emit('launch',pid=p.pid,command=command,trace=args.trace_window,uia=args.uia or args.uia_once,uiaOnce=args.uia_once,idleFramework=args.idle_framework)
    (root/'pid.txt').write_text(str(p.pid))
    while time.monotonic()-start<args.timeout:
        ready=bool(owned_windows()) if args.idle_framework and not sent else (root/'authoring-entry.json').exists()
        if not sent and ready:
            if ready_at is None:ready_at=time.monotonic()
            if (args.uia or args.uia_once) and worker is None:
                worker_command=[r'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe','-NoProfile','-ExecutionPolicy','Bypass',
                                 '-File',str(Path(__file__).with_name('uia_probe.ps1').resolve()),'-TargetPid',str(p.pid),'-OutputRoot',str(root)]
                if args.uia_once:worker_command+=['-MaxQueries','1']
                worker=subprocess.Popen(worker_command,startupinfo=startup)
                emit('uia-start',pid=worker.pid)
            uia_ready=not(args.uia or args.uia_once) or (root/'uia-ready.txt').exists()
            worker_done=not args.idle_framework or worker is None or worker.poll() is not None
            if uia_ready and worker_done and time.monotonic()-ready_at>=max(args.delay,3 if args.idle_framework else 0):
                if args.idle_framework:
                    hwnd=owned_windows()[0]
                    post=bind(C.WinDLL('user32'),'PostMessageW',W.BOOL,W.HWND,W.UINT,C.c_size_t,C.c_ssize_t)
                    emit('normal-close',hwnd=hex(hwnd),success=bool(post(hwnd,0x10,0,0)),workerExited=worker.poll() is not None if worker else None)
                else:
                    (root/'delta-command.json').write_bytes(Path(__file__).with_name('input-command.json').read_bytes())
                sent=True;sent_at=time.monotonic();emit('command-sent')
        if sent and not close_retried and time.monotonic()-sent_at>args.second_close_after and phase().get('quitRequested'):
            callback_type=C.WINFUNCTYPE(W.BOOL,W.HWND,PTR)
            owner_fn=bind(C.WinDLL('user32'),'GetWindowThreadProcessId',W.DWORD,W.HWND,C.POINTER(W.DWORD))
            post=bind(C.WinDLL('user32'),'PostMessageW',W.BOOL,W.HWND,W.UINT,C.c_size_t,C.c_ssize_t)
            windows=[]
            @callback_type
            def find_window(hwnd,user):
                owner=W.DWORD();owner_fn(hwnd,C.byref(owner))
                if owner.value==p.pid:windows.append(hwnd)
                return True
            bind(C.WinDLL('user32'),'EnumWindows',W.BOOL,callback_type,PTR)(find_window,None)
            if windows:
                post(windows[0],0x10,0,0)
                close_retried=True
                emit('normal-close-retry',hwnd=hex(windows[0]),phase={k:v for k,v in phase().items() if k!='logTail'})
        event=DebugEvent()
        if not WaitEvent(C.byref(event),50):continue
        status=0x10002
        if event.code==3:
            main_tid=event.tid
            if event.data.create.file:CloseHandle(event.data.create.file)
            emit('process-created',pid=event.pid,tid=event.tid)
        elif event.code==6:
            dll=event.data.dll
            path=file_path(dll.file) if dll.file else None
            if path:
                symbols.load(path,dll.base,dll.file)
                emit('module-load',base=hex(dll.base),path=path)
                if Path(path).name.lower()=='unityplayer.dll':
                    unity=dll.base
                    symbols.enum(unity,'*GetScreenManager*',10)
                    emit('unity-symbols',fault=symbols.resolve(unity+0x1dd2e9))
                    if args.trace_window:
                        for name,rva in [('window-query',0x1dd2e0),('screen-dtor',0x22fc0a0),('screen-win-dtor',0x1a22520),('gfx-destroy',0x1b82780)]:
                            address=unity+rva
                            original=read(process,address,1)
                            if not original:raise RuntimeError('Cannot read breakpoint '+name)
                            write(process,address,b'\xcc')
                            breakpoints[address]=(name,original)
                            hits[address]=0
            if dll.file:CloseHandle(dll.file)
        elif event.code==1:
            exception=event.data.exception
            code=exception.record.code
            address=exception.record.address or 0
            exceptions[hex(code)]=exceptions.get(hex(code),0)+1
            status=0x80010001
            if code==0x80000003 and address in breakpoints:
                name,original=breakpoints[address]
                hits[address]+=1
                if hits[address]<=100:capture(event,'trace-'+name)
                thread=OpenThread(0x1a,False,event.tid)
                try:
                    buffer,ctx=context(thread)
                    C.c_ulonglong.from_address(ctx+248).value=address
                    C.c_uint32.from_address(ctx+68).value|=0x100
                    write(process,address,original)
                    if not SetContext(thread,PTR(ctx)):raise C.WinError(C.get_last_error())
                    pending_steps[event.tid]=address
                finally:CloseHandle(thread)
                status=0x10002
            elif code==0x80000004 and event.tid in pending_steps:
                address=pending_steps.pop(event.tid)
                if hits[address]<100:write(process,address,b'\xcc')
                thread=OpenThread(0x1a,False,event.tid)
                try:
                    buffer,ctx=context(thread)
                    C.c_uint32.from_address(ctx+68).value&=~0x100
                    SetContext(thread,PTR(ctx))
                finally:CloseHandle(thread)
                status=0x10002
            elif code==0x80000003 and not initial_break:
                initial_break=True;status=0x10002
            elif code==0xc0000005:
                target=unity and unity+0x1dd2e0<=address<unity+0x1dd300
                if target or not exception.first:capture(event,'access-violation',True)
        elif event.code==5:
            exit_code=event.data.exitCode
            signed=exit_code if exit_code<0x80000000 else exit_code-0x100000000
            result={'pid':p.pid,'exitCode':signed,'exitHex':hex(exit_code),'commandSent':sent,'seconds':time.monotonic()-start,
                    'exceptionCounts':exceptions,'captures':len(captures),'traceHits':{breakpoints[a][0]:n for a,n in hits.items()},
                    'debugger':True,'forcedTermination':False,'normalCloseRetry':close_retried}
            (root/'process.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
            emit('exit',**result)
            ContinueEvent(event.pid,event.tid,status)
            break
        if not ContinueEvent(event.pid,event.tid,status):raise C.WinError(C.get_last_error())
    else:
        emit('timeout',pid=p.pid,retained=True)
finally:
    # For a normal completed run there are no live target breakpoints to restore.
    # A timeout removes software probes and detaches; it never kills the player.
    if p.poll() is None:
        for address,(name,original) in breakpoints.items():
            try:write(process,address,original)
            except OSError:pass
        Detach(p.pid)
    symbols.close()
    events_file.close()
