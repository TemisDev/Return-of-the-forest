"""Prepara el audio del Bloque 02 a partir de fuentes CC0 (OpenGameArt) y síntesis propia.
Uso: python audio_build.py <carpeta_descargas> <Game/Assets/_Bosque/Audio> <ffmpeg>
"""
import json, os, subprocess, sys, tempfile
import numpy as np
import soundfile as sf

SRC, OUT, FFMPEG = sys.argv[1], sys.argv[2], sys.argv[3]
SR = 48000
report = []


def load(rel):
    data, sr = sf.read(os.path.join(SRC, rel), always_2d=True, dtype='float32')
    if sr != SR:  # remuestreo lineal sencillo (fuentes de 44,1/44 kHz)
        n = int(len(data) * SR / sr)
        x = np.linspace(0, len(data) - 1, n)
        data = np.stack([np.interp(x, np.arange(len(data)), data[:, c]) for c in range(data.shape[1])], 1).astype('float32')
    return data


def rms(x):
    return float(np.sqrt(np.mean(np.square(x)) + 1e-12))


def calm_segment(x, seconds, win=5.0):
    """Ventana de <seconds> con la sonoridad más estable (evita golpes, voces o cortes de la grabación)."""
    w = int(win * SR); n = len(x) // w
    levels = np.array([rms(x[i * w:(i + 1) * w]) for i in range(n)])
    k = int(seconds / win)
    best, start = 1e9, 0
    for i in range(0, n - k):
        seg = levels[i:i + k]
        score = np.std(seg) / (np.mean(seg) + 1e-6)
        if seg.min() < np.median(levels) * .3: continue
        if score < best: best, start = score, i
    a = start * w
    return x[a:a + int(seconds * SR)]


def loopable(x, fade=2.0):
    """Bucle sin costura: el final se funde con el principio (potencia constante)."""
    f = int(fade * SR)
    head, body, tail = x[:f], x[f:-f], x[-f:]
    t = np.linspace(0, np.pi / 2, f, dtype='float32')[:, None]
    mixed = tail * np.cos(t) + head * np.sin(t)
    return np.concatenate([body, mixed])


def normalize(x, target_rms=None, peak=.89):
    if target_rms:
        x = x * (target_rms / rms(x))
    p = np.max(np.abs(x))
    if p > peak: x = x * (peak / p)
    return x.astype('float32')


def mono(x):
    return x.mean(axis=1, keepdims=True) if x.shape[1] > 1 else x


def trim(x, thresh=.01, pad=.02):
    a = np.abs(x).max(axis=1)
    idx = np.where(a > thresh)[0]
    if len(idx) == 0: return x
    s = max(0, idx[0] - int(pad * SR)); e = min(len(x), idx[-1] + int(pad * SR))
    y = x[s:e].copy(); f = min(len(y) // 4, int(.01 * SR))
    if f > 0: y[-f:] *= np.linspace(1, 0, f, dtype='float32')[:, None]
    return y


def write(x, rel, source, kind, q=4):
    dst = os.path.join(OUT, rel); os.makedirs(os.path.dirname(dst), exist_ok=True)
    tmp = os.path.join(tempfile.gettempdir(), 'bosque_audio_tmp.wav')
    sf.write(tmp, x, SR, subtype='PCM_16')
    subprocess.run([FFMPEG, '-y', '-loglevel', 'error', '-i', tmp, '-c:a', 'libvorbis', '-q:a', str(q), dst], check=True)
    report.append({'archivo': rel.replace(os.sep, '/'), 'tipo': kind, 'fuente': source, 'segundos': round(len(x) / SR, 2),
                   'canales': x.shape[1], 'rms_dbfs': round(float(20 * np.log10(rms(x))), 1), 'pico_dbfs': round(float(20 * np.log10(np.max(np.abs(x)) + 1e-9)), 1)})


OGA = 'https://opengameart.org/content/'
# ---------------------------------------------------------------- ambientes (bucles)
write(normalize(loopable(calm_segment(load('windy_ambiance_0.ogg'), 64)), .12), 'Ambience/AMB_Viento_Seco.ogg', OGA + 'windy-ambient-noise', 'bucle 2D degradado')
write(normalize(loopable(calm_segment(load('park_ambience_wind.wav'), 64)), .07), 'Ambience/AMB_Brisa_Hojas.ogg', OGA + 'park-ambiences', 'bucle 2D restaurado')
write(normalize(loopable(calm_segment(load('park_ambience_river.wav'), 64)), .12), 'Ambience/AMB_Rio.ogg', OGA + 'park-ambiences', 'bucle 2D restaurado')
write(normalize(loopable(calm_segment(load('park_ambience_birds.wav'), 64)), .08), 'Ambience/AMB_Aves.ogg', OGA + 'park-ambiences', 'bucle 2D restaurado')
water = mono(load('x-sfx_loops/water_flowing.ogg'))
water = np.tile(water, (6, 1))
write(normalize(loopable(water, .25), .18), 'Ambience/AMB_Corriente_3D.ogg', OGA + '30-cc0-sfx-loops', 'bucle 3D restaurado (cauce)')
# ---------------------------------------------------------------- ambientes (puntuales)
birds = load('birds-isaiah658_0.ogg'); n = 0
levels = [rms(birds[i:i + SR]) for i in range(0, len(birds) - SR, SR)]
thr = np.percentile(levels, 70)
i = 0
while i < len(levels) and n < 5:
    if levels[i] > thr:
        seg = mono(birds[i * SR:(i + 2) * SR]); seg = seg * np.hanning(len(seg)).astype('float32')[:, None] ** .3
        write(normalize(seg, .1), f'Ambience/AMB_Ave_{n + 1:02d}.ogg', OGA + 'ambient-bird-sounds', 'puntual 3D restaurado'); n += 1; i += 3
    else: i += 1
write(normalize(trim(mono(load('birdchirping071414.wav'))), .1), 'Ambience/AMB_Ave_06.ogg', OGA + 'bird-chirping-sounds', 'puntual 3D restaurado')
for k, f in enumerate(['creak1', 'creak2', 'creak3']):
    write(normalize(trim(mono(load(f'x-RPGsounds_Kenney/OGG/{f}.ogg'))), .06), f'Ambience/AMB_Crujido_{k + 1:02d}.ogg', OGA + '50-rpg-sound-effects', 'puntual 3D degradado')
for k, f in enumerate(['crack02', 'crack05', 'crack08']):
    write(normalize(trim(mono(load(f'x-independent_nu_ljudbank-wood_crack_hit_destruction/wood_impact/{f}.mp3.flac'))), .05), f'Ambience/AMB_Rama_Seca_{k + 1:02d}.ogg', OGA + '35-wooden-crackshitsdestructions', 'puntual 3D degradado')
# ---------------------------------------------------------------- efectos
for k, f in enumerate(['SandL1', 'SandL2', 'SandL3', 'SandR1', 'SandR2', 'SandR3']):
    write(normalize(trim(mono(load(f'x-Fantozzi-footsteps/Fantozzi-footsteps/ogg/Fantozzi-{f}.ogg'))), .12), f'Sfx/SFX_Paso_Tierra_{k + 1:02d}.ogg', OGA + 'fantozzis-footsteps-grasssand-stone', 'paso')
for k, f in enumerate(['cloth1', 'cloth3', 'cloth4']):
    write(normalize(trim(mono(load(f'x-RPGsounds_Kenney/OGG/{f}.ogg'))), .12), f'Sfx/SFX_Salto_{k + 1:02d}.ogg', OGA + '50-rpg-sound-effects', 'salto')
for k, f in enumerate(['StoneL2', 'SandR2']):
    s = mono(load(f'x-Fantozzi-footsteps/Fantozzi-footsteps/ogg/Fantozzi-{f}.ogg')); s = trim(s)
    write(normalize(s * 1.0, .2), f'Sfx/SFX_Aterrizaje_{k + 1:02d}.ogg', OGA + 'fantozzis-footsteps-grasssand-stone', 'aterrizaje')
for k, f in enumerate(['handleSmallLeather', 'handleSmallLeather2']):
    write(normalize(trim(mono(load(f'x-RPGsounds_Kenney/OGG/{f}.ogg'))), .12), f'Sfx/SFX_Interaccion_{k + 1:02d}.ogg', OGA + '50-rpg-sound-effects', 'interacción válida')
for k, f in enumerate(['impactwood04', 'impactwood11', 'impactwood17']):
    a = trim(mono(load(f'x-independent_nu_ljudbank-wood_crack_hit_destruction/wood_impact/{f}.mp3.flac')))
    b = trim(mono(load(f'x-independent_nu_ljudbank-wood_crack_hit_destruction/wood_impact/crack0{k + 3}.mp3.flac')))
    L = max(len(a), len(b) + int(.12 * SR)); m = np.zeros((L, 1), 'float32'); m[:len(a)] += a; m[int(.12 * SR):int(.12 * SR) + len(b)] += b * .7
    write(normalize(m, .18), f'Sfx/SFX_Retirar_Tronco_{k + 1:02d}.ogg', OGA + '35-wooden-crackshitsdestructions', 'retirar tronco')
rocks = [f for f in sorted(os.listdir(os.path.join(SRC, 'x-sfx_breaking_and_falling'))) if 'rock' in f]
for k, f in enumerate(rocks[:4]):
    write(normalize(trim(mono(load(f'x-sfx_breaking_and_falling/{f}'))), .18), f'Sfx/SFX_Retirar_Rocas_{k + 1:02d}.ogg', OGA + '75-cc0-breaking-falling-hit-sfx', 'retirar rocas')
for k, f in enumerate(['crack01', 'crack04', 'crack07']):
    a = trim(mono(load(f'x-independent_nu_ljudbank-wood_crack_hit_destruction/wood_impact/{f}.mp3.flac')))
    c = trim(mono(load(f'x-RPGsounds_Kenney/OGG/cloth{k + 2}.ogg')))
    L = max(len(a), len(c) + int(.05 * SR)); m = np.zeros((L, 1), 'float32'); m[:len(a)] += a; m[int(.05 * SR):int(.05 * SR) + len(c)] += c * .8
    write(normalize(m, .15), f'Sfx/SFX_Retirar_Ramas_{k + 1:02d}.ogg', OGA + '35-wooden-crackshitsdestructions + 50-rpg-sound-effects', 'retirar ramas')
write(normalize(trim(mono(load('x-RPGsounds_Kenney/OGG/doorOpen_1.ogg'))), .14), 'Sfx/SFX_Salida_Desbloqueo_01.ogg', OGA + '50-rpg-sound-effects', 'desbloqueo de salida')


# ---------------------------------------------------------------- síntesis propia (CC0, este proyecto)
def flute(freq, dur, amp=.3, vib=5.2):
    t = np.arange(int(dur * SR)) / SR
    f = freq * (1 + .006 * np.sin(2 * np.pi * vib * t) * np.clip(t * 3, 0, 1))
    ph = 2 * np.pi * np.cumsum(f) / SR
    tone = np.sin(ph) + .35 * np.sin(2 * ph) + .12 * np.sin(3 * ph)
    breath = np.random.default_rng(int(freq)).normal(0, 1, len(t)) * .04
    env = np.minimum(1, t / .06) * np.exp(-np.maximum(0, t - dur * .55) * 5)
    return ((tone + breath) * env * amp).astype('float32')


def place(buf, x, at):
    a = int(at * SR); buf[a:a + len(x)] += x[:len(buf) - a]


def reverb(x, taps=((.031, .5), (.047, .4), (.073, .3), (.11, .22), (.17, .15), (.23, .1))):
    y = x.copy()
    for d, g in taps:
        k = int(d * SR); y[k:] += x[:-k] * g
    return y


# Restauración: brillo ascendente (pentatónica de La menor) sobre agua que corre
pent = [440, 523.25, 587.33, 659.25, 783.99, 880, 1046.5]
buf = np.zeros(int(4.2 * SR), 'float32')
for i, fq in enumerate(pent):
    place(buf, flute(fq, 1.6, .16), .08 + i * .28)
sweep = mono(load('x-water-splash-slime-sfx/loop_water_01.ogg'))[:, 0]
sweep = sweep[:len(buf)] * np.linspace(0, .5, min(len(buf), len(sweep))) ** 1.2
buf[:len(sweep)] += sweep * .6
write(normalize(reverb(buf)[:, None], .1), 'Sfx/SFX_Restauracion_01.ogg', 'síntesis propia + ' + OGA + '40-cc0-water-splash-slime-sfx', 'restauración ambiental')
# Final del nivel: frase de quena en pentatónica (inspiración andina)
phrase = [(659.25, .35), (783.99, .35), (880, .5), (783.99, .35), (659.25, .35), (587.33, .5), (659.25, 1.2)]
buf = np.zeros(int(4.6 * SR), 'float32'); t = .05
for fq, d in phrase:
    place(buf, flute(fq, d + .25, .25), t); t += d
write(normalize(reverb(buf)[:, None], .1), 'Sfx/SFX_Nivel_Completado_01.ogg', 'síntesis propia', 'final del nivel')
# ---------------------------------------------------------------- música
m = load('beautiful_forest_16bit_44.1khz_1.ogg')
write(normalize(loopable(m, 3.0), .1), 'Music/MUS_Bosque_Hermoso.ogg', OGA + 'beautiful-forest-orchestra', 'música en bucle', q=5)

json.dump(report, open(os.path.join(OUT, 'AUDIO-PROCESADO.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
for r in report: print(r['archivo'], r['segundos'], 's', r['rms_dbfs'], 'dBFS rms', r['pico_dbfs'], 'pico')
