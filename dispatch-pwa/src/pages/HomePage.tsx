import { useAuth } from '../contexts/AuthContext'
export function HomePage() {
  const { user, logout } = useAuth()

  return (
    <main className="home-page">
      {/* Barra superior con identidad y control de sesión */}
      <header className="topbar">
        <div className="topbar-brand">
          <img
            src="/GasolinaLogo.png"
            alt="PetroDespacho"
            className="topbar-logo"
          />
          <div className="topbar-info">
            <span className="topbar-title">PetroDespacho</span>
            <span className="topbar-subtitle">Terminal de Servicio</span>
          </div>
        </div>

        <button
          className="button-secondary"
          onClick={() => void logout()}
          title="Cerrar turno y salir del sistema"
        >
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
            <polyline points="16 17 21 12 16 7" />
            <line x1="21" y1="12" x2="9" y2="12" />
          </svg>
          <span>Cerrar sesión</span>
        </button>
      </header>

      {/* Credencial de Operador en Turno (Heurística: Reconocimiento inmediato) */}
      <section className="operator-card" aria-label="Información del operador activo">
        <div className="operator-card-header">
          <span className="operator-badge">
            <span className="status-dot" aria-hidden="true" />
            <span>Turno activo</span>
          </span>
          <span style={{ fontSize: '0.75rem', opacity: 0.8, letterSpacing: '0.04em' }}>ID: DISPATCH-01</span>
        </div>
        <h1 className="operator-name">{user?.name ?? 'Operador'}</h1>
        <p className="operator-station">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <path d="M12 2a8 8 0 0 0-8 8c0 5.25 8 12 8 12s8-6.75 8-12a8 8 0 0 0-8-8z" />
            <circle cx="12" cy="10" r="3" />
          </svg>
          <span>{user?.stationName ?? 'Estación de Combustible'}</span>
        </p>
      </section>

      {/* Módulo principal de despacho */}
      <div>
        <p className="section-title">Operación en pista</p>
        <section className="action-card">
          <div className="action-main">
            <div className="action-icon-wrapper" aria-hidden="true">
              <svg className="action-icon-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                <rect x="3" y="3" width="7" height="7" rx="1.5" />
                <rect x="14" y="3" width="7" height="7" rx="1.5" />
                <rect x="3" y="14" width="7" height="7" rx="1.5" />
                <rect x="14" y="14" width="3" height="3" />
                <path d="M17 14h3v3" />
                <path d="M14 20h6" />
                <line x1="7" y1="7" x2="7.01" y2="7" strokeWidth="2.5" />
                <line x1="17" y1="7" x2="17.01" y2="7" strokeWidth="2.5" />
                <line x1="7" y1="17" x2="7.01" y2="17" strokeWidth="2.5" />
              </svg>
            </div>
            <div className="action-details">
              <h2>Escanear Ticket QR</h2>
              <p>Lee el código QR presentado por el cliente para autorizar el combustible y validar el despacho en bomba.</p>
            </div>
          </div>

          <button type="button" disabled title="Módulo de escáner en fase de integración">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
              <circle cx="12" cy="12" r="10" />
              <polyline points="12 6 12 12 16 14" />
            </svg>
            <span>Módulo de escaneo (Próximamente)</span>
          </button>
        </section>
      </div>

      <div className="demo-note">
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <circle cx="12" cy="12" r="10" />
          <line x1="12" y1="16" x2="12" y2="12" />
          <line x1="12" y1="8" x2="12.01" y2="8" />
        </svg>
        <span>La integración de cámara web/móvil para lectura de QR se incorporará en el siguiente bloque.</span>
      </div>
    </main>
  )
}
