"""Montaje del tráiler de Bosque de regreso (20–30 s, 4K) a partir de las tomas grabadas en Unity.

Uso: python -I montar_trailer.py <raiz_repo> <carpeta_voz> <salida_dir>
- Tomas: <raiz_repo>/output/trailer/raw/<toma>/f0000.jpg (3840x2160, 30 fps, TrailerCapture.cs).
- Voz: <carpeta_voz>/v1.wav … v9.wav (es-PE-CamilaNeural, recortada).
- Música, ambiente y efectos: los del juego (Game/Assets/_Bosque/Audio, CC0 y propios).
Salida: BosqueDeRegreso_Trailer_4K.mp4 (H.264 High, 3840x2160, 30 fps, ~60 Mb/s) y _1080p.mp4.
"""
import os, subprocess, sys

ROOT, VO, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
RAW = os.path.join(ROOT, 'output', 'trailer', 'raw')
AUD = os.path.join(ROOT, 'Game', 'Assets', '_Bosque', 'Audio')
FONT = os.path.join(ROOT, 'Game', 'Assets', '_Bosque', 'UI', 'Fonts', 'AlegreyaSC-Bold.ttf').replace('\\', '/').replace(':', r'\:')
FONT_IT = os.path.join(ROOT, 'Game', 'Assets', '_Bosque', 'UI', 'Fonts', 'Alegreya-Italic.ttf').replace('\\', '/').replace(':', r'\:')
FFMPEG = 'ffmpeg'
FPS = 30
X = 0.4   # fundido entre tomas (s)

# (toma, inicio dentro de la toma en s, duración en el montaje en s)
SHOTS = [
    ('S1_arroyo_seco', 0.4, 4.2),
    ('S2_valle_degradado', 0.6, 3.8),
    ('S3_arroyo_restaura', 0.0, 5.0),
    ('S4_claro_brota', 0.1, 2.8),
    ('S5_refugio_zorro', 2.0, 2.6),
    ('S6_valle_puente', 0.4, 4.2),
    ('S7_valle_lagunas', 0.3, 6.4),
]
cuts = [0.0]
for _, _, d in SHOTS[:-1]: cuts.append(cuts[-1] + d)
TOTAL = cuts[-1] + SHOTS[-1][2]

# Voz: (archivo, instante en el montaje)
VOICE = [('v1', 0.55), ('v2', 4.55), ('v3', 8.15), ('v4', 10.45), ('v5', 13.25), ('v6', 15.95), ('v7', 19.4), ('v8', 22.95), ('v9', 26.95)]
# Efectos del juego: (archivo, instante, ganancia dB)
SFX = [('Sfx/SFX_Retirar_Tronco_01.ogg', 9.15, -6), ('Sfx/SFX_Restauracion_01.ogg', 9.55, -4), ('Sfx/SFX_Brote_01.ogg', 13.25, -6),
       ('Sfx/SFX_Nivel_Completado_01.ogg', 26.75, -10)]

def run(args):
    print(' '.join(args[:6]), '…'); subprocess.run([FFMPEG, '-y', '-hide_banner', '-v', 'error'] + args, check=True)

os.makedirs(OUT, exist_ok=True)

# ---------------------------------------------------------------- vídeo
inputs, chains = [], []
for i, (name, start, dur) in enumerate(SHOTS):
    length = dur + (X if i < len(SHOTS) - 1 else 0)
    first = int(round(start * FPS))
    inputs += ['-framerate', str(FPS), '-start_number', str(first), '-i', os.path.join(RAW, name, 'f%04d.jpg')]
    chains.append(f'[{i}:v]trim=duration={length:.3f},setpts=PTS-STARTPTS,format=yuv444p,fps={FPS}[s{i}]')
prev = 's0'
for i in range(1, len(SHOTS)):
    chains.append(f'[{prev}][s{i}]xfade=transition=fade:duration={X}:offset={cuts[i]:.3f}[x{i}]'); prev = f'x{i}'
# Etalonado suave: más contraste en medios, un poco de saturación y una viñeta ligera.
grade = (f'[{prev}]eq=contrast=1.06:saturation=1.08:gamma=0.98,'
         'curves=master=0/0 0.25/0.22 0.5/0.5 0.75/0.79 1/1,'
         'vignette=angle=PI/9:mode=forward,'
         f'fade=t=in:st=0:d=0.8,fade=t=out:st={TOTAL - 0.7:.2f}:d=0.7')
t_title, t_sub = 26.75, 27.55
title = (f",drawtext=fontfile='{FONT}':text='BOSQUE DE REGRESO':fontsize=196:fontcolor=0xF2EEDD:"
         f"x=(w-text_w)/2:y=(h-text_h)/2-60:shadowcolor=black@0.55:shadowx=0:shadowy=6:"
         f"alpha='if(lt(t,{t_title}),0,if(lt(t,{t_title + 0.9}),(t-{t_title})/0.9,1))'"
         f",drawtext=fontfile='{FONT_IT}':text='Reconecta un bosque andino · ODS 15':fontsize=68:fontcolor=0xD8D3BE:"
         f"x=(w-text_w)/2:y=(h/2)+90:shadowcolor=black@0.55:shadowx=0:shadowy=4:"
         f"alpha='if(lt(t,{t_sub}),0,if(lt(t,{t_sub + 0.9}),(t-{t_sub})/0.9,1))'")
chains.append(grade + '[g]')
# Velo oscuro que entra con el título (legibilidad sobre el valle).
chains.append(f'color=c=black:s=3840x2160:r={FPS}:d={TOTAL:.3f},format=rgba,colorchannelmixer=aa=0.42,'
              f'fade=t=in:st={t_title - 0.5:.2f}:d=1.2:alpha=1[dim]')
chains.append('[g][dim]overlay=format=auto' + title + ',format=yuv420p[v]')
vfilter = ';'.join(chains)

# ---------------------------------------------------------------- audio
a_inputs, a_chains, mix = [], [], []
n = len(SHOTS)
def add_in(path):
    a_inputs.extend(['-i', path]); return n + len(a_inputs) // 2 - 1
# Música: pad sombrío hasta la restauración, luego el tema del bosque (como en el juego).
m1 = add_in(os.path.join(AUD, 'Music', 'MUS_Bosque_Apagado.ogg'))
a_chains.append(f'[{m1}:a]atrim=0:12,asetpts=PTS-STARTPTS,volume=-15dB,afade=t=in:st=0:d=1.2,afade=t=out:st=9.3:d=2.0[mA]')
m2 = add_in(os.path.join(AUD, 'Music', 'MUS_Bosque_Hermoso.ogg'))
a_chains.append(f'[{m2}:a]atrim=0:{TOTAL - 9.3 + .5:.2f},asetpts=PTS-STARTPTS,volume=-11dB,afade=t=in:st=0:d=1.8,'
                f'afade=t=out:st={TOTAL - 9.3 - 1.6:.2f}:d=1.6,adelay=9300|9300[mB]')
# Ambiente: viento seco → agua y aves.
w = add_in(os.path.join(AUD, 'Ambience', 'AMB_Viento_Seco.ogg'))
a_chains.append(f'[{w}:a]atrim=0:11,asetpts=PTS-STARTPTS,volume=-9dB,afade=t=in:st=0:d=0.8,afade=t=out:st=9.2:d=1.6[aW]')
r = add_in(os.path.join(AUD, 'Ambience', 'AMB_Rio.ogg'))
a_chains.append(f'[{r}:a]atrim=0:{TOTAL - 9.4:.2f},asetpts=PTS-STARTPTS,volume=-16dB,afade=t=in:st=0:d=1.5,'
                f'afade=t=out:st={TOTAL - 9.4 - 1.2:.2f}:d=1.2,adelay=9400|9400[aR]')
b = add_in(os.path.join(AUD, 'Ambience', 'AMB_Aves.ogg'))
a_chains.append(f'[{b}:a]atrim=0:{TOTAL - 12.5:.2f},asetpts=PTS-STARTPTS,volume=-10dB,afade=t=in:st=0:d=1.5,'
                f'afade=t=out:st={TOTAL - 12.5 - 1.2:.2f}:d=1.2,adelay=12500|12500[aB]')
bed = ['[mA]', '[mB]', '[aW]', '[aR]', '[aB]']
for k, (f, at, g) in enumerate(SFX):
    i = add_in(os.path.join(AUD, f)); ms = int(at * 1000)
    a_chains.append(f'[{i}:a]volume={g}dB,adelay={ms}|{ms}[fx{k}]'); bed.append(f'[fx{k}]')
vo_labels = []
for k, (f, at) in enumerate(VOICE):
    i = add_in(os.path.join(VO, f + '.wav')); ms = int(at * 1000)
    # Voz: un poco de cuerpo y presencia, compresión suave.
    a_chains.append(f'[{i}:a]aformat=channel_layouts=mono,highpass=f=80,equalizer=f=180:t=q:w=1:g=1.5,equalizer=f=3200:t=q:w=1.2:g=2,'
                    f'acompressor=threshold=-20dB:ratio=3:attack=5:release=120,pan=stereo|c0=c0|c1=c0,adelay={ms}|{ms}[vo{k}]')
    vo_labels.append(f'[vo{k}]')
a_chains.append(''.join(bed) + f'amix=inputs={len(bed)}:normalize=0,aformat=channel_layouts=stereo[bed]')
a_chains.append(''.join(vo_labels) + f'amix=inputs={len(vo_labels)}:normalize=0,asplit=2[voice][key]')
# La voz baja la cama (ducking con compresor de cadena lateral).
a_chains.append('[bed][key]sidechaincompress=threshold=0.03:ratio=6:attack=15:release=450:makeup=1[ducked]')
a_chains.append(f'[ducked][voice]amix=inputs=2:normalize=0,afade=t=out:st={TOTAL - 0.8:.2f}:d=0.8,'
                'loudnorm=I=-14:TP=-1.0:LRA=9,aresample=48000[a]')
afilter = ';'.join(a_chains)

out4k = os.path.join(OUT, 'BosqueDeRegreso_Trailer_4K.mp4')
run(inputs + a_inputs + ['-filter_complex', vfilter + ';' + afilter, '-map', '[v]', '-map', '[a]', '-t', f'{TOTAL:.3f}',
     '-c:v', 'libx264', '-preset', 'slow', '-crf', '15', '-profile:v', 'high', '-level', '5.2', '-pix_fmt', 'yuv420p',
     '-x264-params', 'aq-mode=3:deblock=-1,-1', '-c:a', 'aac', '-b:a', '320k', '-movflags', '+faststart', out4k])
run(['-i', out4k, '-vf', 'scale=1920:1080:flags=lanczos', '-c:v', 'libx264', '-preset', 'slow', '-crf', '16', '-pix_fmt', 'yuv420p',
     '-c:a', 'copy', '-movflags', '+faststart', os.path.join(OUT, 'BosqueDeRegreso_Trailer_1080p.mp4')])
print('TOTAL', round(TOTAL, 2), 's')
