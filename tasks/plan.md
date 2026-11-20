# Plan de producción — Bosque de regreso
Fecha de planificación: 6 de noviembre de 2026.
Entrega: 27 de noviembre de 2026, 18:00 America/Lima.
Estado: planificación creada; no se ha iniciado el desarrollo.
Actualización del 11 de noviembre de 2026: se inicia T05 con creación de proyecto Unity. Se mantiene el calendario del GDD, sin marcar hitos anteriores como completados automáticamente.

## Coordinación de asistentes
El usuario usará Claude Code con acceso al proyecto. Codex lidera base técnica, gameplay e integración. Claude Code prepara herramientas de nivel, interfaz y pruebas dentro de carpetas separadas. Los roles humanos se mantienen.
- Reparto y secuencia: [COORDINACION-IA](../Docs/COORDINACION-IA.md).
- Marcadores y límites de integración: [CONTRATOS](../Docs/CONTRATOS.md).
- Mensaje de inicio: [ENCARGO-PARA-CLAUDE](../Docs/ENCARGO-PARA-CLAUDE.md).
- Estado de cada asistente: Docs/Handoffs/.
- Proyecto: Game/. Editor elegido entre los instalados: 6000.3.22f1.

## Objetivo
Prototipo Unity 3D de una aventura sobre ODS 15. Un mapa pequeño, tres hábitats, puzles ambientales y restauración que abre rutas. Diseño completo en ../GDD-Bosque-de-regreso-v0.1.md.

## Responsables
- Tú: programación e integración (elección confirmada).
- Juan: arte 3D, animación y audio (reparto propuesto).
- Diego: QA, documentación y apoyo a diseño de niveles (reparto propuesto).
Los nombres figuran en títulos y descripciones de Trello. No se identificaron perfiles de los compañeros y no se enviaron invitaciones.

## Decisiones
Primero validar arroyo con primitivas. Añadir crecimiento y zonas solo tras validar el recorrido inicial. Usar prefabs modulares, estados por zona y eventos. Fijar editor y paquetes en T05. El guardado persistente y sistemas complejos quedan fuera del prototipo.

## Hitos y comprobaciones
- H1, 9/11: revisar alcance, narrativa, mapa y guía visual.
- H2, 13/11: completar primera zona sin guía verbal.
- H3, 20/11: grabar recorrido completo de tres hábitats.
- H4, 27/11: build verificada en otra PC Windows, GDD actualizado y presentación.
Juan puede preparar el kit modular desde la semana 1 y Diego prueba cada nueva entrega.

## Índice de tareas
El seguimiento vive en Trello: https://trello.com/b/rv1JFCL7/bosque-de-regreso-gdd-y-desarrollo-unity-3d
- H1 · T01 · Diego · [Confirmar alcance y ODS](https://trello.com/c/Jc50dNEY/7-t01-diego-confirmar-alcance-y-ods)
- H1 · T02 · Diego · [Definir historia y objetivos de zona](https://trello.com/c/bunRSW03/8-t02-diego-definir-historia-y-objetivos-de-zona)
- H1 · T03 · Diego · [Dibujar mapa del recorrido](https://trello.com/c/Y4BSPoGx/9-t03-diego-dibujar-mapa-del-recorrido)
- H1 · T04 · Juan · [Definir guía visual](https://trello.com/c/iY8T8fUT/10-t04-juan-definir-gu%C3%ADa-visual)
- H2 · T05 · Tú · [Preparar proyecto compartido](https://trello.com/c/9dpB8lah/11-t05-t%C3%BA-preparar-proyecto-compartido)
- H2 · T06 · Tú · [Prototipar movimiento y cámara](https://trello.com/c/twE0IXX1/12-t06-t%C3%BA-prototipar-movimiento-y-c%C3%A1mara)
- H2 · T07 · Diego · [Montar zona inicial en bloques](https://trello.com/c/kRSad2H7/13-t07-diego-montar-zona-inicial-en-bloques)
- H2 · T08 · Tú · [Crear interacción contextual](https://trello.com/c/pKoxljl3/14-t08-t%C3%BA-crear-interacci%C3%B3n-contextual)
- H2 · T09 · Tú · [Implementar recuperación del arroyo](https://trello.com/c/TOe65kqc/15-t09-t%C3%BA-implementar-recuperaci%C3%B3n-del-arroyo)
- H2 · T10 · Diego · [Validar primera zona jugable](https://trello.com/c/Z2zJpCBh/16-t10-diego-validar-primera-zona-jugable)
- H3 · T11 · Juan · [Crear kit modular de bosque](https://trello.com/c/0BNTr3fx/17-t11-juan-crear-kit-modular-de-bosque)
- H3 · T12 · Juan · [Preparar protagonista animada](https://trello.com/c/TWao1nTg/18-t12-juan-preparar-protagonista-animada)
- H3 · T13 · Tú · [Implementar habilidad de crecimiento](https://trello.com/c/GWTEnzl8/19-t13-t%C3%BA-implementar-habilidad-de-crecimiento)
- H3 · T14 · Diego · [Montar claro y refugio en bloques](https://trello.com/c/2D1iM7dd/20-t14-diego-montar-claro-y-refugio-en-bloques)
- H3 · T15 · Tú · [Implementar puzle del claro](https://trello.com/c/rcPa7C1x/21-t15-t%C3%BA-implementar-puzle-del-claro)
- H3 · T16 · Tú · [Implementar puzle del refugio y final](https://trello.com/c/vaIMPr5t/22-t16-t%C3%BA-implementar-puzle-del-refugio-y-final)
- H3 · T17 · Juan · [Preparar fauna y estados visuales](https://trello.com/c/TiHuNflL/23-t17-juan-preparar-fauna-y-estados-visuales)
- H3 · T18 · Diego · [Validar recorrido completo](https://trello.com/c/bo3wcNoM/24-t18-diego-validar-recorrido-completo)
- H4 · T19 · Juan · [Preparar interfaz visual y audio](https://trello.com/c/lUW9CooZ/25-t19-juan-preparar-interfaz-visual-y-audio)
- H4 · T20 · Tú · [Integrar interfaz y controles de sesión](https://trello.com/c/QUzOcxnK/26-t20-t%C3%BA-integrar-interfaz-y-controles-de-sesi%C3%B3n)
- H4 · T21 · Diego · [Realizar pruebas con tres jugadores](https://trello.com/c/ezb7m5pb/27-t21-diego-realizar-pruebas-con-tres-jugadores)
- H4 · T22 · Tú · [Corregir bloqueos y medir rendimiento](https://trello.com/c/JMeDP2cC/28-t22-t%C3%BA-corregir-bloqueos-y-medir-rendimiento)
- H4 · T23 · Juan · [Revisar recursos y créditos](https://trello.com/c/VqJd7BGw/29-t23-juan-revisar-recursos-y-cr%C3%A9ditos)
- H4 · T24 · Diego · [Preparar entrega y presentación](https://trello.com/c/Ga3NQijY/30-t24-diego-preparar-entrega-y-presentaci%C3%B3n)

## Riesgos y mitigación
- Tres semanas y programación concentrada en una persona: evitar nuevos sistemas y usar recursos licenciados coherentes.
- Si T10 no está validada el 13/11: reducir a una zona completa con un final breve; diferir las otras dos.
- Conflictos de escenas: acordar propietario por sesión, usar prefabs y conservar .meta.
- Hardware sin confirmar: medir rendimiento en la PC designada; 30 FPS a 1080p es una meta provisional.
- Referencias biológicas sin validar: bosque y especies ficticias; evitar afirmaciones científicas específicas sin investigación.

## Pendientes
Experiencia de Juan y Diego, perfiles Trello, conformidad con sus roles propuestos, editor y hardware.

## Avance del primer corte — 11/11/2026
- Editor confirmado: Unity 6000.3.22f1. T05 conserva pendiente la apertura en otro equipo.
- C-01/T07: nivel integrado y validación estructural sin errores. Pendiente aceptación manual de Diego.
- T06: controlador y cámara implementados en ArroyoPlayable; 6/6 pruebas automatizadas aprobadas en Unity. Pendiente recorrido manual con teclado y ratón.
- Claude C-02: revisar recorrido y ampliar QA. Codex siguiente: T08 y T09. La UI espera los contratos de progreso y sesión.

## Entregable 02 — 16/11/2026
T08/T09 implementadas y entregadas para revisión en https://github.com/TemisDev/Return-of-the-forest/pull/1, desde feature/arroyo-gameplay-v2 hacia develop. 21/21 pruebas aprobadas, incluyendo T06 y Play Mode automatizado; build Windows x64 correcta. Contratos en Docs/INTEGRACION-ARROYO-V2.md y evidencia en Docs/ENTREGABLE-02-RESULTADOS.md.
Pendiente validación manual de Diego, hardware/rendimiento y otra PC. C-02 ya fue entregada; no repetir el encargo inicial. Este ciclo termina el 22 de noviembre y se limita al arroyo.
