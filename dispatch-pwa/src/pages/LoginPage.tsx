import { useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
export function LoginPage() {
  const { login, isAuthenticated } = useAuth(); const navigate = useNavigate(); const location = useLocation()
  const [username, setUsername] = useState(''); const [password, setPassword] = useState(''); const [error, setError] = useState(''); const [isSubmitting, setIsSubmitting] = useState(false)
  if (isAuthenticated) return <Navigate to="/" replace />
  async function handleSubmit(event: FormEvent<HTMLFormElement>) { event.preventDefault(); setError(''); setIsSubmitting(true); try { await login({ username, password }); const destination = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname ?? '/'; navigate(destination, { replace: true }) } catch (reason) { setError(reason instanceof Error ? reason.message : 'No fue posible iniciar sesión.') } finally { setIsSubmitting(false) } }
  return (
    <main className="auth-page">
      <section className="auth-card">
        <header className="brand-header">
          <div className="brand-logo-container">
            <img
              src="/GasolinaLogo.png"
              alt="Logotipo de La Bomba"
              className="brand-logo"
            />
          </div>
          <div className="brand-title">
            <span>La Bomba</span>
            <span className="brand-badge">PWA</span>
          </div>
          <div className="auth-header-text">
            <h1>Acceso de Operador</h1>
          <p>Inicia sesión para abrir tu turno en estación</p>
          </div>
        </header>

        <form onSubmit={handleSubmit} noValidate>
          <div className="form-group">
            <label htmlFor="username">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2" />
                <circle cx="12" cy="7" r="4" />
              </svg>
              <span>Usuario del despachador</span>
            </label>
            <input
              id="username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              autoComplete="username"
              placeholder="Ej. christopher"
              required
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <rect width="18" height="11" x="3" y="11" rx="2" ry="2" />
                <path d="M7 11V7a5 5 0 0 1 10 0v4" />
              </svg>
              <span>Contraseña de acceso</span>
            </label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="current-password"
              placeholder="••••••••"
              required
            />
          </div>

          {error && (
            <p className="form-error" role="alert">
              <svg className="form-error-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              <span>{error}</span>
            </p>
          )}

          <button type="submit" className="btn-primary" disabled={isSubmitting}>
            {isSubmitting ? (
              <>
                <span className="spinner" aria-hidden="true" />
                <span>Validando credenciales…</span>
              </>
            ) : (
              <span>Ingresar al terminal</span>
            )}
          </button>
        </form>

      </section>
    </main>
  )
}
