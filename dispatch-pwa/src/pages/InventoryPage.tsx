import { useEffect, useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import { tanksApi } from '../api/tanks.api'
import type { Tank } from '../types/tank'

export function InventoryPage() {
  const navigate = useNavigate()
  const { user, session, refreshSession } = useAuth()
  const [tanks, setTanks] = useState<Tank[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const loadTanks = useCallback(async () => {
    if (!session || !user?.stationId) {
      setIsLoading(false)
      return
    }

    setIsLoading(true)
    setError(null)

    try {
      const data = await tanksApi.getStationTanks(
        user.stationId,
        session,
        { refreshSession }
      )
      setTanks(data)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'No fue posible consultar las existencias.')
    } finally {
      setIsLoading(false)
    }
  }, [session, user?.stationId, refreshSession])

  useEffect(() => {
    void loadTanks()
  }, [loadTanks])

  const stationTitle = tanks[0]?.estacionNombre
    ? `Estación ${tanks[0].estacionNombre}`
    : user?.stationId
      ? `Estación ${user.stationId.slice(0, 8).toUpperCase()}`
      : 'Mi estación'

  return (
    <main className="closure-page">
      <header className="closure-header" style={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <button
            type="button"
            className="button-secondary"
            onClick={() => navigate('/')}
          >
            ← Volver
          </button>
          <div>
            <h1>Existencias de combustible</h1>
            <p>{stationTitle}</p>
          </div>
        </div>

        <button
          type="button"
          className="button-secondary"
          onClick={() => void loadTanks()}
          disabled={isLoading}
          title="Actualizar existencias"
        >
          {isLoading ? 'Consultando...' : '↻ Actualizar'}
        </button>
      </header>

      {error && (
        <div className="form-error" role="alert">
          <span>{error}</span>
        </div>
      )}

      {isLoading && tanks.length === 0 ? (
        <section className="closure-card" style={{ textAlign: 'center', padding: '2.5rem 1rem' }}>
          <span className="spinner" style={{ margin: '0 auto 1rem', display: 'inline-block' }} />
          <p style={{ margin: 0, color: 'var(--slate-600)', fontSize: '0.9rem' }}>
            Consultando existencias de la estación...
          </p>
        </section>
      ) : tanks.length === 0 ? (
        <section className="closure-card" style={{ textAlign: 'center', padding: '2.5rem 1rem' }}>
          <p style={{ margin: 0, color: 'var(--slate-600)', fontSize: '0.9rem' }}>
            No se encontraron tanques registrados para esta estación.
          </p>
        </section>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {tanks.map((tank) => {
            const pct = tank.capacidadMaxima > 0
              ? Math.min(100, Math.max(0, Math.round((tank.stockActual / tank.capacidadMaxima) * 100)))
              : 0

            return (
              <section key={tank.id} className="closure-card">
                <div className="closure-card-heading" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '0.75rem' }}>
                  <div>
                    <span className="ticket-label">{tank.codigo}</span>
                    <h2 style={{ margin: '0.25rem 0 0' }}>{tank.nombre || 'Tanque de combustible'}</h2>
                  </div>
                  <span
                    style={{
                      display: 'inline-block',
                      padding: '0.25rem 0.65rem',
                      borderRadius: 'var(--radius-full)',
                      backgroundColor: 'var(--slate-100)',
                      border: '1px solid var(--slate-200)',
                      fontSize: '0.75rem',
                      fontWeight: 600,
                      color: 'var(--slate-800)',
                      whiteSpace: 'nowrap',
                    }}
                  >
                    {tank.combustibleNombre}
                  </span>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginTop: '0.5rem' }}>
                  <div style={{ background: 'var(--slate-50)', padding: '0.85rem', borderRadius: 'var(--radius-md)', border: '1px solid var(--slate-200)' }}>
                    <span style={{ display: 'block', fontSize: '0.75rem', color: 'var(--slate-500)', textTransform: 'uppercase', letterSpacing: '0.04em', fontWeight: 600 }}>
                      Existencia actual
                    </span>
                    <strong style={{ display: 'block', fontSize: '1.25rem', color: 'var(--slate-900)', marginTop: '0.25rem' }}>
                      {tank.stockActual.toLocaleString('es-DO', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} gal
                    </strong>
                  </div>

                  <div style={{ background: 'var(--slate-50)', padding: '0.85rem', borderRadius: 'var(--radius-md)', border: '1px solid var(--slate-200)' }}>
                    <span style={{ display: 'block', fontSize: '0.75rem', color: 'var(--slate-500)', textTransform: 'uppercase', letterSpacing: '0.04em', fontWeight: 600 }}>
                      Capacidad máxima
                    </span>
                    <strong style={{ display: 'block', fontSize: '1.25rem', color: 'var(--slate-700)', marginTop: '0.25rem' }}>
                      {tank.capacidadMaxima.toLocaleString('es-DO', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} gal
                    </strong>
                  </div>
                </div>

                <div style={{ marginTop: '0.5rem' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.8rem', color: 'var(--slate-600)', marginBottom: '0.35rem' }}>
                    <span>Nivel de llenado</span>
                    <span style={{ fontWeight: 700, color: 'var(--slate-900)' }}>{pct}%</span>
                  </div>
                  <div
                    style={{
                      width: '100%',
                      height: '10px',
                      backgroundColor: 'var(--slate-200)',
                      borderRadius: 'var(--radius-full)',
                      overflow: 'hidden',
                    }}
                  >
                    <div
                      style={{
                        width: `${pct}%`,
                        height: '100%',
                        backgroundColor: '#087e8b',
                        borderRadius: 'var(--radius-full)',
                        transition: 'width 0.4s ease',
                      }}
                    />
                  </div>
                </div>
              </section>
            )
          })}
        </div>
      )}
    </main>
  )
}
