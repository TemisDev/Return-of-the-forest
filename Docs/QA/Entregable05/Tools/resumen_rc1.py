import csv, re
from pathlib import Path
from collections import Counter
base=Path(__file__).resolve().parents[1]
with (base/'PUERTA-RC1.csv').open(encoding='utf-8-sig',newline='') as f: rows=list(csv.DictReader(f))
with (base/'INCIDENCIAS-RC1.csv').open(encoding='utf-8-sig',newline='') as f: issues=list(csv.DictReader(f))
states={'APROBADO','FALLIDO','BLOQUEADO','NO EJECUTADO'}; counts=Counter(); alerts=[]; blockers=[]; commits=set(); soft=[]
if not rows: alerts.append('Matriz vacía.')
for r in rows:
    s=r['estado'].strip().upper(); counts[s]+=1; ident=r['id']; sha=r['sha_build'].strip().lower()
    if s not in states: alerts.append(f'{ident}: estado inválido')
    if s in {'APROBADO','FALLIDO'}:
        if not re.fullmatch(r'[0-9a-f]{7,40}',sha): alerts.append(f'{ident}: SHA ausente o inválido')
        else: commits.add(sha)
        if not r['entorno'].strip() or not r['resultado'].strip(): alerts.append(f'{ident}: entorno o resultado ausente')
        ev=r['evidencia'].strip(); path=(base/ev).resolve()
        if not ev or not path.is_relative_to((base/'EVIDENCIAS').resolve()) or not path.is_file() or path.name=='LEEME.md': alerts.append(f'{ident}: evidencia local faltante o inválida')
        if s=='FALLIDO' and not r['incidencia'].strip(): alerts.append(f'{ident}: falta ID de incidencia')
    if s=='BLOQUEADO' and not r['resultado'].strip(): alerts.append(f'{ident}: motivo de bloqueo ausente')
    if r['prioridad'] in {'P0','P1'} and s!='APROBADO': blockers.append(f'{ident}: {s}')
    if r['prioridad'] not in {'P0','P1'} and s!='APROBADO': soft.append(f'{ident}: {s}')
for i in issues:
    if i['severidad'].strip().upper() in {'P0','P1'} and i['estado'].strip().upper() not in {'CERRADO','CERRADA'}: blockers.append(f"Incidencia {i['id']}: {i['estado']}")
if len(commits)>1: alerts.append('Se mezclan SHAs de build; separar las ejecuciones.')
result='NO APTA PARA ACEPTACIÓN' if blockers or alerts else 'REVISIÓN HUMANA REQUERIDA'
lines=['# Resumen de puerta RC1',f'**{result}**',f'Casos: {len(rows)}']
lines += [f'- {s}: {counts[s]}' for s in sorted(states)]
for title,entries in [('Bloqueos P0/P1',blockers),('Alertas de documentación',alerts),('Casos no críticos pendientes',soft)]: lines += ['',f'## {title}'] + ([f'- {x}' for x in entries] or ['- Ninguno registrado.'])
lines += ['', 'Este resumen valida presencia formal de datos, no autenticidad de evidencia ni comportamiento. No evalúa la matriz V4 completa, el acta de Arte ni la aprobación humana.']
(base/'RESUMEN-RC1.md').write_text('\n'.join(lines)+'\n',encoding='utf-8'); print(result)
