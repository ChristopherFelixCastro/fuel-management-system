import {
  useEffect,
  useState,
  type FormEvent,
} from 'react'
import {
  Navigate,
  useLocation,
  useNavigate,
} from 'react-router-dom'

import { dispatchApi } from '../api/dispatch.api'
import { tanksApi } from '../api/tanks.api'
import { useAuth } from '../contexts/AuthContext'
import type { DispatchResult } from '../types/dispatch'
import type { Tank } from '../types/tank'
import {
  ApiError,
  type Ticket,
} from '../types/ticket'

interface TicketLocationState {
  ticket?: Ticket
}

function getDispatchError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return 'No fue posible completar el despacho.'
  }

  switch (error.code) {
    case 'TICKET_CONSUMED':
      return 'Este ticket ya fue utilizado.'

    case 'TICKET_EXPIRED':
      return 'El ticket venció antes de completar el despacho.'

    case 'TICKET_ANULADO':
      return 'El ticket fue anulado.'

    case 'FORBIDDEN':
      return 'No tiene autorización para realizar este despacho.'

    default:
      return error.message
  }
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('es-DO', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

export function TicketPage() {
  const location = useLocation()
  const navigate = useNavigate()

  const {
    session,
    refreshSession,
  } = useAuth()

  const state =
    location.state as TicketLocationState | null

  const ticket = state?.ticket

  const [tanks, setTanks] = useState<Tank[]>([])
  const [selectedTankId, setSelectedTankId] =
    useState('')

  const [odometer, setOdometer] = useState('')
  const [observation, setObservation] =
    useState('')

  const [isLoadingTanks, setIsLoadingTanks] =
    useState(true)

  const [isDispatching, setIsDispatching] =
    useState(false)

  const [error, setError] =
    useState<string | null>(null)

  const [result, setResult] =
    useState<DispatchResult | null>(null)

  useEffect(() => {
    if (!ticket || !session) {
      return
    }

    let cancelled = false

    async function loadTanks() {
      if (!ticket || !session) {
        return
      }

      try {
        setIsLoadingTanks(true)
        setError(null)

        const compatibleTanks =
          await tanksApi.getCompatibleTanks(
            ticket.estacionId,
            ticket.tipoCombustibleId,
            session,
            {
              refreshSession,
            },
          )

        if (cancelled) {
          return
        }

        setTanks(compatibleTanks)

        if (compatibleTanks.length === 1) {
          setSelectedTankId(
            compatibleTanks[0].id,
          )
        } else {
          setSelectedTankId('')
        }
      } catch (loadError) {
        if (!cancelled) {
          setError(
            loadError instanceof Error
              ? loadError.message
              : 'No fue posible consultar los tanques.',
          )
        }
      } finally {
        if (!cancelled) {
          setIsLoadingTanks(false)
        }
      }
    }

    void loadTanks()

    return () => {
      cancelled = true
    }
  }, [ticket, session, refreshSession])

  if (!ticket) {
    return <Navigate to="/scan" replace />
  }

  if (!session) {
    return <Navigate to="/login" replace />
  }

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault()

    if (!session) {
      navigate('/login', {
        replace: true,
      })
      return
    }

    if (!ticket) {
      navigate('/scan', {
        replace: true,
      })
      return
    }

    if (!selectedTankId) {
      setError(
        'Seleccione el tanque que realizará el despacho.',
      )
      return
    }

    const parsedOdometer = Number(odometer)

    if (
      !Number.isFinite(parsedOdometer) ||
      parsedOdometer < 0
    ) {
      setError('Ingrese un odómetro válido.')
      return
    }

    setError(null)
    setIsDispatching(true)

    try {
      const dispatchResult =
        await dispatchApi.create(
          {
            ticketId: ticket.ticketId,
            tanqueId: selectedTankId,
            galones:
              ticket.cantidadAutorizada,
            odometro: parsedOdometer,
            observacion: observation.trim(),
          },
          session,
          {
            refreshSession,
          },
        )

      setResult(dispatchResult)
    } catch (dispatchError) {
      setError(
        getDispatchError(dispatchError),
      )
    } finally {
      setIsDispatching(false)
    }
  }

  if (result) {
    return (
      <main className="ticket-page">
        <section className="dispatch-success">
          <div
            className="success-icon"
            aria-hidden="true"
          >
            ✓
          </div>

          <p className="success-label">
            Operación completada
          </p>

          <h1>Despacho completado</h1>

          <p>
            El combustible fue registrado
            correctamente.
          </p>

          <div className="success-summary">
            <div>
              <span>Ticket</span>
              <strong>
                {result.numeroTicket}
              </strong>
            </div>

            <div>
              <span>
                Galones despachados
              </span>
              <strong>
                {result.galonesDespachados} gal
              </strong>
            </div>

            <div>
              <span>Saldo del tanque</span>
              <strong>
                {result.saldoResultanteTanque} gal
              </strong>
            </div>

            <div>
              <span>Estado</span>
              <strong>{result.estado}</strong>
            </div>
          </div>

          <button
            type="button"
            className="btn-primary"
            onClick={() =>
              navigate('/scan', {
                replace: true,
              })
            }
          >
            Escanear otro ticket
          </button>

          <button
            type="button"
            className="button-secondary ticket-home-button"
            onClick={() =>
              navigate('/', {
                replace: true,
              })
            }
          >
            Volver al inicio
          </button>
        </section>
      </main>
    )
  }

  return (
    <main className="ticket-page">
      <header className="ticket-page-header">
        <button
          type="button"
          className="button-secondary"
          onClick={() => navigate('/scan')}
        >
          ← Volver
        </button>

        <div>
          <h1>Validación de despacho</h1>
          <p>
            Verifique físicamente los datos
            antes de suministrar combustible.
          </p>
        </div>
      </header>

      <section className="ticket-card">
        <div className="ticket-card-heading">
          <div>
            <span className="ticket-label">
              Ticket autorizado
            </span>

            <h2>{ticket.numeroTicket}</h2>
          </div>

          <span className="ticket-status">
            {ticket.estadoEfectivo.replaceAll(
              '_',
              ' ',
            )}
          </span>
        </div>

        <div className="ticket-quantity">
          <span>Cantidad autorizada</span>
          <strong>
            {ticket.cantidadAutorizada} gal
          </strong>
        </div>

        <div className="ticket-grid">
          <div>
            <span>Empleado</span>
            <strong>{ticket.empleado}</strong>
            <small>
              {ticket.codigoEmpleado}
            </small>
          </div>

          <div>
            <span>Departamento</span>
            <strong>
              {ticket.departamentoNombre}
            </strong>
          </div>

          <div>
            <span>Vehículo</span>
            <strong>{ticket.vehiculo}</strong>
          </div>

          <div>
            <span>Placa</span>
            <strong>{ticket.placa}</strong>
          </div>

          <div>
            <span>Ficha</span>
            <strong>{ticket.ficha}</strong>
          </div>

          <div>
            <span>Combustible</span>
            <strong>
              {ticket.tipoCombustible}
            </strong>
          </div>

          <div>
            <span>Estación</span>
            <strong>
              {ticket.estacionNombre}
            </strong>
          </div>

          <div>
            <span>Vencimiento</span>
            <strong>
              {formatDate(
                ticket.fechaExpiracion,
              )}
            </strong>
          </div>
        </div>
      </section>

      <section className="physical-check">
        <h2>Confirmación física</h2>

        <p>
          Antes de continuar, confirme que el
          empleado, la placa y la ficha coinciden
          con los datos mostrados.
        </p>

        <div className="physical-values">
          <span>
            Placa:{' '}
            <strong>{ticket.placa}</strong>
          </span>

          <span>
            Ficha:{' '}
            <strong>{ticket.ficha}</strong>
          </span>
        </div>
      </section>

      <form
        className="dispatch-form"
        onSubmit={(event) =>
          void handleSubmit(event)
        }
      >
        <div className="dispatch-form-heading">
          <h2>Datos del despacho</h2>
          <p>
            Complete la información
            operacional.
          </p>
        </div>

        <div className="form-group">
          <label htmlFor="tank">
            Tanque de suministro
          </label>

          {isLoadingTanks ? (
            <p className="field-message">
              Consultando tanques
              compatibles...
            </p>
          ) : tanks.length === 0 ? (
            <p className="form-error">
              No existen tanques activos
              compatibles con{' '}
              {ticket.tipoCombustible}.
            </p>
          ) : (
            <select
              id="tank"
              value={selectedTankId}
              onChange={(event) =>
                setSelectedTankId(
                  event.target.value,
                )
              }
              required
            >
              {tanks.length > 1 && (
                <option value="">
                  Seleccione un tanque
                </option>
              )}

              {tanks.map((tank) => (
                <option
                  key={tank.id}
                  value={tank.id}
                >
                  {tank.nombre} ·{' '}
                  {tank.codigo} ·{' '}
                  {tank.stockActual} gal
                </option>
              ))}
            </select>
          )}
        </div>

        <div className="form-group">
          <label htmlFor="odometer">
            Odómetro actual *
          </label>

          <input
            id="odometer"
            type="number"
            inputMode="numeric"
            min="0"
            step="1"
            value={odometer}
            onChange={(event) =>
              setOdometer(
                event.target.value,
              )
            }
            placeholder="Ej. 12503"
            required
          />
        </div>

        <div className="form-group">
          <label htmlFor="observation">
            Observación
          </label>

          <textarea
            id="observation"
            rows={3}
            value={observation}
            onChange={(event) =>
              setObservation(
                event.target.value,
              )
            }
            placeholder="Opcional"
          />
        </div>

        {error && (
          <div
            className="form-error"
            role="alert"
          >
            {error}
          </div>
        )}

        <div className="dispatch-warning">
          <strong>
            Confirme antes de continuar
          </strong>

          <span>
            Se registrarán exactamente{' '}
            {ticket.cantidadAutorizada}{' '}
            galones. Un despacho completado
            no puede modificarse desde esta
            pantalla.
          </span>
        </div>

        <button
          type="submit"
          className="btn-primary"
          disabled={
            isDispatching ||
            isLoadingTanks ||
            tanks.length === 0 ||
            !selectedTankId
          }
        >
          {isDispatching ? (
            <>
              <span className="spinner" />
              Registrando despacho...
            </>
          ) : (
            `Confirmar despacho de ${ticket.cantidadAutorizada} gal`
          )}
        </button>
      </form>
    </main>
  )
}