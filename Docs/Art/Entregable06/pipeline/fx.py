import bpy, sys, os, math, json
import numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from common import save, load, pixels
import trees

args = sys.argv[sys.argv.index('--') + 1:]
RAW, OUT = args[0], args[1]
FX = os.path.join(OUT, 'Textures', 'FX')
WATER = os.path.join(OUT, 'Textures', 'Water')
rng = np.random.default_rng(11)

def grid(n):
    y, x = np.mgrid[0:n, 0:n].astype(np.float32)
    return (x + 0.5) / n * 2 - 1, (y + 0.5) / n * 2 - 1

def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1); return t * t * (3 - 2 * t)

def fftnoise(n, beta, seed):
    r = np.random.default_rng(seed)
    fy = np.fft.fftfreq(n)[:, None]; fx = np.fft.fftfreq(n)[None, :]
    k = np.sqrt(fx * fx + fy * fy); k[0, 0] = 1
    amp = 1 / k ** beta; amp[0, 0] = 0
    f = np.real(np.fft.ifft2(amp * np.exp(2j * np.pi * r.random((n, n)))))
    return ((f - f.min()) / (f.max() - f.min())).astype(np.float32)

def rgba(rgb, a):
    out = np.zeros(a.shape + (4,), np.float32)
    out[..., :3] = rgb; out[..., 3] = a
    return out

def sheet(cells):
    """2x2 flipbook, cells listed top-left, top-right, bottom-left, bottom-right (Unity reads row 0 at top)."""
    top = np.concatenate(cells[:2], 1); bot = np.concatenate(cells[2:], 1)
    return np.concatenate([top, bot], 0)

def out(img, name):
    save(img[::-1].copy(), os.path.join(FX if not name.startswith('T_Water') else WATER, name))

def soft_dot(n=128):
    x, y = grid(n); r = np.sqrt(x * x + y * y)
    a = np.exp(-r * r * 4.5) * (1 - smooth(0.85, 1.0, r))
    return rgba(1.0, a)

def puff(n, seed, density=1.0, edge=0.35):
    x, y = grid(n); r = np.sqrt(x * x + y * y)
    low = fftnoise(n, 3.2, seed); mid = fftnoise(n, 2.6, seed + 1)
    nz = low * 0.65 + mid * 0.35
    warp = r + (low - 0.5) * 0.55
    shape = 1 - smooth(0.15, 0.95, warp)
    a = np.clip(shape * smooth(0.2, 0.75, nz + shape * 0.35) * density, 0, 1) ** 1.3
    gy, gx = np.gradient(low)
    light = np.clip(0.9 - gy * 18 - gx * 6, 0.7, 1.0)
    return rgba(np.stack([light] * 3, -1), a)

def sparkle(n=128):
    x, y = grid(n); r = np.sqrt(x * x + y * y)
    core = np.exp(-r * r * 60)
    rays = np.exp(-np.abs(x) * 40) * np.exp(-np.abs(y) * 3.5) + np.exp(-np.abs(y) * 40) * np.exp(-np.abs(x) * 3.5)
    diag = (np.exp(-np.abs(x - y) * 50) + np.exp(-np.abs(x + y) * 50)) * np.exp(-r * 6) * 0.35
    glow = np.exp(-r * r * 8) * 0.35
    a = np.clip(core + rays * 0.8 + diag + glow, 0, 1) * (1 - smooth(0.9, 1.0, r))
    return rgba(1.0, a)

def ring(n=256):
    x, y = grid(n); r = np.sqrt(x * x + y * y)
    a = np.exp(-((r - 0.78) / 0.07) ** 2) + 0.35 * np.exp(-((r - 0.6) / 0.05) ** 2)
    a *= 1 - smooth(0.92, 1.0, r)
    return rgba(1.0, np.clip(a, 0, 1))

def droplet(n=64):
    x, y = grid(n)
    yy = y * 1.0; xx = x / (0.45 + 0.35 * smooth(-1, 0.6, -yy))
    r = np.sqrt(xx * xx + yy * yy)
    a = 1 - smooth(0.75, 0.95, r)
    hl = np.exp(-((x + 0.15) ** 2 + (y + 0.25) ** 2) * 40)
    col = np.clip(0.8 + hl[..., None] * 0.6, 0, 1) * np.ones(3)
    return rgba(col, a)

def splash(n, seed):
    r_ = np.random.default_rng(seed)
    x, y = grid(n); a = np.zeros((n, n), np.float32)
    for _ in range(140):
        ang = r_.uniform(-2.4, -0.74)  # upward fan (image y down)
        d = r_.uniform(0.05, 0.9) ** 0.8
        cx, cy = math.cos(ang) * d, 0.75 + math.sin(ang) * d * 1.3
        s = r_.uniform(0.012, 0.04) * (1.2 - d * 0.6)
        a += np.exp(-(((x - cx) ** 2 + (y - cy) ** 2) / (2 * s * s)))
    sheet_ = np.exp(-((y - 0.72) / 0.12) ** 2) * np.exp(-(x / 0.55) ** 2) * 0.7
    a = np.clip(a + sheet_, 0, 1)
    return rgba(1.0, a)

def butterfly(n, open_):
    x, y = grid(n); a = np.zeros((n, n), np.float32); col = np.zeros((n, n, 3), np.float32)
    sx = 0.15 + 0.85 * open_
    for side in (-1, 1):
        for (cx, cy, rx, ry, c) in [(0.42, -0.25, 0.42, 0.38, (0.95, 0.55, 0.12)), (0.32, 0.32, 0.3, 0.3, (0.9, 0.42, 0.08))]:
            ex = (x - side * cx * sx) / (rx * sx); ey = (y - cy) / ry
            m = np.clip(1 - smooth(0.85, 1.0, np.sqrt(ex * ex + ey * ey)), 0, 1)
            edge = smooth(0.62, 0.9, np.sqrt(ex * ex + ey * ey))
            cc = np.array(c) * (1 - edge[..., None] * 0.85) + edge[..., None] * np.array([0.05, 0.04, 0.03])
            spot = np.exp(-(((x - side * cx * sx * 1.15) / (0.07 * sx + 1e-3)) ** 2 + ((y - cy + 0.1) / 0.07) ** 2))
            cc = cc * (1 - spot[..., None]) + spot[..., None] * 0.95
            col = col * (1 - m[..., None]) + cc * m[..., None]; a = np.maximum(a, m)
    body = np.clip(1 - smooth(0.8, 1.0, np.sqrt((x / 0.05) ** 2 + (y / 0.55) ** 2)), 0, 1)
    col = col * (1 - body[..., None]) + body[..., None] * 0.06; a = np.maximum(a, body)
    return rgba(col, a)

def leaves_sheet(n=256):
    spr = trees.leaf_sprites()
    tints = [(1.0, 1.05, 0.9), (1.2, 1.1, 0.55), (1.0, 0.7, 0.42), (0.8, 0.52, 0.32)]
    cells = []
    for i, t in enumerate(tints):
        C = np.zeros((n, n, 4), np.float32); Hm = np.zeros((n, n), np.float32)
        s = spr[i * 2 % len(spr)]
        sc = (n * 0.85) / max(s.shape[:2])
        trees.stamp(C, Hm, s, n // 2, n // 2, math.radians(35), sc, np.array(t), desat=i >= 2)
        cells.append(C)
    return sheet(cells)

def water_maps():
    n = 512
    os.makedirs(WATER, exist_ok=True)
    for name, beta, seed in [('T_Water_NormalA.png', 3.4, 31), ('T_Water_NormalB.png', 2.9, 47)]:
        h = fftnoise(n, beta, seed)
        gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)); gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0))
        k = 0.22 / (np.sqrt((gx ** 2 + gy ** 2).mean()) + 1e-6)
        nrm = np.stack([-gx * k, -gy * k, np.ones_like(h)], -1); nrm /= np.linalg.norm(nrm, axis=-1, keepdims=True)
        out(rgba(nrm * 0.5 + 0.5, np.ones_like(h)), name)
    n1 = fftnoise(n, 2.2, 5); n2 = fftnoise(n, 2.8, 6)
    cell = 1 - np.abs(fftnoise(n, 1.2, 7) * 2 - 1); caust = np.clip((cell - 0.72) / 0.28, 0, 1) ** 2
    out(rgba(np.stack([n1, n2, caust], -1), np.ones_like(n1)), 'T_Water_Noise.png')

if __name__ == '__main__':
    out(soft_dot(), 'T_FX_SoftDot.png')
    out(sheet([puff(256, s) for s in (1, 2, 3, 4)]), 'T_FX_Smoke_2x2.png')
    out(sheet([puff(256, s, 0.7, 0.6) for s in (9, 10, 11, 12)]), 'T_FX_Dust_2x2.png')
    out(sparkle(), 'T_FX_Sparkle.png')
    out(ring(), 'T_FX_Ripple.png')
    out(droplet(), 'T_FX_Droplet.png')
    out(sheet([splash(256, s) for s in (21, 22, 23, 24)]), 'T_FX_Splash_2x2.png')
    out(np.concatenate([butterfly(256, o) for o in (1.0, 0.25)], 1), 'T_FX_Butterfly_2x1.png')
    out(leaves_sheet(), 'T_FX_Leaves_2x2.png')
    water_maps()
    print('@@ fx done')
