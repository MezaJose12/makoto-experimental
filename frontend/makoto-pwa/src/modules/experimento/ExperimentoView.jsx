import { useEffect, useState } from 'react'
import { sesiones, urlApi, ApiError } from '../../services/api.js'
// D-07: texto definitivo del consentimiento y su versión
import { CONSENTIMIENTO, VERSION_CONSENTIMIENTO } from './consentimiento.js'
// D-05: mensaje del estudiante que se muestra en pantalla
import { ESCENARIO } from './estimulo.js'
import ReproductorBloqueado from './ReproductorBloqueado.jsx'

// T2-18: el código del estudio llega en el enlace del laboratorio (?codigo=XXXX), nunca en el código de la PWA
const leerCodigoAcceso = () => new URLSearchParams(window.location.search).get('codigo') ?? undefined

// T2-07: si el estudiante recarga la página, continúa en la misma sesión (solo dura lo que la pestaña)
const CLAVE_SESION = 'makoto.sesion'
const guardarSesion = (id) => { try { sessionStorage.setItem(CLAVE_SESION, id) } catch { /* modo privado */ } }
const leerSesion = () => { try { return sessionStorage.getItem(CLAVE_SESION) } catch { return null } }

// T2-07: tono de 1 s generado en el navegador para ajustar el volumen (no descarga ningún archivo)
function tonoDePrueba() {
  const Contexto = window.AudioContext || window.webkitAudioContext
  if (!Contexto) return
  const ctx = new Contexto()
  const osc = ctx.createOscillator()
  const vol = ctx.createGain()
  osc.frequency.value = 440
  vol.gain.setValueAtTime(0.0001, ctx.currentTime)
  vol.gain.exponentialRampToValueAtTime(0.3, ctx.currentTime + 0.05)
  vol.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + 1.2)
  osc.connect(vol).connect(ctx.destination)
  osc.start()
  osc.stop(ctx.currentTime + 1.25)
  osc.onended = () => ctx.close()
}

const Burbuja = ({ quien, estudiante, children }) => (
  <div className="mensaje">
    <span className="quien">{quien}</span>
    <div className={estudiante ? 'burbuja estudiante' : 'burbuja'}>{children}</div>
  </div>
)

// T2-07: barra de avance (una franja por voz: hecha, actual, pendiente)
function Progreso({ pistas, actual, evaluando }) {
  return (
    <div className="progreso" aria-label={`Voz ${actual} de ${pistas.length}`}>
      {pistas.map((p) => (
        <span key={p.indice} className={p.indice < actual || (p.indice === actual && evaluando) ? 'hecho' : p.indice === actual ? 'actual' : ''} />
      ))}
    </div>
  )
}

const MensajeError = ({ texto }) => texto ? <p role="alert" className="error">{texto}</p> : null

// T2-07 · Flujo del participante (Figma P1–P3; P4–P6 llegan con T2-08):
// consentimiento → prueba de audio y situación → [escucha de la voz k → evaluación de la voz k] × n → fin
export default function ExperimentoView() {
  // Sin sesión guardada se empieza directamente en el consentimiento
  const [paso, setPaso] = useState(() => (leerSesion() ? 'cargando' : 'consentimiento'))
  const [sesion, setSesion] = useState(null)    // { sesionId, pistas, pistasEscuchadas }
  const [actual, setActual] = useState(1)       // voz que se escucha o evalúa (1..n)
  const [acepta, setAcepta] = useState(false)
  const [oye, setOye] = useState(false)
  const [terminada, setTerminada] = useState(false)
  const [error, setError] = useState(null)
  const [ocupado, setOcupado] = useState(false)

  // Retomar una sesión de esta pestaña
  useEffect(() => {
    const id = leerSesion()
    if (!id) return
    sesiones.obtener(id)
      .then((s) => {
        if (s.estado === 'Completada' || s.estado === 'Expirada') { setPaso('consentimiento'); return }
        setSesion(s)
        const total = s.pistas.length
        if (s.pistasEscuchadas >= total) { setActual(total); setPaso('fin') }
        else { setActual(s.pistasEscuchadas + 1); setPaso(s.pistasEscuchadas === 0 ? 'preparacion' : 'escucha') }
      })
      .catch(() => setPaso('consentimiento'))
  }, [])

  async function aceptar() {
    setOcupado(true); setError(null)
    try {
      const s = await sesiones.iniciar(VERSION_CONSENTIMIENTO, leerCodigoAcceso())
      guardarSesion(s.sesionId)
      setSesion(s); setActual(1); setPaso('preparacion')
    } catch (e) {
      setError(e instanceof ApiError && e.status === 403
        ? 'El enlace no tiene un código de estudio válido. Pide el enlace correcto a quien coordina la sesión.'
        : `No se pudo iniciar la sesión: ${e.message}`)
    } finally { setOcupado(false) }
  }

  async function alTerminarAudio() {
    setTerminada(true)
    try { await sesiones.finPista(sesion.sesionId, actual) }
    catch (e) { setError(`No se pudo registrar el final del audio: ${e.message}`) }
  }

  function siguiente() {
    setTerminada(false); setError(null)
    if (actual >= sesion.pistas.length) setPaso('fin')
    else { setActual(actual + 1); setPaso('escucha') }
  }

  const total = sesion?.pistas.length ?? 0
  const pista = sesion?.pistas[actual - 1]

  if (paso === 'cargando') return <p className="nota centro">Cargando…</p>

  // ── P1 · Consentimiento ──
  if (paso === 'consentimiento') return (
    <div className="pila">
      <section className="tarjeta titulo-proyecto">
        <h1>Efecto del género y la frecuencia vocal en la empatía percibida de un chatbot de apoyo emocional estudiantil</h1>
        <p className="nota">Universidad de las Fuerzas Armadas ESPE · Ingeniería en Tecnologías de la Información</p>
      </section>
      <section className="tarjeta">
        <h1>{CONSENTIMIENTO.titulo}</h1>
        {/* D-07: qué hará, que es voluntario, que sus datos son anónimos y solo se usan estadísticamente */}
        {CONSENTIMIENTO.secciones.map((s) => (
          <div key={s.titulo}><h2>{s.titulo}</h2><p>{s.texto}</p></div>
        ))}
        <div className="fila">
          <label className="check">
            <input type="checkbox" checked={acepta} onChange={(e) => setAcepta(e.target.checked)} />
            {CONSENTIMIENTO.aceptacion}
          </label>
          <button disabled={!acepta || ocupado} onClick={aceptar}>{ocupado ? 'Conectando…' : 'Aceptar y comenzar'}</button>
        </div>
        <MensajeError texto={error} />
      </section>
    </div>
  )

  // ── P2 · Prueba de audio y situación ──
  if (paso === 'preparacion') return (
    <div className="pila">
      <section className="tarjeta">
        <h1>Antes de empezar: prueba tu audio</h1>
        <p className="nota">Ponte audífonos o sube el volumen. Reproduce el sonido de prueba y confirma que lo escuchas con claridad.</p>
        <div className="fila">
          <button className="secundario" onClick={tonoDePrueba}>▶ Reproducir sonido de prueba</button>
          <label className="check"><input type="checkbox" checked={oye} onChange={(e) => setOye(e.target.checked)} /> Lo escucho con claridad</label>
        </div>
      </section>
      <section className="tarjeta">
        <h1>La situación</h1>
        <p className="nota">Imagina que eres un estudiante y escribes este mensaje al asistente de apoyo. Luego escucharás {total} respuestas, cada una con una voz distinta. El contenido es el mismo: fíjate en cómo te hace sentir cada voz.</p>
        <Burbuja quien="Tú" estudiante>{ESCENARIO}</Burbuja>
        <button disabled={!oye} onClick={() => setPaso('escucha')}>Escuchar la primera voz</button>
      </section>
    </div>
  )

  // ── P3 · Escucha de la voz k ──
  if (paso === 'escucha') return (
    <div className="pila">
      <Progreso pistas={sesion.pistas} actual={actual} evaluando={false} />
      <section className="tarjeta">
        <p className="nota">{pista.etiqueta} de {total}</p>
        <Burbuja quien="Tú" estudiante>{ESCENARIO}</Burbuja>
        <Burbuja quien={`Asistente · ${pista.etiqueta}`}>
          <ReproductorBloqueado key={pista.audioUrl} src={urlApi(pista.audioUrl)} onTerminado={alTerminarAudio} />
        </Burbuja>
        <div className="fila">
          <button disabled={!terminada} onClick={() => setPaso('evaluacion')}>Evaluar esta voz</button>
          {!terminada && <span className="nota">Se activa al terminar el audio</span>}
        </div>
        <MensajeError texto={error} />
      </section>
    </div>
  )

  // ── P4 · Evaluación de la voz k (el cuestionario PETS · Calidez llega en T2-08) ──
  if (paso === 'evaluacion') return (
    <div className="pila">
      <Progreso pistas={sesion.pistas} actual={actual} evaluando />
      <section className="tarjeta">
        <h1>¿Qué te pareció la {pista.etiqueta}?</h1>
        <p className="aviso">Aquí irá el cuestionario PETS y Calidez de esta voz (T2-08).</p>
        <button onClick={siguiente}>{actual >= total ? 'Ir a la comparación final' : 'Siguiente voz'}</button>
      </section>
    </div>
  )

  // ── P5–P6 · Comparación final y cierre (T2-08) ──
  return (
    <section className="tarjeta">
      <h1>Escuchaste las {total} voces</h1>
      <p className="nota">La comparación final y tus datos anónimos se agregan en T2-08.</p>
    </section>
  )
}
