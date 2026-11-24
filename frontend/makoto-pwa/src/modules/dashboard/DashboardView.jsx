import { useState } from 'react'
import { admin } from '../../services/api.js'

// Lee cuotas y condiciones reales desde PostgreSQL: prueba la cadena PWA → API → base de datos.
// La clave temporal se guarda solo en esta pestaña (sessionStorage). En T2-20 se reemplaza por login JWT.
const leerClave = () => { try { return sessionStorage.getItem('makoto-admin') ?? '' } catch { return '' } }

export default function DashboardView() {
  const [clave, setClave] = useState(leerClave)
  const [datos, setDatos] = useState(null)
  const [error, setError] = useState('')

  async function cargar(e) {
    e.preventDefault()
    setError('')
    try {
      const [condiciones, cuotas] = await Promise.all([admin.condiciones(clave), admin.cuotas(clave)])
      try { sessionStorage.setItem('makoto-admin', clave) } catch { /* sin almacenamiento */ }
      setDatos({ condiciones, cuotas })
    } catch (err) {
      setDatos(null)
      setError(err.status === 401 ? 'Clave incorrecta.' : `No se pudo cargar: ${err.message}`)
    }
  }

  return (
    <section className="tarjeta ancha">
      <h1>Dashboard</h1>
      <form onSubmit={cargar} className="fila">
        <label htmlFor="clave">Clave de administrador</label>
        <input id="clave" type="password" value={clave} onChange={(e) => setClave(e.target.value)} autoComplete="off" />
        <button disabled={!clave}>Ver cuotas</button>
      </form>
      {error && <p role="alert" className="error">{error}</p>}
      {datos && (
        <table>
          <caption>Cuotas del diseño 2×3</caption>
          <thead>
            <tr><th>Celda</th><th>Género</th><th>Nivel</th><th>F0 objetivo (Hz)</th><th>Audio</th><th>Completadas</th><th>En curso</th></tr>
          </thead>
          <tbody>
            {datos.cuotas.map((c) => {
              const cond = datos.condiciones.find((x) => x.codigo === c.codigo)
              return (
                <tr key={c.codigo}>
                  <td>{c.codigo}</td><td>{c.genero}</td><td>{c.nivel}</td>
                  <td>{cond?.f0ObjetivoHz}</td><td>{cond?.certificada ? 'Certificado' : 'Pendiente'}</td>
                  <td>{c.completadas}</td><td>{c.enCurso}</td>
                </tr>
              )
            })}
          </tbody>
        </table>
      )}
    </section>
  )
}
