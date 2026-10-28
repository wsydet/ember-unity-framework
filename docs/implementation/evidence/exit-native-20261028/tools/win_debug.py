"""Read-only native symbol/stack helpers for the scoped exit investigation."""
import ctypes as C
from ctypes import wintypes as W
import struct

K = C.WinDLL('kernel32', use_last_error=True)
D = C.WinDLL('dbghelp', use_last_error=True)
PTR = C.c_void_p
U64 = C.c_ulonglong

def bind(lib, name, restype, *args):
    f = getattr(lib, name)
    f.restype, f.argtypes = restype, args
    return f

CloseHandle = bind(K, 'CloseHandle', W.BOOL, W.HANDLE)
ReadMemory = bind(K, 'ReadProcessMemory', W.BOOL, W.HANDLE, PTR, PTR, C.c_size_t, PTR)
WriteMemory = bind(K, 'WriteProcessMemory', W.BOOL, W.HANDLE, PTR, PTR, C.c_size_t, PTR)
OpenThread = bind(K, 'OpenThread', W.HANDLE, W.DWORD, W.BOOL, W.DWORD)
GetContext = bind(K, 'GetThreadContext', W.BOOL, W.HANDLE, PTR)
SetContext = bind(K, 'SetThreadContext', W.BOOL, W.HANDLE, PTR)
FlushInstructionCache = bind(K, 'FlushInstructionCache', W.BOOL, W.HANDLE, PTR, C.c_size_t)
GetFinalPath = bind(K, 'GetFinalPathNameByHandleW', W.DWORD, W.HANDLE, W.LPWSTR, W.DWORD, W.DWORD)
SymInitialize = bind(D, 'SymInitializeW', W.BOOL, W.HANDLE, W.LPCWSTR, W.BOOL)
SymCleanup = bind(D, 'SymCleanup', W.BOOL, W.HANDLE)
SymSetOptions = bind(D, 'SymSetOptions', W.DWORD, W.DWORD)
SymLoad = bind(D, 'SymLoadModuleExW', U64, W.HANDLE, W.HANDLE, W.LPCWSTR, W.LPCWSTR, U64, W.DWORD, PTR, W.DWORD)
SymFromAddr = bind(D, 'SymFromAddr', W.BOOL, W.HANDLE, U64, C.POINTER(U64), PTR)
SymEnumCallback = C.WINFUNCTYPE(W.BOOL, PTR, W.ULONG, PTR)
SymEnum = bind(D, 'SymEnumSymbols', W.BOOL, W.HANDLE, U64, W.LPCSTR, SymEnumCallback, PTR)

class Line(C.Structure):
    _fields_ = [('size', W.DWORD), ('key', PTR), ('line', W.DWORD), ('file', PTR), ('address', U64)]

GetLine = bind(D, 'SymGetLineFromAddr64', W.BOOL, W.HANDLE, U64, C.POINTER(W.DWORD), C.POINTER(Line))

class Address(C.Structure):
    _fields_ = [('offset', U64), ('segment', W.DWORD), ('mode', W.DWORD)]

class StackFrame(C.Structure):
    _fields_ = [('pc', Address), ('ret', Address), ('frame', Address), ('stack', Address), ('bstore', Address),
                ('function', PTR), ('params', U64 * 4), ('far', W.BOOL), ('virtual', W.BOOL),
                ('reserved', U64 * 3), ('kdhelp', C.c_ubyte * 112)]

StackWalk = bind(D, 'StackWalk64', W.BOOL, W.DWORD, W.HANDLE, W.HANDLE, C.POINTER(StackFrame), PTR, PTR, PTR, PTR, PTR)

def file_path(handle):
    b = C.create_unicode_buffer(32768)
    if not GetFinalPath(handle, b, len(b), 0):
        return None
    return b.value.removeprefix('\\\\?\\')

def read(process, address, length):
    b = C.create_string_buffer(length)
    n = C.c_size_t()
    if not ReadMemory(process, PTR(address), b, length, C.byref(n)):
        return None
    return b.raw[:n.value]

def write(process, address, data):
    b = C.create_string_buffer(data)
    n = C.c_size_t()
    if not WriteMemory(process, PTR(address), b, len(data), C.byref(n)):
        raise C.WinError(C.get_last_error())
    FlushInstructionCache(process, PTR(address), len(data))

class Symbols:
    def __init__(self, process, search):
        self.process = process
        SymSetOptions(0x2 | 0x4 | 0x10 | 0x200)
        if not SymInitialize(process, search, False):
            raise C.WinError(C.get_last_error())
        self.modules = {}
        self.module_sizes = {}

    def load(self, path, base=0, handle=None):
        result = SymLoad(self.process, handle, path, None, base, 0, None, 0)
        if base or result:
            self.modules[base or result] = path
            try:
                with open(path,'rb') as source:data=source.read(4096)
                nt=struct.unpack_from('<I',data,0x3c)[0]
                self.module_sizes[base or result]=struct.unpack_from('<I',data,nt+24+56)[0]
            except (OSError,struct.error):pass
        return result

    def resolve(self, address):
        b = C.create_string_buffer(2048)
        struct.pack_into('<I', b, 0, 88)
        struct.pack_into('<I', b, 80, 1024)
        displacement = U64()
        ok = SymFromAddr(self.process, address, C.byref(displacement), b)
        result = {'address': hex(address)}
        if ok:
            result.update(symbol=C.string_at(C.addressof(b) + 84).decode(errors='replace'),
                          displacement=hex(displacement.value), moduleBase=hex(struct.unpack_from('<Q', b, 32)[0]))
        else:
            bases = [base for base in self.modules if base <= address < base+self.module_sizes.get(base,0)]
            if bases:
                base = max(bases)
                result.update(module=self.modules[base], rva=hex(address-base))
        line, disp = Line(), W.DWORD()
        line.size = C.sizeof(line)
        if GetLine(self.process, address, C.byref(disp), C.byref(line)):
            result.update(file=C.string_at(line.file).decode(errors='replace'), line=line.line)
        return result

    def enum(self, base, mask, limit=200):
        result = []
        @SymEnumCallback
        def callback(info, size, user):
            name = C.string_at(info + 84).decode(errors='replace')
            address = C.c_ulonglong.from_address(info + 56).value
            result.append({'symbol': name, 'address': hex(address), 'rva': hex(address-base)})
            return len(result) < limit
        SymEnum(self.process, base, mask.encode(), callback, None)
        return result

    def close(self):
        SymCleanup(self.process)

def context(thread):
    b = C.create_string_buffer(1232 + 16)
    address = (C.addressof(b) + 15) & ~15
    C.memset(address, 0, 1232)
    C.c_uint32.from_address(address+48).value = 0x100003
    if not GetContext(thread, PTR(address)):
        raise C.WinError(C.get_last_error())
    return b, address

def registers(address):
    names = ['rax','rcx','rdx','rbx','rsp','rbp','rsi','rdi','r8','r9','r10','r11','r12','r13','r14','r15','rip']
    return dict(zip(names, struct.unpack('<17Q', C.string_at(address+120, 17*8))))

def stack(symbols, thread, ctx, limit=48):
    regs = registers(ctx)
    context_copy = C.create_string_buffer(1232+16)
    walk_ctx = (C.addressof(context_copy)+15)&~15
    C.memmove(walk_ctx,ctx,1232)
    frame = StackFrame()
    frame.pc.offset, frame.frame.offset, frame.stack.offset = regs['rip'], regs['rbp'], regs['rsp']
    frame.pc.mode = frame.frame.mode = frame.stack.mode = 3
    result = [symbols.resolve(regs['rip'])]
    last = None
    for _ in range(limit):
        if not StackWalk(0x8664, symbols.process, thread, C.byref(frame), PTR(walk_ctx), None,
                         C.cast(D.SymFunctionTableAccess64, PTR), C.cast(D.SymGetModuleBase64, PTR), None):
            break
        key = (frame.pc.offset, frame.stack.offset)
        if not frame.pc.offset or key == last:
            break
        last = key
        result.append(symbols.resolve(frame.pc.offset))
    return result

def pe_bytes(path, rva, length):
    data = path.read_bytes()
    nt = struct.unpack_from('<I', data, 0x3c)[0]
    count = struct.unpack_from('<H', data, nt+6)[0]
    optional_size = struct.unpack_from('<H', data, nt+20)[0]
    sections = nt+24+optional_size
    for i in range(count):
        offset = sections+40*i
        vsize, va, rawsize, raw = struct.unpack_from('<4I', data, offset+8)
        if va <= rva < va+max(vsize, rawsize):
            return data[raw+rva-va:raw+rva-va+length]
    raise ValueError(hex(rva))
