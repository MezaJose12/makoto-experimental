import { useState } from 'react'
import { sesiones, ApiError } from '../../services/api.js'

const VERSION_CONSENTIMIENTO = 'v1-borrador'

// Paso 1 del flujo del estudiante: consentimiento → POST /api/sesiones.
// El escenario y el reproductor bloqueado se construyen en T2-07.
export default function ExperimentoView() {
  const [acepta, setAcepta] = useState(false)
  const [estado, setEstado] = useState({ tipo: 'inicio' })

  async function comenzar() {
    setEstado({ tipo: 'cargando' })
    try {
      const sesion = await sesiones.iniciar(VERSION_CONSENTIMIENTO)
      setEstado({ tipo: 'ok', sesion })
    } catch (e) {
      const pendiente = e instanceof ApiError && e.status === 501
      setEstado({ tipo: pendiente ? 'pendiente' : 'error', mensaje: e.message })
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
      {estado.tipo === 'error' && <p role="alert" className="error">No se pudo conectar con la API: {estado.mensaje}</p>}
    </section>
  )
}
