#!/usr/bin/env python3
"""Suma estados de la matriz V4 sin asignar ni inferir resultados de prueba."""
import csv
from collections import Counter, defaultdict
from pathlib import Path

root=Path(__file__).resolve().parents[1]
path=root/'MATRIZ-QA-V4.csv'
rows=list(csv.DictReader(path.open(encoding='utf-8-sig', newline='')))
statuses=('APROBADO','FALLIDO','BLOQUEADO','NO EJECUTADO')
by_group=defaultdict(Counter)
errors=[]
for row in rows:
    status=(row.get('estado') or '').strip().upper()
    if status not in statuses:
        errors.append(f"{row['ID']}: estado desconocido {status!r}")
    by_group[row['suite']][status]+=1
if errors:
    raise SystemExit('Corregir estados en el CSV:\n'+'\n'.join(errors))

out=['# Resumen automático QA V4 (no sustituye el informe de aceptación)','',
     f'- Casos en matriz: {len(rows)}','- Fuente: MATRIZ-QA-V4.csv',
     '- ATENCIÓN: un estado solo acredita ejecución si existen evidencias reales.','',
     '| Suite | APROBADO | FALLIDO | BLOQUEADO | NO EJECUTADO |',
     '|---|---:|---:|---:|---:|']
for suite in sorted(by_group):
    counts=by_group[suite]
    out.append('| '+suite+' | '+' | '.join(str(counts[x]) for x in statuses)+' |')
tot=Counter(x['estado'].strip().upper() for x in rows)
out.append('| **TOTAL** | '+' | '.join('**'+str(tot[x])+'**' for x in statuses)+' |')
out.append('')
if any(r['prioridad'] in {'P0','P1'} and r['estado'].strip().upper() == 'FALLIDO' for r in rows):
    out.append('**Atención:** existen fallos P0/P1; emitir incidencias y bloquear aceptación hasta decisión responsable.')
else:
    out.append('**Nota:** ausencia de fallos documentados no acredita aprobación de pruebas pendientes.')
out.append('')
report=root/'RESUMEN-EJECUCION-GENERADO.md'
report.write_text('\n'.join(out),encoding='utf-8')
print(f'Resumen generado: {report}')
print('Estados:',dict(tot))
