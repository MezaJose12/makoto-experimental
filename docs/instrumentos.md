# Instrumentos del Test (D-06 · especificación para T2-08)

Orden en la PWA: **PETS → Calidez → chequeos de manipulación → datos del participante**. Los ítems de PETS se presentan en orden aleatorio, como en la escala original.

## 1. PETS · Perceived Empathy of Technology Scale (principal)

Schmidmaier, M., Rupp, J., Cvetanova, D. y Mayer, S. (2024). *Perceived Empathy of Technology Scale (PETS): Measuring Empathy of Systems Toward the User*. CHI '24. https://doi.org/10.1145/3613904.3642035

- 10 ítems, deslizador de **0 a 100**: 0 = «Totalmente en desacuerdo», 100 = «Totalmente de acuerdo».
- «The system» se adapta como «el asistente».
- La traducción es directa, por el equipo. No hay una versión validada en español (sí en chino). Se declara como limitación; la retrotraducción queda opcional (T1-14).

| Código | Factor | Original | Español |
| --- | --- | --- | --- |
| PETS_E1 | ER | The system considered my mental state. | El asistente tuvo en cuenta mi estado mental. |
| PETS_E2 | ER | The system seemed emotionally intelligent. | El asistente parecía emocionalmente inteligente. |
| PETS_E3 | ER | The system expressed emotions. | El asistente expresó emociones. |
| PETS_E4 | ER | The system sympathized with me. | El asistente se solidarizó conmigo. |
| PETS_E5 | ER | The system showed interest in me. | El asistente mostró interés en mí. |
| PETS_E6 | ER | The system supported me in coping with an emotional situation. | El asistente me apoyó para afrontar una situación emocional. |
| PETS_U1 | UT | The system understood my goals. | El asistente entendió mis objetivos. |
| PETS_U2 | UT | The system understood my needs. | El asistente entendió mis necesidades. |
| PETS_U3 | UT | I trusted the system. | Confié en el asistente. |
| PETS_U4 | UT | The system understood my intentions. | El asistente entendió mis intenciones. |

**Puntuación:**
- PETS-ER = media de E1 a E6.
- PETS-UT = media de U1 a U4.
- PETS total = 0,6 · ER + 0,4 · UT, en un rango de 0 a 100.

Ninguno de los ítems es invertido.

## 2. Calidez percibida

Adjetivos de la dimensión de calidez del modelo de contenido de estereotipos: Fiske, Cuddy, Glick y Xu (2002); Cuddy, Fiske y Glick (2008).

- Escala de **1 a 7**: 1 = «Nada», 7 = «Totalmente».
- Pregunta: «La voz del asistente me pareció…»
- Se usa «la voz», sustantivo femenino en español, para todas las condiciones. Así el adjetivo no cambia según el género de la voz.

| Código | Ítem |
| --- | --- |
| CAL_1 | cálida |
| CAL_2 | amigable |
| CAL_3 | bien intencionada |
| CAL_4 | sincera |

**Puntuación:** media de los 4 ítems.

## 3. Chequeos de manipulación

Se registran para comprobar que la manipulación se percibió. No entran en el índice de empatía.

| Código | Pregunta | Respuesta |
| --- | --- | --- |
| CHK_GENERO | La voz que escuchaste era… | Femenina / Masculina / No sabría decir |
| CHK_TONO | El tono de la voz era… | 1 = Muy grave … 7 = Muy agudo |
| CHK_NATURALIDAD | ¿Qué tan natural sonó la voz? | 1 = Nada natural … 7 = Totalmente natural (`evaluaciones.naturalidad`) |
| CHK_ESCUCHA | ¿Pudiste escuchar el audio completo y con claridad? | Sí / No. Si es No, la sesión se excluye del análisis |

## 4. Datos del participante (covariables)

Sin datos identificativos.

| Campo | Respuesta |
| --- | --- |
| Edad | Número entre 18 y 99 |
| Género | Femenino / Masculino / Otro / Prefiero no decir |
| Experiencia con asistentes de voz | 1 = Nunca … 5 = A diario |
