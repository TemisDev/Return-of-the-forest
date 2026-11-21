"""Mapas de normales del agua del valle (V13): olas sin repetición visible.

Uso: python -I generar_normales_agua.py <carpeta_texturas>
Suma de ~70 ondas con números de onda enteros (la textura repite sin costura), en direcciones al azar y con
amplitud que cae con la frecuencia, como el oleaje real. A: ondas medianas; B: rizos finos.
Escribe T_Valle_AguaNormalA.png y T_Valle_AguaNormalB.png (normal OpenGL, 1024 px).
"""
import os, sys
import numpy as np
from PIL import Image

out = sys.argv[1]
N = 1024


def waves(seed, kmin, kmax, count, slope):
    rng = np.random.default_rng(seed)
    y, x = np.mgrid[0:N, 0:N] / N
    dx = np.zeros((N, N)); dy = np.zeros((N, N))
    for _ in range(count):
        while True:
            kx, ky = rng.integers(-kmax, kmax + 1, 2)
            k = np.hypot(kx, ky)
            if kmin <= k <= kmax: break
        a = k ** -slope; ph = rng.uniform(0, 2 * np.pi)
        arg = 2 * np.pi * (kx * x + ky * y) + ph
        # derivadas analíticas de a·sin(arg)
        c = a * np.cos(arg) * 2 * np.pi
        dx += c * kx; dy += c * ky
    s = 1.0 / np.percentile(np.hypot(dx, dy), 99) * 1.4
    n = np.stack([-dx * s, -dy * s, np.ones_like(dx)], -1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return ((n * .5 + .5) * 255).round().astype(np.uint8)


Image.fromarray(waves(11, 3, 16, 70, 1.6)).save(os.path.join(out, "T_Valle_AguaNormalA.png"))
Image.fromarray(waves(23, 12, 48, 90, 1.3)).save(os.path.join(out, "T_Valle_AguaNormalB.png"))
print("ok")
