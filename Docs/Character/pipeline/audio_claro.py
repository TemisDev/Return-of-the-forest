"""Efectos del Bloque 03 (zona El claro vacío): brote, intento fallido y puente de raíces.
Fuentes CC0 de OpenGameArt (las mismas de audio_build.py) + síntesis propia.
Uso: python audio_claro.py <carpeta_descargas> <Game/Assets/_Bosque/Audio> <ffmpeg>
"""
import json, os, subprocess, sys, tempfile
import numpy as np
import soundfile as sf

SRC, OUT, FFMPEG = sys.argv[1], sys.argv[2], sys.argv[3]
SR = 48000
rng = np.random.default_rng(20261121)


def load(rel):
    d, sr = sf.read(os.path.join(SRC, rel), always_2d=True, dtype='float32')
    d = d.mean(axis=1)
    if sr != SR:
        x = np.linspace(0, len(d) - 1, int(len(d) * SR / sr)); d = np.interp(x, np.arange(len(d)), d).astype('float32')
    return d


def trim(x, thr=.01):
    idx = np.where(np.abs(x) > thr)[0]
    return x[max(0, idx[0] - 200): idx[-1] + 400] if len(idx) else x


def env(n, a=.01, r=.4):
    t = np.arange(n) / SR
    return np.minimum(1, t / a) * np.exp(-t / r)


def tone(f, dur, amp=.3, a=.01, r=.35, harm=(1, .3, .1)):
    t = np.arange(int(dur * SR)) / SR
    s = sum(h * np.sin(2 * np.pi * f * (k + 1) * t) for k, h in enumerate(harm))
    return (s * env(len(t), a, r) * amp).astype('float32')


def place(buf, x, at):
    i = int(at * SR); n = min(len(x), len(buf) - i); buf[i:i + n] += x[:n]


def reverb(x):
    y = x.copy()
    for d, g in ((.029, .45), (.043, .35), (.067, .25), (.1, .17), (.16, .1)):
        k = int(d * SR); y[k:] += x[:-k] * g
    return y


def norm(x, rms=.1, peak=.89):
    x = x * (rms / (np.sqrt(np.mean(x ** 2)) + 1e-9)); p = np.abs(x).max()
    return (x * (peak / p) if p > peak else x).astype('float32')


report = []


def write(x, name, source):
    dst = os.path.join(OUT, 'Sfx', name)
    tmp = os.path.join(tempfile.gettempdir(), 'bosque_claro_tmp.wav'); sf.write(tmp, x, SR, subtype='PCM_16')
    subprocess.run([FFMPEG, '-y', '-loglevel', 'error', '-i', tmp, '-c:a', 'libvorbis', '-q:a', '5', dst], check=True)
    report.append({'archivo': 'Sfx/' + name, 'fuente': source, 'segundos': round(len(x) / SR, 2)})
    print(name, round(len(x) / SR, 2), 's')


OGA = 'https://opengameart.org/content/'
cloth = [trim(load(f'x-RPGsounds_Kenney/OGG/cloth{i}.ogg')) for i in (1, 2, 3)]
for k in range(3):
    buf = np.zeros(int(1.6 * SR), 'float32')
    base = [523.25, 587.33, 659.25][k]
    for j, f in enumerate([base, base * 1.5, base * 2]):  # pulso ascendente (quinta y octava)
        place(buf, tone(f, .9, .16 - j * .03, .005, .3), .02 + j * .09)
    leaves = cloth[k] * .55; place(buf, leaves, .0)
    write(norm(reverb(buf), .09), f'SFX_Brote_{k + 1:02d}.ogg', 'síntesis propia + ' + OGA + '50-rpg-sound-effects')

for k, f in enumerate(['SandL3', 'SandR1']):
    s = trim(load(f'x-Fantozzi-footsteps/Fantozzi-footsteps/ogg/Fantozzi-{f}.ogg'))
    s = np.interp(np.arange(0, len(s), .7), np.arange(len(s)), s).astype('float32')  # más lento y grave: tierra seca
    buf = np.zeros(int(1.0 * SR), 'float32'); place(buf, s * .9, 0)
    place(buf, tone(196, .5, .12, .01, .18, (1, .15)), .03)
    write(norm(buf, .1), f'SFX_Brote_Fallido_{k + 1:02d}.ogg', 'síntesis propia + ' + OGA + 'fantozzis-footsteps-grasssand-stone')

creak = trim(load('x-RPGsounds_Kenney/OGG/creak2.ogg'))
wood = trim(load('x-independent_nu_ljudbank-wood_crack_hit_destruction/wood_impact/impactwood08.mp3.flac'))
buf = np.zeros(int(3.2 * SR), 'float32')
slow = np.interp(np.arange(0, len(creak), .5), np.arange(len(creak)), creak).astype('float32')
place(buf, slow * .8, .1); place(buf, wood * .5, .05); place(buf, wood * .35, 1.2)
for j, f in enumerate([220, 277.18, 329.63, 440]):
    place(buf, tone(f, 2.2, .07, .4, 1.2, (1, .4, .15)), .2 + j * .25)
write(norm(reverb(buf), .1), 'SFX_Puente_Raices_01.ogg', 'síntesis propia + ' + OGA + '50-rpg-sound-effects + 35-wooden-crackshitsdestructions')
json.dump(report, open(os.path.join(os.path.dirname(__file__), '..', 'AUDIO-CLARO.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
