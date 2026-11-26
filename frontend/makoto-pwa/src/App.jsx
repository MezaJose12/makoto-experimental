import { Route, Routes } from 'react-router-dom'
import ExperimentoView from './modules/experimento/ExperimentoView.jsx'
import TestView from './modules/test/TestView.jsx'
import DashboardView from './modules/dashboard/DashboardView.jsx'

export default function App() {
  return (
    <>
      {/* T2-07: el participante no ve enlaces al Test ni al Dashboard (Figma P1–P6). El panel se abre en /admin. */}
      <header className="barra">
        <strong className="marca">Makoto</strong>
        <span className="nota">Estudio de voz y empatía</span>
      </header>
      <main className="contenido">
        <Routes>
          <Route path="/" element={<ExperimentoView />} />
          <Route path="/test" element={<TestView />} />
          <Route path="/admin" element={<DashboardView />} />
          <Route path="*" element={<p>Página no encontrada.</p>} />
        </Routes>
      </main>
    </>
  )
}
