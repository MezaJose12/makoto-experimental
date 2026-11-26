import { useEffect, useRef, useState } from 'react'

// T2-07 · Reproductor bloqueado (Figma P3). Garantiza que cada voz se escuche completa y a velocidad normal:
//  · sin controles nativos (no hay barra para adelantar ni menú de velocidad);
//  · si algo intenta adelantar (teclado, pantalla de bloqueo, extensiones), vuelve al punto máximo escuchado;
//  · si algo cambia la velocidad, la devuelve a 1×;
//  · se puede pausar y continuar; «onTerminado» se llama solo al llegar al final.
const formato = (s) => (Number.isFinite(s) ? `${Math.floor(s / 60)}:${String(Math.floor(s % 60)).padStart(2, '0')}` : '0:00')

export default function ReproductorBloqueado({ src, onTerminado }) {
  const audio = useRef(null)
  const maximo = useRef(0)
  const [estado, setEstado] = useState('listo') // listo · reproduciendo · pausado · terminado · error
  const [tiempo, setTiempo] = useState({ actual: 0, total: 0 })

  // Cada voz monta un reproductor nuevo (key={audioUrl} en el padre): el estado empieza de cero.

  // Quita «adelantar/retroceder/siguiente» de la pantalla de bloqueo del celular (Media Session)
  useEffect(() => {
    const ms = navigator.mediaSession
    if (!ms) return
    for (const accion of ['seekforward', 'seekbackward', 'seekto', 'nexttrack', 'previoustrack']) {
      try { ms.setActionHandler(accion, null) } catch { /* el navegador no admite esa acción */ }
    }
  }, [])

  async function alternar() {
    const a = audio.current
    if (!a || estado === 'terminado') return
    if (a.paused) {
      try { await a.play() } catch { setEstado('error') }
    } else {
      a.pause()
    }
  }

  const eventos = {
    onPlay: () => setEstado('reproduciendo'),
    onPause: (e) => { if (!e.currentTarget.ended) setEstado('pausado') },
    onLoadedMetadata: (e) => setTiempo((t) => ({ ...t, total: e.currentTarget.duration })),
    onTimeUpdate: (e) => {
      const a = e.currentTarget
      if (!a.seeking) maximo.current = Math.max(maximo.current, a.currentTime)
      setTiempo({ actual: a.currentTime, total: a.duration })
    },
    // Intento de adelantar: vuelve a lo ya escuchado (retroceder sí se permite)
    onSeeking: (e) => {
      const a = e.currentTarget
      if (a.currentTime > maximo.current + 0.5) a.currentTime = maximo.current
    },
    onRateChange: (e) => { if (e.currentTarget.playbackRate !== 1) e.currentTarget.playbackRate = 1 },
    onEnded: () => { setEstado('terminado'); onTerminado?.() },
    onError: () => setEstado('error'),
  }

  const avance = tiempo.total ? Math.min(100, (tiempo.actual / tiempo.total) * 100) : 0
  const sonando = estado === 'reproduciendo'

  return (
    <div className="reproductor">
      <audio ref={audio} src={src} preload="auto" controlsList="nodownload noplaybackrate" disableRemotePlayback {...eventos} />
      <div className="controles">
        <button type="button" className="redondo" onClick={alternar} disabled={estado === 'terminado' || estado === 'error'}
          aria-label={sonando ? 'Pausar' : 'Reproducir'}>
          {estado === 'terminado' ? '✓' : sonando ? '❚❚' : '▶'}
        </button>
        <div className="avance" role="progressbar" aria-label="Avance del audio" aria-valuemin={0} aria-valuemax={100} aria-valuenow={Math.round(avance)}>
          <div style={{ width: `${avance}%` }} />
        </div>
        <span className="nota">{formato(tiempo.actual)} / {formato(tiempo.total)}</span>
      </div>
      {estado === 'error'
        ? <p role="alert" className="error">No se pudo reproducir el audio. Revise la conexión y recargue la página.</p>
        : <p className="nota">🔒 No se puede adelantar ni cambiar la velocidad. Escucha hasta el final para continuar.</p>}
    </div>
  )
}
