# REV-TEC 03 · Asignador de condiciones y entrega de la pista

**Tarea:** T2-06 (revisión del Tutor 2) · cubre T2-05 y T2-17
**Sprint:** 2 · ventana del martes 24/11/2026
**Repositorio:** https://github.com/MezaJose12/makoto-experimental

## 1. Qué se revisa

| Paso | Componente | Qué hace | Dónde está |
| --- | --- | --- | --- |
| 1 | Asignador de condiciones | Asigna a cada estudiante una de las 6 celdas 2×3 (género × F0) en PostgreSQL, balanceando las cuotas | `backend/Makoto.Api/Services/AsignadorCondiciones.cs` |
| 2 | Entrega de la pista | Sirve el MP3 certificado de la celda asignada desde el volumen o la imagen | `SesionesController.Audio` |

Azure Speech (síntesis) y Praat (medición de F0) **no se invocan en ejecución**: forman un pipeline offline (`tools/audio-pipeline`, T2-03 y T2-04) que produce los 6 MP3 y `certificacion.json`. La API solo lee esos archivos y comprueba su SHA-256 al arrancar.

## 2. Flujo

```text
PWA ──POST /api/sesiones──▶ SesionesController.Iniciar
                              │ 400 sin consentimiento · 403 código del estudio incorrecto
                              ▼
                     AsignadorCondiciones.IniciarAsync  (una transacción)
                       1. pg_advisory_xact_lock(7231001)      ← serializa asignaciones simultáneas
                       2. Expira reservas > 30 min            ← libera plazas abandonadas
                       3. Ocupación por celda certificada     ← completadas + en curso
                       4. Celda menos ocupada (empate: azar)
                       5. INSERT sesión + código MK-XXXXXXXX
                              │
PWA ◀── 201 {sesionId, estado, audioUrl} ──┘

PWA ──GET /api/sesiones/{id}/audio──▶ 200 audio/mpeg (Range, no-store)
                                       404 no existe · 409 terminada o expirada · 503 sin certificar
```

## 3. Decisiones que se aplican

- **D-08 (aleatorización por bloques de 6 con cuota por sesión):** elegir siempre la celda menos ocupada, con empates al azar, equivale a bloques permutados de 6. Mientras no expire ninguna reserva, cada grupo de 6 asignaciones consecutivas contiene cada celda una vez, en orden aleatorio. Si un estudiante abandona, su plaza se libera a los 30 min y la siguiente asignación la rellena. La prueba unitaria `600 asignaciones → 100 por celda` lo demuestra.
- **Candado y transacción:** `pg_advisory_xact_lock` se libera solo al terminar la transacción. Funciona igual en Docker y en Neon, también con el *pooler* de Neon (modo transacción). Se ejecuta dentro de la estrategia de reintentos de EF Core (`EnableRetryOnFailure`).
- **Datos mínimos (D-07):** la sesión guarda un `Guid` v7 y un código `MK-XXXXXXXX` generado en el servidor. No guarda IP, nombre ni correo.
- **Reproductor bloqueado (D-09):** el audio se sirve con `Cache-Control: no-store`, y la PWA no guarda en caché `/api` ni el audio.

## 4. Demostración

### A. Local (Docker Compose)

```powershell
docker compose up --build
powershell -ExecutionPolicy Bypass -File tools\revision\prueba-asignador.ps1 -ClaveAdmin <ADMIN_CLAVE_TEMPORAL> -Sesiones 30
```

### B. Contra Neon (rama «desarrollo»)

1. En el `.env`, ponga `NEON_DATABASE_URL=<URI de la rama desarrollo>`. Nunca use la rama «producción».
2. `docker compose up --build`. El log debe mostrar la migración aplicada en Neon y «Audios certificados: 6 de 6».
3. `Invoke-RestMethod http://localhost:8080/health/ready`: `postgresql` y `audios` deben estar en `Healthy`.
4. `powershell -ExecutionPolicy Bypass -File tools\revision\prueba-asignador.ps1 -ClaveAdmin <clave> -Sesiones 30`
5. Al terminar, ejecute `tools/revision/limpiar-pruebas.sql` en el SQL Editor de Neon y deje `NEON_DATABASE_URL` vacío otra vez.

Sin Docker: `cd backend\Makoto.Api`, luego `$env:DATABASE_URL="<URI>"`, `$env:Admin__ClaveTemporal="<clave>"` y `dotnet run`.

El script envía las N solicitudes **al mismo tiempo** y comprueba que:
- todas responden `201`,
- la diferencia entre la celda más llena y la más vacía es como máximo 1,
- el audio de una sesión llega como `audio/mpeg`.

## 5. Evidencia

| Criterio (planificación) | Cómo se comprueba | Resultado |
| --- | --- | --- |
| Asigna celda 2×3 en PostgreSQL | `POST /api/sesiones` → `201` y Dashboard con 1 en curso | ☐ local ☐ Neon |
| Entrega la pista desde el volumen o la imagen | `GET …/audio` → `200 audio/mpeg`, se escucha | ☐ local ☐ Neon |
| 20–50 solicitudes simultáneas sin colisiones | `prueba-asignador.ps1 -Sesiones 30` → APROBADO | ☐ local ☐ Neon |
| 60 simultáneas → 10 por celda | CI, trabajo «Docker Compose»: `[10, 10, 10, 10, 10, 10]` | ☐ |
| Reglas del Asignador | `dotnet test`: `AsignadorCondicionesTests` (5) y `ApiTests` 400/403 | ☐ |
| Solo celdas certificadas | Celda con huella incorrecta → no se asigna; ninguna → `503` | ☐ |

Adjunte capturas de la salida del script (local y Neon), del Dashboard y de la ejecución de la CI.

## 6. Fuera del alcance de esta revisión

- Telemetría play/fin (T2-10) y Test PETS (T2-08). Sus endpoints responden `501`.
- Prueba de carga de 50–100 sesiones (T2-21, 04/12) y login JWT del panel (T2-20).
- Despliegue en Render (T2-18). Los MP3 certificados ya están exceptuados en `.gitignore` para que viajen en la imagen (R-15).

## 7. Riesgos

| Riesgo | Estado tras T2-05 |
| --- | --- |
| R-06 Colisión de cuotas con sesiones simultáneas | Mitigado: candado + prueba de 60 en la CI + script contra Neon |
| R-13 Arranque en frío de Neon | La primera solicitud puede tardar unos segundos; `EnableRetryOnFailure` reintenta. Despierte Neon antes de la demostración con `/health/ready`. |
| Sesiones de prueba mezcladas con el estudio | Se crean con `consentimientoVersion = "prueba-rev-tec-03"` o `"ci"` y se borran con `limpiar-pruebas.sql` |
