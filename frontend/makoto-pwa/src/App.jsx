import { NavLink, Route, Routes } from 'react-router-dom'
import ExperimentoView from './modules/experimento/ExperimentoView.jsx'
import TestView from './modules/test/TestView.jsx'
import DashboardView from './modules/dashboard/DashboardView.jsx'

export default function App() {
  return (
    <>
      <header className="barra">
        <strong>Makoto</strong>
        <nav aria-label="Módulos">
          <NavLink to="/" end>Experimento</NavLink>
          <NavLink to="/test">Test</NavLink>
          <NavLink to="/admin">Dashboard</NavLink>
        </nav>
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
