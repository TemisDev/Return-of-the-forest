#!/usr/bin/env python3

import argparse, csv, struct
from pathlib import Path
p=argparse.ArgumentParser(description='Audita recursos Art sin modificar Unity.')
p.add_argument('--repo',type=Path,default=Path.cwd())
a=p.parse_args(); repo=a.repo.resolve(); art=repo/'Game/Assets/_Bosque/Art'
if not art.is_dir(): p.error('No existe Game/Assets/_Bosque/Art; ejecutar desde la raíz o usar --repo.')
out=Path(__file__).resolve().parents[1]/'AUDITORIA-RECURSOS.csv'
rows=[]
for f in sorted(art.rglob('*')):
    if f.suffix.lower()=='.meta': continue
    if not (f.is_file() or f.is_dir()): continue
    if f.is_symlink(): continue
    problems=[]; detail=''; size=''
    if not Path(str(f)+'.meta').is_file(): problems.append('META AUSENTE')
    if f.is_file():
        size=f.stat().st_size
        with f.open('rb') as stream: head=stream.read(128)
        if head.startswith(b'version https://git-lfs.github.com/spec/v1'): problems.append('PUNTERO LFS SIN MATERIALIZAR')
        elif f.suffix.lower()=='.png':
            if len(head)<24 or head[:8]!=b'\x89PNG\r\n\x1a\n' or head[12:16]!=b'IHDR': problems.append('CABECERA PNG INVALIDA')
            else:
                w,h=struct.unpack('>II',head[16:24]); detail=f'{w}x{h}'
                if not w or not h: problems.append('DIMENSION INVALIDA')
                if max(w,h)>2048: problems.append('REVISAR TEXTURA >2048px')
        elif f.suffix.lower()=='.obj':
            v=n=0
            with f.open(encoding='utf-8',errors='replace') as stream:
                for line in stream:
                    t=line.split()
                    if t and t[0]=='v': v+=1
                    if t and t[0]=='f': n+=1
            detail=f'vertices={v}; caras={n}'
            if not v or not n: problems.append('REVISAR OBJ SIN GEOMETRIA')
    rows.append([f.relative_to(repo).as_posix(),'directorio' if f.is_dir() else f.suffix.lower(),size,detail,'; '.join(problems) or 'SIN ALERTA ESTRUCTURAL'])
with out.open('w',encoding='utf-8-sig',newline='') as stream:
    w=csv.writer(stream); w.writerow(['ruta','tipo','bytes','detalle','alertas']);w.writerows(rows)
print(f'{len(rows)} entradas; {sum(r[-1]!="SIN ALERTA ESTRUCTURAL" for r in rows)} con alertas. {out}')
print('No verifica importación Unity, materiales, referencias GUID, licencias ni calidad visual.')
