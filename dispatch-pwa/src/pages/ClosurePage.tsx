import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'

import { closuresApi } from '../api/closures.api'
import { tanksApi } from '../api/tanks.api'
import { useAuth } from '../contexts/AuthContext'

import type {
  Closure,
  ClosurePreview,
} from '../types/closure'
import type { Tank } from '../types/tank'

function getToday(): string {
  const now = new Date()
  const offset = now.getTimezoneOffset()

  return new Date(
    now.getTime() - offset * 60_000,
  )
    .toISOString()
    .slice(0, 10)
}

function gallons(value: number): string {
  return `${value.toLocaleString('es-DO', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })} gal`
}

function getErrorMessage(error: unknown): string {
  if (
    error instanceof Error &&
    error.message.trim().length > 0
  ) {
    return error.message
  }

  return 'Ocurri� un error inesperado.'
}

export function ClosurePage() {
  const navigate = useNavigate()

  const {
    user,
    session,
    refreshSession,
  } = useAuth()

  const [tanks, setTanks] = useState<Tank[]>([])
  const [tankId, setTankId] = useState('')
  const [date, setDate] = useState(getToday())

  const [preview, setPreview] =
    useState<ClosurePreview | null>(null)

  const [physicalStock, setPhysicalStock] =
    useState('')

  const [differenceReason, setDifferenceReason] =
    useState('')

  const [observations, setObservations] =
    useState('')

  const [loadingTanks, setLoadingTanks] =
    useState(true)

  const [loadingPreview, setLoadingPreview] =
    useState(false)

  const [submitting, setSubmitting] =
    useState(false)

  const [error, setError] =
    useState<string | null>(null)

  const [createdClosure, setCreatedClosure] =
    useState<Closure | null>(null)

  useEffect(() => {
  if (!session || !user?.stationId) {
    setLoadingTanks(false)
    return
  }

  const currentSession = session
  const stationId = user.stationId

  async function loadTanks() {
    setLoadingTanks(true)
    setError(null)

    try {
      const result =
        await tanksApi.getStationTanks(
          stationId,
          currentSession,
          { refreshSession },
        )

      setTanks(result)

      if (result.length === 1) {
        setTankId(result[0].id)
      }
    } catch (loadError) {
      setError(getErrorMessage(loadError))
      setTanks([])
    } finally {
      setLoadingTanks(false)
    }
  }

  void loadTanks()

  // La estación y la sesión identifican la carga inicial.
  // eslint-disable-next-line react-hooks/exhaustive-deps
}, [session?.accessToken, user?.stationId])
  useEffect(() => {
    setPreview(null)
    setPhysicalStock('')
    setDifferenceReason('')
    setCreatedClosure(null)
  }, [tankId, date])

  const selectedTank = useMemo(
    () => tanks.find(
      (tank) => tank.id === tankId,
    ),
    [tanks, tankId],
  )

  const numericPhysicalStock =
    physicalStock.trim() === ''
      ? null
      : Number(physicalStock)

  const difference =
    preview &&
    numericPhysicalStock !== null &&
    Number.isFinite(numericPhysicalStock)
      ? numericPhysicalStock -
        preview.stockTeoricoFinal
      : null

  const hasDifference =
    difference !== null &&
    Math.abs(difference) > 0.0001

  async function handlePreview() {
    if (!session || !tankId || !date) {
      return
    }

    setLoadingPreview(true)
    setError(null)
    setCreatedClosure(null)

    try {
      const result =
        await closuresApi.preview(
          tankId,
          date,
          session,
          { refreshSession },
        )

      setPreview(result)
      setPhysicalStock('')
      setDifferenceReason('')
    } catch (previewError) {
      setPreview(null)
      setError(
        getErrorMessage(previewError),
      )
    } finally {
      setLoadingPreview(false)
    }
  }

  async function handleSubmit(
    event: React.FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault()

    if (!session || !preview) {
      return
    }

    const stock = Number(physicalStock)

    if (
      physicalStock.trim() === '' ||
      !Number.isFinite(stock) ||
      stock < 0
    ) {
      setError(
        'Ingresa una existencia f�sica final v�lida.',
      )
      return
    }

    if (
      hasDifference &&
      differenceReason.trim().length === 0
    ) {
      setError(
        'Debes indicar el motivo de la diferencia entre la existencia f�sica y la te�rica.',
      )
      return
    }

    setSubmitting(true)
    setError(null)

    try {
      const result =
        await closuresApi.create(
          {
            tanqueId: preview.tanqueId,
            fecha: preview.fecha,
            stockFisicoFinal: stock,
            motivoDiferencia:
              hasDifference
                ? differenceReason.trim()
                : undefined,
            observaciones:
              observations.trim() ||
              undefined,
          },
          session,
          { refreshSession },
        )

      setCreatedClosure(result)
    } catch (submitError) {
      setError(
        getErrorMessage(submitError),
      )
    } finally {
      setSubmitting(false)
    }
  }

  if (createdClosure) {
    return (
      <main className="closure-page">
        <section className="closure-success">
          <div
            className="success-icon"
            aria-hidden="true"
          >
            ?
          </div>

          <p className="success-label">
            Cierre registrado
          </p>

          <h1>Cierre diario enviado</h1>

          <p>
            El cierre qued� pendiente de revisi�n por
            un supervisor.
          </p>

          <div className="success-summary">
            <div>
              <span>Tanque</span>
              <strong>
                {createdClosure.tanqueCodigo ??
                  createdClosure.tanqueNombre ??
                  'N/D'}
              </strong>
            </div>

            <div>
              <span>Fecha</span>
              <strong>
                {createdClosure.fechaCierre}
              </strong>
            </div>

            <div>
              <span>Existencia te�rica</span>
              <strong>
                {gallons(
                  createdClosure.stockTeoricoFinal,
                )}
              </strong>
            </div>

            <div>
              <span>Existencia f�sica</span>
              <strong>
                {gallons(
                  createdClosure.stockFisicoFinal,
                )}
              </strong>
            </div>

            <div>
              <span>Diferencia</span>
              <strong>
                {gallons(
                  createdClosure.diferencia,
                )}
              </strong>
            </div>

            <div>
              <span>Estado</span>
              <strong>
                {createdClosure.estado.replaceAll(
                  '_',
                  ' ',
                )}
              </strong>
            </div>
          </div>

          <button
            type="button"
            className="btn-primary"
            onClick={() => navigate('/')}
          >
            Volver al inicio
          </button>
        </section>
      </main>
    )
  }

  return (
    <main className="closure-page">
      <header className="closure-header">
        <button
          type="button"
          className="button-secondary"
          onClick={() => navigate('/')}
        >
          ? Volver
        </button>

        <div>
          <h1>Cierre diario</h1>
          <p>
            Registra la medici�n f�sica final de cada
            tanque al finalizar la jornada.
          </p>
        </div>
      </header>

      <section className="closure-card">
        <div className="closure-card-heading">
          <div>
            <span className="ticket-label">
              Paso 1
            </span>
            <h2>Seleccionar tanque y fecha</h2>
          </div>
        </div>

        <div className="closure-fields">
          <div className="form-group">
            <label htmlFor="closure-tank">
              Tanque
            </label>

            <select
              id="closure-tank"
              value={tankId}
              disabled={loadingTanks}
              onChange={(event) =>
                setTankId(event.target.value)
              }
            >
              <option value="">
                {loadingTanks
                  ? 'Consultando tanques...'
                  : 'Selecciona un tanque'}
              </option>

              {tanks.map((tank) => (
                <option
                  key={tank.id}
                  value={tank.id}
                >
                  {tank.codigo} � {tank.nombre} �{' '}
                  {tank.combustibleNombre}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="closure-date">
              Fecha de cierre
            </label>

            <input
              id="closure-date"
              type="date"
              value={date}
              max={getToday()}
              onChange={(event) =>
                setDate(event.target.value)
              }
            />
          </div>
        </div>

        {selectedTank && (
          <div className="tank-summary">
            <span>
              {selectedTank.combustibleNombre}
            </span>
            <span>
              Stock actual:{' '}
              <strong>
                {gallons(
                  selectedTank.stockActual,
                )}
              </strong>
            </span>
            <span>
              Capacidad:{' '}
              <strong>
                {gallons(
                  selectedTank.capacidadMaxima,
                )}
              </strong>
            </span>
          </div>
        )}

        <button
          type="button"
          className="btn-primary"
          disabled={
            !tankId ||
            !date ||
            loadingPreview
          }
          onClick={() => void handlePreview()}
        >
          {loadingPreview
            ? 'Calculando...'
            : 'Calcular cierre'}
        </button>
      </section>

      {error && (
        <div
          className="form-error"
          role="alert"
        >
          {error}
        </div>
      )}

      {preview && (
        <>
          <section className="closure-card">
            <div className="closure-card-heading">
              <div>
                <span className="ticket-label">
                  Paso 2
                </span>
                <h2>Resumen de movimientos</h2>
              </div>
            </div>

            <div className="closure-movements">
              <div>
                <span>Existencia inicial</span>
                <strong>
                  {gallons(
                    preview.stockInicial,
                  )}
                </strong>
              </div>

              <div>
                <span>Recepciones</span>
                <strong>
                  +{' '}
                  {gallons(
                    preview.totalRecepciones,
                  )}
                </strong>
              </div>

              <div>
                <span>
                  Transferencias de entrada
                </span>
                <strong>
                  +{' '}
                  {gallons(
                    preview.totalTransferenciasEntrada,
                  )}
                </strong>
              </div>

              <div>
                <span>
                  Transferencias de salida
                </span>
                <strong>
                  -{' '}
                  {gallons(
                    preview.totalTransferenciasSalida,
                  )}
                </strong>
              </div>

              <div>
                <span>Despachos</span>
                <strong>
                  -{' '}
                  {gallons(
                    preview.totalDespachos,
                  )}
                </strong>
              </div>

              <div>
                <span>Ajustes positivos</span>
                <strong>
                  +{' '}
                  {gallons(
                    preview.totalAjustesPositivos,
                  )}
                </strong>
              </div>

              <div>
                <span>Ajustes negativos</span>
                <strong>
                  -{' '}
                  {gallons(
                    preview.totalAjustesNegativos,
                  )}
                </strong>
              </div>
            </div>

            <div className="closure-theoretical">
              <span>Existencia te�rica final</span>
              <strong>
                {gallons(
                  preview.stockTeoricoFinal,
                )}
              </strong>
            </div>
          </section>

          <form
            className="closure-card closure-form"
            onSubmit={handleSubmit}
          >
            <div className="closure-card-heading">
              <div>
                <span className="ticket-label">
                  Paso 3
                </span>
                <h2>Medici�n f�sica</h2>
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="physical-stock">
                Existencia f�sica final (galones)
              </label>

              <input
                id="physical-stock"
                type="number"
                min="0"
                step="0.01"
                inputMode="decimal"
                required
                value={physicalStock}
                onChange={(event) =>
                  setPhysicalStock(
                    event.target.value,
                  )
                }
                placeholder="Ej. 425.50"
              />
            </div>

            {difference !== null && (
              <div
                className={
                  hasDifference
                    ? 'closure-difference has-difference'
                    : 'closure-difference'
                }
              >
                <span>Diferencia</span>
                <strong>
                  {difference > 0 ? '+' : ''}
                  {gallons(difference)}
                </strong>
              </div>
            )}

            {hasDifference && (
              <div className="form-group">
                <label htmlFor="difference-reason">
                  Motivo de la diferencia *
                </label>

                <textarea
                  id="difference-reason"
                  rows={3}
                  required
                  value={differenceReason}
                  onChange={(event) =>
                    setDifferenceReason(
                      event.target.value,
                    )
                  }
                  placeholder="Describe la causa observada de la diferencia."
                />
              </div>
            )}

            <div className="form-group">
              <label htmlFor="closure-observations">
                Observaciones
              </label>

              <textarea
                id="closure-observations"
                rows={3}
                value={observations}
                onChange={(event) =>
                  setObservations(
                    event.target.value,
                  )
                }
                placeholder="Informaci�n adicional del cierre (opcional)."
              />
            </div>

            <div className="closure-warning">
              <strong>
                Verifica la medici�n antes de continuar.
              </strong>
              <span>
                Una vez registrado, el cierre quedar�
                pendiente de aprobaci�n del supervisor.
              </span>
            </div>

            <button
              type="submit"
              className="btn-primary"
              disabled={
                submitting ||
                physicalStock.trim() === ''
              }
            >
              {submitting
                ? 'Registrando cierre...'
                : 'Registrar cierre diario'}
            </button>
          </form>
        </>
      )}
    </main>
  )
}
