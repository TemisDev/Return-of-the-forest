import html
from pathlib import Path
base=Path(__file__).resolve().parents[1]; folder=base/'CAPTURAS'; out=base/'COMPARATIVA.html'
def panel(name,label):
    f=folder/name
    safe=html.escape(name,quote=True)
    if f.is_file(): return f'<figure><img src="CAPTURAS/{safe}" alt="{safe}"><figcaption>{html.escape(label)} · {safe}</figcaption></figure>'
    return f'<figure><div class="missing">FALTA: {safe}</div><figcaption>{html.escape(label)}</figcaption></figure>'
parts=['<!doctype html><html lang="es"><meta charset="utf-8"><title>Arroyo RC1: comparativa</title><style>body{font:16px system-ui;background:#f5f5f0;color:#24342e;margin:auto;max-width:1200px;padding:24px}section{display:grid;grid-template-columns:1fr 1fr;gap:16px}figure{margin:0;background:white;padding:8px}img{width:100%;height:auto;display:block}.missing{padding:36px;background:#e4e5df}h2{margin-top:28px;font-size:20px}@media(max-width:650px){section{grid-template-columns:1fr}}</style><h1>Arroyo RC1 — comparativa visual</h1><p>Identificar los commits y el entorno en REVISION-VISUAL.csv. La presencia de una imagen no certifica su contenido ni el resultado.</p>']
for name,title in [('spawn','Inicio'),('obstaculo','Obstáculo'),('restaurado','Arroyo recuperado'),('salida','Salida'),('camara','Cámara'),('hud','HUD')]:
    parts.extend([f'<h2>{title}</h2><section>',panel(name+'_base.png','Referencia'),panel(name+'_rc1.png','RC1'),'</section>'])
parts.extend(['<h2>Transformación en RC1 (mismo encuadre)</h2><section>',panel('estado_degradado_rc1.png','Degradado'),panel('estado_restaurado_rc1.png','Restaurado'),'</section></html>'])
out.write_text('\n'.join(parts),encoding='utf-8'); print(out)
