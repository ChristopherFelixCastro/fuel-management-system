import { useEffect, useState, useCallback } from 'react'
import type { FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import { tanksApi } from '../api/tanks.api'
import { getMyAdjustments, reportAdjustment, type AdjustmentStatus } from '../api/adjustments.api'
import type { Tank } from '../types/tank'

export function AdjustmentPage() {
  const navigate = useNavigate()
  const { user, session, refreshSession } = useAuth()

  const [tanks, setTanks] = useState<Tank[]>([])
  const [tankId, setTankId] = useState('')
  const [count, setCount] = useState('')
  const [reason, setReason] = useState('')

  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; text: string } | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isLoadingReports, setIsLoadingReports] = useState(false)
  const [reports, setReports] = useState<AdjustmentStatus[]>([])

  const loadReports = useCallback(async () => {
    if (!session) return
    setIsLoadingReports(true)
    try {
      const data = await getMyAdjustments(session, refreshSession)
      setReports(data)
    } catch (e: unknown) {
      // Si falla la carga de reportes previos no bloquea el formulario
      console.warn('Error al cargar reportes:', e)
    } finally {
      setIsLoadingReports(false)
    }
  }, [session, refreshSession])

  useEffect(() => {
    if (session && user?.stationId) {
      tanksApi
        .getStationTanks(user.stationId, session, { refreshSession })
        .then((result) => {
          setTanks(result)
          if (result.length === 1) {
            setTankId(result[0].id)
          }
        })
        .catch((e) => {
          setFeedback({
            type: 'error',
            text: e instanceof Error ? e.message : 'Error al cargar tanques de la estación.',
          })
        })
    }
  }, [session, user?.stationId, refreshSession])

  useEffect(() => {
    void loadReports()
  }, [loadReports])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!session) return

    setFeedback(null)
    setIsSubmitting(true)

    try {
      const r = await reportAdjustment(session, refreshSession, {
        tanqueId: tankId,
        conteoFisico: Number(count),
        motivo: reason.trim(),
      })

      setFeedback({
        type: 'success',
        text: `Reporte registrado con estado ${r.estado}. No se modificó el stock hasta aprobación de un supervisor.`,
      })
      setCount('')
      setReason('')
      void loadReports()
    } catch (err: unknown) {
      setFeedback({
        type: 'error',
        text: err instanceof Error ? err.message : 'Error al enviar el reporte de conteo físico.',
      })
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="closure-page">
      <header className="closure-header">
        <button
          type="button"
          className="button-secondary"
          onClick={() => navigate('/')}
        >
          ← Volver
        </button>
        <div>
          <h1>Reportar ajuste de inventario</h1>
          <p>Registra un conteo físico para revisión y autorización del supervisor.</p>
        </div>
      </header>

      <section className="closure-card">
        <div className="closure-card-heading">
          <h2>Nuevo reporte de conteo</h2>
        </div>

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          <div className="form-group">
            <label htmlFor="adjustment-tank">Tanque *</label>
            <select
              id="adjustment-tank"
              required
              value={tankId}
              onChange={(e) => setTankId(e.target.value)}
            >
              <option value="">Seleccione un tanque</option>
              {tanks.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.codigo} · {t.nombre} ({t.combustibleNombre})
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="adjustment-count">Conteo físico reportado (galones) *</label>
            <input
              id="adjustment-count"
              required
              min="0"
              step="0.01"
              type="number"
              inputMode="decimal"
              value={count}
              onChange={(e) => setCount(e.target.value)}
              placeholder="Ej. 1250.50"
            />
          </div>

          <div className="form-group">
            <label htmlFor="adjustment-reason">Motivo u observaciones *</label>
            <textarea
              id="adjustment-reason"
              required
              rows={3}
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Describa el motivo o las observaciones del conteo físico..."
            />
          </div>

          <div className="closure-warning">
            <strong>Flujo de control:</strong>
            <span>
              Este reporte genera una solicitud de ajuste físico. El inventario se mantendrá inalterado
              hasta que un supervisor revise y apruebe el registro.
            </span>
          </div>

          {feedback && (
            <div
              className={feedback.type === 'success' ? 'closure-card' : 'form-error'}
              role="alert"
              style={
                feedback.type === 'success'
                  ? { backgroundColor: '#ecfdf5', borderColor: '#a7f3d0', color: '#065f46' }
                  : undefined
              }
            >
              <span>{feedback.text}</span>
            </div>
          )}

          <button
            type="submit"
            className="btn-primary"
            disabled={isSubmitting || !tankId || !count || !reason.trim()}
          >
            {isSubmitting ? (
              <>
                <span className="spinner" />
                <span>Enviando reporte...</span>
              </>
            ) : (
              <span>Enviar para aprobación</span>
            )}
          </button>
        </form>
      </section>

      <section className="closure-card">
        <div className="closure-card-heading" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h2>Mis reportes</h2>
          {isLoadingReports && <span className="spinner" />}
        </div>

        {reports.length === 0 ? (
          <p style={{ color: 'var(--slate-500)', fontSize: '0.875rem', margin: '0.5rem 0 0' }}>
            No tienes reportes de conteo físico registrados.
          </p>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', marginTop: '0.5rem' }}>
            {reports.map((r) => {
              const badgeStyle: React.CSSProperties =
                r.estado === 'APROBADO'
                  ? { backgroundColor: '#ecfdf5', color: '#065f46', border: '1px solid #a7f3d0' }
                  : r.estado === 'RECHAZADO'
                    ? { backgroundColor: '#fef2f2', color: '#991b1b', border: '1px solid #fecaca' }
                    : { backgroundColor: '#fffbeb', color: '#92400e', border: '1px solid #fde68a' }

              return (
                <article
                  key={r.id}
                  style={{
                    padding: '0.85rem',
                    borderRadius: 'var(--radius-md)',
                    border: '1px solid var(--slate-200)',
                    background: 'var(--slate-50)',
                  }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <span style={{ fontWeight: 700, color: 'var(--slate-900)' }}>
                      Tanque {r.tanqueCodigo}
                    </span>
                    <span
                      style={{
                        padding: '0.2rem 0.55rem',
                        borderRadius: 'var(--radius-full)',
                        fontSize: '0.75rem',
                        fontWeight: 700,
                        ...badgeStyle,
                      }}
                    >
                      {r.estado}
                    </span>
                  </div>

                  <p style={{ margin: '0.4rem 0 0.2rem', fontSize: '0.875rem', color: 'var(--slate-700)' }}>
                    Conteo: <strong>{r.conteoFisico.toLocaleString('es-DO', { minimumFractionDigits: 2 })} gal</strong>
                  </p>

                  <p style={{ margin: 0, fontSize: '0.825rem', color: 'var(--slate-600)' }}>
                    {r.motivo}
                  </p>

                  {r.motivoRechazo && (
                    <div
                      style={{
                        marginTop: '0.5rem',
                        padding: '0.5rem 0.75rem',
                        borderRadius: 'var(--radius-sm)',
                        backgroundColor: '#fef2f2',
                        border: '1px solid #fecaca',
                        color: '#991b1b',
                        fontSize: '0.8rem',
                      }}
                    >
                      <strong>Motivo de rechazo:</strong> {r.motivoRechazo}
                    </div>
                  )}
                </article>
              )
            })}
          </div>
        )}
      </section>
    </main>
  )
}
