export interface ClosurePreview {
  tanqueId: string
  fecha: string
  stockInicial: number
  totalRecepciones: number
  totalTransferenciasEntrada: number
  totalTransferenciasSalida: number
  totalDespachos: number
  totalAjustesPositivos: number
  totalAjustesNegativos: number
  stockTeoricoFinal: number
}

export interface Closure {
  id: string
  tanqueId: string
  tanqueCodigo?: string | null
  tanqueNombre?: string | null
  estacionId?: string | null
  estacionNombre?: string | null
  fechaCierre: string
  stockInicial: number
  totalRecepciones: number
  totalTransferenciasEntrada: number
  totalTransferenciasSalida: number
  totalDespachos: number
  totalAjustesPositivos: number
  totalAjustesNegativos: number
  stockTeoricoFinal: number
  stockFisicoFinal: number
  diferencia: number
  estado: string
  motivoDiferencia?: string | null
  motivoRechazo?: string | null
  observaciones?: string | null
}

export interface CreateDailyClosureRequest {
  tanqueId: string
  fecha: string
  stockFisicoFinal: number
  motivoDiferencia?: string
  observaciones?: string
}

export interface ApiDataResponse<T> {
  data: T
  meta: {
    traceId: string
  }
}
