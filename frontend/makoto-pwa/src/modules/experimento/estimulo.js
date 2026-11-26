// D-01 y D-05 (25/11/2026): el estímulo es fijo e igual para las 6 condiciones; solo cambia la voz.
// ESCENARIO: el mensaje del estudiante que se muestra en pantalla (interfaz tipo chat, un turno).
// La respuesta del asistente es el audio, sintetizado desde tools/audio-pipeline/guion.txt.
// Si cambia el guion, hay que volver a ejecutar sintetizar.py y certificar.py.
export const ESCENARIO =
  'Tengo varios exámenes esta semana y un proyecto que todavía no termino. ' +
  'Siento que no me alcanza el tiempo y ya no sé por dónde empezar.'
