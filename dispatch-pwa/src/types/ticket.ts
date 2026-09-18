export interface Ticket {
  ticketId: string
  numeroTicket: string
  ticket: string
  numero: string

  empleadoId: string
  empleado: string
  codigoEmpleado: string

  vehiculoId: string
  vehiculo: string
  placa: string
  ficha: string

  tipoCombustibleId: number
  tipoCombustible: string
  combustibleCodigo: string

  cantidadAutorizada: number
  cantidadDisponible: number

  fechaExpiracion: string
  vencimiento: string
  venceEn: string

  estado: string
  estadoEfectivo: string

  estacionId: string
  estacionNombre: string

  departamentoId: string
  departamentoNombre: string
}

export interface TicketValidationResponse {
  data: Ticket
  meta: {
    traceId: string
  }
}

export interface ApiErrorResponse {
  error?: {
    code?: string
    message?: string
    details?: unknown[]
  }
  traceId?: string
}

export class ApiError extends Error {
  readonly code: string
  readonly status: number

  constructor(
    message: string,
    code = 'UNKNOWN_ERROR',
    status = 500,
  ) {
    super(message)
    this.name = 'ApiError'
    this.code = code
    this.status = status
  }
}