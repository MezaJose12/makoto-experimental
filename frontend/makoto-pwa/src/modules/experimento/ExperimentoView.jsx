import { useState } from 'react'
import { sesiones, ApiError } from '../../services/api.js'

const VERSION_CONSENTIMIENTO = 'v1-borrador'

// T2-18: en Render la API exige el código del estudio (Estudio__CodigoAcceso). Se entrega a los
// participantes dentro del enlace o QR del laboratorio: https://<servicio>.onrender.com/?codigo=XXXX
// No se escribe en el código de la PWA (sería público); se lee del enlace al abrir la página.
const leerCodigoAcceso = () => new URLSearchParams(window.location.search).get('codigo') ?? undefined

// Paso 1 del flujo del estudiante: consentimiento → POST /api/sesiones.
// El escenario y el reproductor bloqueado se construyen en T2-07.
export default function ExperimentoView() {
  const [acepta, setAcepta] = useState(false)
  const [estado, setEstado] = useState({ tipo: 'inicio' })

  async function comenzar() {
    setEstado({ tipo: 'cargando' })
    try {
      const sesion = await sesiones.iniciar(VERSION_CONSENTIMIENTO, leerCodigoAcceso())
      setEstado({ tipo: 'ok', sesion })
    } catch (e) {
      const pendiente = e instanceof ApiError && e.status === 501
      // T2-18: 403 = falta el código del estudio o es incorrecto (enlace incompleto)
      const sinCodigo = e instanceof ApiError && e.status === 403
      setEstado({ tipo: pendiente ? 'pendiente' : sinCodigo ? 'sin-codigo' : 'error', mensaje: e.message })
    }
  }

  return (
    <section className="tarjeta">
      <h1>Consentimiento informado</h1>
      <p className="nota">Texto definitivo pendiente de aprobación (T1-04 / T1-06).</p>
      <label className="check">
        <input type="checkbox" checked={acepta} onChange={(e) => setAcepta(e.target.checked)} />
        He leído la información y acepto participar de forma voluntaria y anónima.
      </label>
      <button disabled={!acepta || estado.tipo === 'cargando'} onClick={comenzar}>
        {estado.tipo === 'cargando' ? 'Conectando…' : 'Comenzar'}
      </button>
      {estado.tipo === 'ok' && <p role="status">Sesión creada: {estado.sesion.sesionId}</p>}
      {estado.tipo === 'pendiente' && <p role="status" className="aviso">La API respondió: {estado.mensaje}</p>}
      {estado.tipo === 'sin-codigo' && <p role="alert" className="error">El enlace no tiene un código de estudio válido. Pida el enlace correcto a quien coordina la sesión.</p>}
      {estado.tipo === 'error' && <p role="alert" className="error">No se pudo conectar con la API: {estado.mensaje}</p>}
    </section>
  )
}
