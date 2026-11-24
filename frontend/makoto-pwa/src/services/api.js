// Único lugar de la PWA que habla con la API. Las vistas nunca usan fetch directamente.
const BASE_URL = import.meta.env.VITE_API_URL ?? ''

export class ApiError extends Error {
  constructor(status, problem) {
    super(problem?.detail || problem?.title || `Error HTTP ${status}`)
    this.status = status
    this.problem = problem
  }
}

async function request(path, { method = 'GET', body, headers = {} } = {}) {
  const response = await fetch(`${BASE_URL}${path}`, {
    method,
    headers: { ...(body ? { 'Content-Type': 'application/json' } : {}), ...headers },
    body: body ? JSON.stringify(body) : undefined,
  })
  const isJson = response.headers.get('content-type')?.includes('json')
  const data = isJson ? await response.json() : null
  if (!response.ok) throw new ApiError(response.status, data)
  return data
}

// ── Estudiante ──
export const sesiones = {
  iniciar: (consentimientoVersion, codigoAcceso) =>
    request('/api/sesiones', { method: 'POST', body: { consentimientoAceptado: true, consentimientoVersion, codigoAcceso } }),
  obtener: (id) => request(`/api/sesiones/${id}`),
}

// ── Administrador (clave temporal hasta el login JWT de T2-20) ──
const conClave = (clave) => ({ headers: { 'X-Admin-Key': clave } })
export const admin = {
  condiciones: (clave) => request('/api/admin/condiciones', conClave(clave)),
  cuotas: (clave) => request('/api/admin/cuotas', conClave(clave)),
}

export const salud = () => request('/health/ready')
