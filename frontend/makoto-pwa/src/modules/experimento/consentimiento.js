// D-07 (25/11/2026): texto del consentimiento informado que ve cada participante.
// Si cambia cualquier frase, suba la VERSION: se guarda en cada sesión (consentimiento_version)
// para saber qué texto aceptó cada persona.
// v2 (25/11): diseño intra-sujeto, el participante escucha y evalúa todas las voces activas
export const VERSION_CONSENTIMIENTO = 'v2'

export const CONSENTIMIENTO = {
  titulo: 'Consentimiento informado',
  secciones: [
    {
      titulo: '¿De qué se trata?',
      texto:
        'Es un estudio académico de la carrera de Ingeniería en Tecnologías de la Información de la ' +
        'Universidad de las Fuerzas Armadas ESPE. Queremos conocer cómo perciben los estudiantes ' +
        'distintas voces de un asistente de apoyo emocional.',
    },
    {
      titulo: '¿Qué harás?',
      texto:
        'Leerás el mensaje de un estudiante con estrés académico y escucharás varias respuestas de voz del ' +
        'asistente (unos 40 segundos cada una; usa audífonos o el altavoz con volumen suficiente). Después de ' +
        'cada una responderás unas preguntas y al final compararás las voces. En total, unos 25 minutos.',
    },
    {
      titulo: 'Participación voluntaria',
      texto:
        'Participar es voluntario. Puedes detenerte en cualquier momento cerrando la página, sin ninguna ' +
        'consecuencia.',
    },
    {
      titulo: 'Tus datos están protegidos',
      texto:
        'Tu participación es anónima: no pedimos tu nombre, cédula ni correo, y no guardamos tu dirección IP. ' +
        'Recibirás un código aleatorio. No se graba tu voz ni tu imagen. Las respuestas se guardan en una base ' +
        'de datos con conexión cifrada, a la que solo accede el equipo investigador.',
    },
    {
      titulo: 'Uso de la información',
      texto:
        'Las respuestas se usarán únicamente con fines estadísticos y académicos, analizadas en conjunto ' +
        '(promedios y comparaciones entre grupos). No se divulgarán respuestas individuales ni se compartirán ' +
        'con terceros.',
    },
    {
      titulo: 'Si te sientes mal',
      texto:
        'El tema es el estrés académico. Si en algún momento te genera malestar, puedes detenerte. ' +
        'El área de Bienestar Estudiantil de tu universidad puede escucharte y orientarte. ' +
        'Si tienes preguntas sobre el estudio, consulta a la persona que coordina la sesión.',
    },
  ],
  aceptacion:
    'Tengo 18 años o más, he leído esta información y acepto participar de forma voluntaria y anónima.',
}
