# Plan de QA — Arte V6

## Objetivo

Comprobar que el arte V6:

- no rompe el gameplay ni el contrato de escena;
- no crea colisiones o pasos engañosos (ver algo sólido que no bloquea, o chocar con algo que no se ve);
- se lee bien y cuenta la restauración;
- y rinde en el hardware del curso.

## Alcance

**Dentro:**
- `Game/Assets/_Bosque/Art/V6/`;
- la escena de muestra `Art/V6/Showcase/ArroyoV6_Showcase.unity`, que es una copia jugable de `ArroyoPlayable` vestida con V6;
- la build de muestra;
- la revalidación de I-01 a I-05.

**Fuera, hasta la integración:**
- la build integrada de `ArroyoPlayable`, que se cubrirá con el caso M15;
- audio;
- el protagonista definitivo.

## Entrada

- La entrega de Juan coincide con su manifiesto SHA256 (750 archivos).
- Unity 6000.3.22f1.
- Ningún otro proceso de Unity usando `Game/`.

## Suites

| Suite | Método | Casos |
|---|---|---|
| Contrato | Sonda: hashes, colliders, componentes, marcadores, IDs y colliders sin visual | Q01–Q04, Q18 |
| Recursos | Sonda: shaders, prefabs, importación y partículas | Q05, Q16 |
| Gameplay | Sonda: conduce `ThirdPersonMotor`, `PlayerInteractor` y `ArroyoSession` reales por API | Q06–Q10 |
| Cámara y nivel | Sonda: 2736 posiciones de cámara por estado y barrido de la cápsula del jugador contra mallas V6 | Q11–Q13 |
| Legibilidad | Sonda: collider frente a visual de cada obstáculo | Q14 |
| Visual | Análisis de las capturas reales (16 desde la corrección de las P1) | Q15 |
| Rendimiento | Build de muestra con `-v6-perf`, 1080p, fases de 20 s intercaladas | Q17 |
| Manual | Teclado y ratón, persona | M01–M15 |
| UX | Persona ajena al proyecto | U01–U04 |

## Criterios de salida para integrar

- 0 FALLA en la sonda.
- K03 por debajo del 2 % de las celdas.
- Sin incidencias P0 ni P1 abiertas.
- M01 a M14 aprobados.
- p95 de 16,67 ms o menos en la PC de la presentación (o una decisión documentada del equipo sobre el riesgo).

## Riesgos

- **Decorado sin collider:** las rocas, troncos y raíces V6 son solo visuales, así que el jugador o la cámara pueden atravesarlos. Lo vigilan Q12 y Q18.
- **Colliders sin visual:** al ocultar los renderers antiguos pueden quedar paredes invisibles. Es lo que pasó con QA6-013 y QA6-014.
- **Rendimiento dependiente de la temperatura** del portátil.
- **Las capturas automáticas no incluyen el HUD**, que se dibuja en una capa de pantalla. Su legibilidad solo se puede comprobar a mano.
