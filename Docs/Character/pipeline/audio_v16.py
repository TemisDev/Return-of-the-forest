"""Audio V16: ambiente y música de bosque sin el "zumbido de ventilador" de los bucles anteriores.

Uso: python -I audio_v16.py <carpeta_descargas> <Game/Assets/_Bosque/Audio> <ffmpeg>

Problema medido en V15: AMB_Viento_Seco era una banda de ruido fija (76 % de la energía entre 500 y 2000 Hz, nada
por encima) y AMB_Brisa_Hojas tenía el 74 % por debajo de 150 Hz (retumbo): a oído, un motor o un ventilador.

- AMB_Viento_Seco.ogg (zonas degradadas) y AMB_Brisa_Hojas.ogg (restauradas): viento sintetizado como el real:
  ráfagas lentas e irregulares, banda que sube con cada ráfaga, silbido leve en las fuertes y, encima, crujido de
  pasto seco (degradado) o susurro de hojas (restaurado). Sin retumbo (< 90 Hz). Bucle de 60 s sin costura.
- AMB_Aves.ogg: "Birds and Wind - Ambient" (Spring Spring, CC0) con paso alto a 160 Hz y bucle con fundido.
- MUS_Bosque_Hermoso.ogg: "Forest Whisper" (Cleyton Kauffman, CC0), tema del bosque restaurado.
- MUS_Bosque_Apagado.ogg: "Cathedral in the forest" (congusbongus, CC0), pad sombrío para lo degradado.
Sonoridad: ambientes a -24/-26 LUFS, música a -23 LUFS (el juego aún los escala por bus y por estado).
"""
import os, subprocess, sys, tempfile
import numpy as np
import soundfile as sf

SRC, OUT, FFMPEG = sys.argv[1], sys.argv[2], sys.argv[3]
SR = 44100
rng = np.random.default_rng(1609)


def smooth_noise(n, hz, seed):
    """Ruido suave de baja frecuencia (curvas de ráfaga), en 0..1, periódico en n muestras."""
    r = np.random.default_rng(seed)
    k = max(4, int(n / SR * hz))
    pts = r.random(k)
    pts = np.append(pts, pts[0])                         # periódico: el bucle no salta
    x = np.linspace(0, k, n, endpoint=False)
    i = x.astype(int); f = x - i; f = f * f * (3 - 2 * f)
    return pts[i] * (1 - f) + pts[i + 1] * f


def shaped_noise(n, centers, width_oct, seed, floor_hz=90.0, hop=1024):
    """Ruido filtrado frame a frame con una campana (en octavas) centrada en centers[t] (Hz)."""
    r = np.random.default_rng(seed)
    win = 4096
    x = r.standard_normal(n + win).astype(np.float32)
    out = np.zeros(n + win, np.float32); norm = np.zeros(n + win, np.float32)
    w = np.hanning(win).astype(np.float32)
    fr = np.fft.rfftfreq(win, 1 / SR); lf = np.log2(np.maximum(fr, 1.0))
    hp = 1.0 / (1.0 + (floor_hz / np.maximum(fr, 1.0)) ** 6)     # paso alto: sin retumbo
    for a in range(0, n, hop):
        c = centers[min(a, n - 1)]
        shape = np.exp(-0.5 * ((lf - np.log2(c)) / width_oct) ** 2) * hp
        spec = np.fft.rfft(x[a:a + win] * w) * shape
        out[a:a + win] += np.fft.irfft(spec).astype(np.float32) * w
        norm[a:a + win] += w * w
    return (out / np.maximum(norm, 1e-3))[:n]


def rustle(n, density, band, seed):
    """Crujido/susurro: granos cortos de ruido agudo con densidad variable (pasto seco u hojas)."""
    r = np.random.default_rng(seed)
    out = np.zeros(n, np.float32)
    base = shaped_noise(n, np.full(n, band), .55, seed + 7, floor_hz=1500)
    env = np.zeros(n, np.float32)
    t = 0
    while t < n:
        d = density[t]
        t += int(SR * r.exponential(1.0 / max(d, .05)))
        if t >= n: break
        L = int(SR * r.uniform(.015, .09)); amp = r.uniform(.3, 1.0)
        e = np.exp(-np.linspace(0, 5, L)) * amp
        end = min(n, t + L); env[t:end] += e[:end - t]
    # Lecho continuo muy suave bajo los granos (hojas moviéndose).
    env += .04 * density / max(density.max(), 1e-6)
    out = base * env
    return out


def wind(seconds, dry, seed):
    n = int(seconds * SR)
    g = .55 * smooth_noise(n, .07, seed) + .3 * smooth_noise(n, .19, seed + 1) + .15 * smooth_noise(n, .6, seed + 2)
    g = np.clip((g - .3) / .55, 0, 1) ** 1.8                       # ráfagas: calmas largas y picos marcados
    lo, hi = (260, 1100) if dry else (320, 1500)
    centers = lo * (hi / lo) ** g
    chans = []
    for c in range(2):                                             # estéreo descorrelacionado
        body = shaped_noise(n, centers * (1 + .04 * c), .9, seed + 10 + c)
        body *= (.06 + .94 * g)
        whistle_c = np.clip(g - .62, 0, 1) / .38
        whistle = shaped_noise(n, centers * 1.9, .08, seed + 20 + c) * whistle_c * .35
        gr = g ** 1.5
        grains = rustle(n, (.4 + 7 * gr) if dry else (.8 + 10 * gr), 3800 if dry else 5200, seed + 30 + c) * (.32 if dry else .26)
        chans.append(body + whistle + grains)
    x = np.stack(chans, 1)
    return x / np.abs(x).max() * .8


def loop(x, fade_s):
    """Bucle sin costura: el final se funde con el principio."""
    f = int(fade_s * SR); a = x[:f]; b = x[-f:]
    t = np.linspace(0, np.pi / 2, f)[:, None]
    head = b * np.cos(t) + a * np.sin(t)
    return np.concatenate([head, x[f:-f]], 0)


def ff(args):
    subprocess.run([FFMPEG, '-y', '-v', 'error'] + args, check=True)


def finish(x_or_path, out_name, lufs, filters=''):
    """Normaliza sonoridad (EBU R128) y exporta OGG; x puede ser un array o un archivo."""
    tmp = tempfile.mktemp(suffix='.wav')
    if isinstance(x_or_path, str): src = x_or_path
    else: sf.write(tmp, x_or_path.astype(np.float32), SR); src = tmp
    af = (filters + ',' if filters else '') + f'loudnorm=I={lufs}:TP=-2:LRA=11'
    ff(['-i', src, '-af', af, '-ar', str(SR), '-c:a', 'libvorbis', '-q:a', '5', os.path.join(OUT, out_name)])
    print('ok', out_name)


def decode(path):
    tmp = tempfile.mktemp(suffix='.wav')
    ff(['-i', path, '-ar', str(SR), '-ac', '2', tmp])
    return sf.read(tmp, dtype='float32')[0]


SECONDS = 66.0
finish(loop(wind(SECONDS, True, 11), 6.0), 'Ambience/AMB_Viento_Seco.ogg', -24)
finish(loop(wind(SECONDS, False, 23), 6.0), 'Ambience/AMB_Brisa_Hojas.ogg', -26)
birds = decode(os.path.join(SRC, 'birds_wind_ambient.ogg'))
finish(loop(birds, 5.0), 'Ambience/AMB_Aves.ogg', -25, 'highpass=f=160:poles=2')
whisper = decode(os.path.join(SRC, 'fw', 'Forest Whisper Theme', 'Forest Whisper.ogg'))
finish(loop(whisper, 4.0), 'Music/MUS_Bosque_Hermoso.ogg', -23, 'lowpass=f=9000')
cathedral = decode(os.path.join(SRC, 'cathedral_forest.ogg'))
finish(cathedral, 'Music/MUS_Bosque_Apagado.ogg', -25, 'lowpass=f=7000')
