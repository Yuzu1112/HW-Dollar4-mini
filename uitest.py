import ctypes, subprocess, sys, time
from ctypes import wintypes
from PIL import Image

try:
    ctypes.windll.shcore.SetProcessDpiAwareness(2)
except Exception:
    pass

u = ctypes.windll.user32
k32 = ctypes.windll.kernel32
g32 = ctypes.windll.gdi32

# 64 位下必须声明返回类型，否则句柄/指针被截成 32 位
k32.GlobalAlloc.restype = ctypes.c_void_p
k32.GlobalAlloc.argtypes = [wintypes.UINT, ctypes.c_size_t]
k32.GlobalLock.restype = ctypes.c_void_p
k32.GlobalLock.argtypes = [ctypes.c_void_p]
k32.GlobalUnlock.argtypes = [ctypes.c_void_p]
u.SetClipboardData.restype = ctypes.c_void_p
u.SetClipboardData.argtypes = [wintypes.UINT, ctypes.c_void_p]
u.GetClipboardData.restype = ctypes.c_void_p


class RECT(ctypes.Structure):
    _fields_ = [("l", ctypes.c_long), ("t", ctypes.c_long), ("r", ctypes.c_long), ("b", ctypes.c_long)]


def set_clipboard(text):
    CF_UNICODETEXT = 13
    GMEM_MOVEABLE = 0x0002
    data = text.encode("utf-16-le") + b"\x00\x00"
    u.OpenClipboard(0)
    u.EmptyClipboard()
    h = k32.GlobalAlloc(GMEM_MOVEABLE, len(data))
    p = k32.GlobalLock(h)
    ctypes.memmove(p, data, len(data))
    k32.GlobalUnlock(h)
    u.SetClipboardData(CF_UNICODETEXT, h)
    u.CloseClipboard()


def get_clipboard():
    u.OpenClipboard(0)
    try:
        h = u.GetClipboardData(13)
        if not h:
            return ""
        p = k32.GlobalLock(h)
        try:
            return ctypes.wstring_at(p)
        finally:
            k32.GlobalUnlock(h)
    finally:
        u.CloseClipboard()


def get_text(h):
    n = u.GetWindowTextLengthW(h)
    buf = ctypes.create_unicode_buffer(n + 2)
    u.GetWindowTextW(h, buf, n + 2)
    return buf.value


def click(h):
    """投递鼠标消息，不依赖前台窗口。"""
    lp = 5 | (5 << 16)
    u.PostMessageW(h, 0x0200, 0, lp)
    u.PostMessageW(h, 0x0201, 1, lp)
    time.sleep(0.05)
    u.PostMessageW(h, 0x0202, 0, lp)
    time.sleep(1.0)


def grab(hwnd, out):
    rc = RECT()
    u.GetWindowRect(hwnd, ctypes.byref(rc))
    w, hh = rc.r - rc.l, rc.b - rc.t
    hdcWin = u.GetWindowDC(hwnd)
    hdcMem = g32.CreateCompatibleDC(hdcWin)
    hbm = g32.CreateCompatibleBitmap(hdcWin, w, hh)
    g32.SelectObject(hdcMem, hbm)
    u.PrintWindow(hwnd, hdcMem, 0)

    class BIH(ctypes.Structure):
        _fields_ = [("biSize", wintypes.DWORD), ("biWidth", ctypes.c_long),
                    ("biHeight", ctypes.c_long), ("biPlanes", wintypes.WORD),
                    ("biBitCount", wintypes.WORD), ("biCompression", wintypes.DWORD),
                    ("biSizeImage", wintypes.DWORD), ("biXPelsPerMeter", ctypes.c_long),
                    ("biYPelsPerMeter", ctypes.c_long), ("biClrUsed", wintypes.DWORD),
                    ("biClrImportant", wintypes.DWORD)]

    bi = BIH()
    bi.biSize = ctypes.sizeof(BIH)
    bi.biWidth = w
    bi.biHeight = -hh
    bi.biPlanes = 1
    bi.biBitCount = 32
    buf = ctypes.create_string_buffer(w * hh * 4)
    g32.GetDIBits(hdcMem, hbm, 0, hh, buf, ctypes.byref(bi), 0)
    Image.frombuffer("RGBA", (w, hh), buf, "raw", "BGRA", 0, 1).convert("RGB").save(out)
    g32.DeleteObject(hbm)
    g32.DeleteDC(hdcMem)
    u.ReleaseDC(hwnd, hdcWin)


subprocess.run(["taskkill", "/F", "/IM", "HwD4Mini.exe"], capture_output=True)
time.sleep(0.5)
p = subprocess.Popen([r"C:\Users\QWQ\Documents\Code\HW-Dollar4-Mini\bin\HwD4Mini.exe"])
time.sleep(2.2)
pid = p.pid

found = []


@ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)
def top_cb(h, l):
    wpid = wintypes.DWORD()
    u.GetWindowThreadProcessId(h, ctypes.byref(wpid))
    if wpid.value == pid and u.IsWindowVisible(h):
        n = u.GetWindowTextLengthW(h)
        buf = ctypes.create_unicode_buffer(n + 2)
        u.GetWindowTextW(h, buf, n + 2)
        if buf.value.strip():
            found.append(h)
    return True


u.EnumWindows(top_cb, 0)
if not found:
    sys.exit("没找到窗体")
hwnd = found[0]

crect = RECT()
u.GetClientRect(hwnd, ctypes.byref(crect))
scale = (crect.r - crect.l) / 792.0
origin = wintypes.POINT(0, 0)
u.ClientToScreen(hwnd, ctypes.byref(origin))


def drect(h):
    r = RECT()
    u.GetWindowRect(h, ctypes.byref(r))
    return ((r.l - origin.x) / scale, (r.t - origin.y) / scale)


children = []


@ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)
def child_cb(h, l):
    cls = ctypes.create_unicode_buffer(128)
    u.GetClassNameW(h, cls, 128)
    children.append((h, cls.value, get_text(h), drect(h)))
    return True


u.EnumChildWindows(hwnd, child_cb, 0)

edit_in = edit_out = btn_auto = btn_enc = None
for h, cls, txt, (x, y) in children:
    if "EDIT" in cls:
        if abs(x - 24) < 8 and abs(y - 298) < 10:
            edit_in = h
        elif abs(x - 24) < 8 and abs(y - 334) < 10:
            edit_out = h
    elif "BUTTON" in cls:
        if txt == "自适应解密":
            btn_auto = h
        elif txt == "$2 加密":
            btn_enc = h

print("输入框", edit_in, "输出框", edit_out, "自适应", btn_auto, "$2加密", btn_enc)
if not (edit_in and edit_out and btn_auto and btn_enc):
    sys.exit("控件没找全")

cipher = open(r"C:\Users\QWQ\AppData\Local\Temp\d2field.txt", encoding="utf-8").read().strip()

# ---- 1) 粘贴密文 → 自适应解密 ----
u.SetWindowTextW(edit_in, "")
time.sleep(0.2)
set_clipboard(cipher)
print("剪贴板回读长度", len(get_clipboard()))
u.PostMessageW(edit_in, 0x0302, 0, 0)        # WM_PASTE
time.sleep(0.6)
print("粘贴后输入框:", repr(get_text(edit_in)[:45]))

click(btn_auto)
out1 = get_text(edit_out)
print("解密输出:", repr(out1))
print("解密正确:", out1.strip() == "HelloMini-2026")

# ---- 2) 粘贴明文 → $2 加密 ----
u.SetWindowTextW(edit_in, "")
time.sleep(0.2)
set_clipboard("PlainText-ABC")
u.PostMessageW(edit_in, 0x0302, 0, 0)
time.sleep(0.5)
print("粘贴后输入框:", repr(get_text(edit_in)[:45]))
click(btn_enc)
out2 = get_text(edit_out)
print("加密输出:", repr(out2[:60]))

# ---- 3) 把密文粘回去再自适应解密，验证往返 ----
if out2.strip():
    u.SetWindowTextW(edit_in, "")
    time.sleep(0.2)
    set_clipboard(out2)
    u.PostMessageW(edit_in, 0x0302, 0, 0)
    time.sleep(0.5)
    click(btn_auto)
    out3 = get_text(edit_out)
    print("往返结果:", repr(out3), "正确:", out3.strip() == "PlainText-ABC")

grab(hwnd, r"C:\Users\QWQ\Documents\Code\HW-Dollar4-Mini\ui_test.png")
print("已保存 ui_test.png   PID", pid)
