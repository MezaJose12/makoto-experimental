# T2-19 · Pruebas del reproductor en móviles y navegadores

**Objetivo (planificación):** tener la matriz de compatibilidad completa o un plan B documentado.
**Qué se prueba:** el flujo del participante y el reproductor bloqueado (T2-07) en la URL de Render: `https://<servicio>.onrender.com/?codigo=<código>`.

## Antes de empezar

1. Abra `/health/ready` unos minutos antes. Render gratis tarda unos 50 s en despertar.
2. Al terminar, borre las sesiones de prueba con `tools/revision/limpiar-pruebas.sql`. Use `consentimiento_version = 'v2'` y la fecha de hoy, o limpie toda la rama de desarrollo.
3. Use una ventana nueva o de incógnito en cada dispositivo. La sesión se guarda por pestaña.

## Dispositivos

| Código | Dispositivo y navegador | Versión del sistema | Quién |
| --- | --- | --- | --- |
| A1 | Android · Chrome | | |
| A2 | Android · Samsung Internet (si hay) | | |
| I1 | iPhone · Safari | | |
| I2 | iPhone · PWA instalada (Compartir → «Agregar a inicio») | | |
| W1 | PC del laboratorio · Chrome o Edge | | |
| W2 | PC · Firefox | | |

## Casos

Marque ✅ (cumple), ❌ (falla: describa en «Hallazgos») o — (no aplica).

| # | Caso | Resultado esperado | A1 | A2 | I1 | I2 | W1 | W2 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Abrir el enlace con `?codigo=` y aceptar el consentimiento | Pasa a «Prueba tu audio» | | | | | | |
| 2 | Abrir el enlace **sin** código | Aviso «El enlace no tiene un código de estudio válido» | | | | | | |
| 3 | Botón «Reproducir sonido de prueba» | Suena un tono de 1 s. En iPhone, con el interruptor de silencio activado puede no sonar (ver plan B) | | | | | | |
| 4 | Reproducir la Voz 1 | Empieza al tocar ▶. Avance y tiempo se mueven | | | | | | |
| 5 | Pausar y continuar | Continúa donde quedó | | | | | | |
| 6 | Intentar adelantar: tocar la barra, teclas → o L, controles de la pantalla de bloqueo | No adelanta; si salta, vuelve al punto escuchado | | | | | | |
| 7 | Intentar cambiar la velocidad (menú del navegador o extensión) | Sigue a 1× | | | | | | |
| 8 | Botón «siguiente» o doble toque en audífonos Bluetooth | No salta de voz ni adelanta | | | | | | |
| 9 | Bloquear la pantalla durante el audio | El audio sigue. Al desbloquear tras el final, «Evaluar esta voz» está activo | | | | | | |
| 10 | Cambiar a otra app y volver | El audio se pausa o sigue, pero nunca se salta. Se puede continuar | | | | | | |
| 11 | Llamada o notificación con sonido durante el audio | Se pausa; se puede continuar desde ahí | | | | | | |
| 12 | «Evaluar esta voz» antes de terminar | Desactivado hasta el final del audio | | | | | | |
| 13 | Recargar la página en la Voz 2 | Retoma en «Voz 2 de 6», no vuelve al consentimiento | | | | | | |
| 14 | Completar las 6 voces | Llega a «Escuchaste las 6 voces» | | | | | | |
| 15 | Volumen percibido entre voces | Las 6 suenan igual de fuertes (todas a 70 dB) | | | | | | |
| 16 | Red lenta (Chrome DevTools → «Slow 4G», o datos móviles) | El audio carga y se reproduce sin cortes largos | | | | | | |
| 17 | Girar a horizontal | El diseño se mantiene legible, sin desplazamiento lateral | | | | | | |
| 18 | Tiempo total del flujo de escucha (6 voces, sin cuestionario) | Unos 4–5 min | | | | | | |

## Hallazgos

| # | Dispositivo | Caso | Qué pasó | Gravedad (Must / Should / Could) | Plan B o corrección |
| --- | --- | --- | --- | --- | --- |
| | | | | | |

## Planes B conocidos

| Riesgo | Plan B |
| --- | --- |
| iPhone con el interruptor de silencio activado: el tono de prueba (Web Audio) no suena, aunque las voces (`<audio>`) sí | Indicar en la pantalla «Desactiva el modo silencio». Opción técnica: reemplazar el tono por un MP3 corto de prueba |
| Un navegador permite adelantar con algún control no previsto | El servidor no entrega la voz siguiente sin el aviso de fin, y la telemetría (T2-10) registrará el tiempo real de escucha para excluir sesiones dudosas |
| La pantalla de bloqueo muestra controles de adelantar en algún modelo | Pedir a los participantes que no bloqueen la pantalla; queda registrado en la telemetría |
| Render tarda en despertar al inicio de una sesión de laboratorio | Abrir `/health/ready` 5 min antes, o plan Starter durante la recolección |
| Un dispositivo no reproduce MP3 | Usar los PC del laboratorio (W1) para esa persona |

## Resultado

- [ ] Matriz completa en al menos A1, I1 y W1.
- [ ] Hallazgos «Must» corregidos o con plan B.
- [ ] Capturas o video corto de los casos 6, 9 y 13 como evidencia.
