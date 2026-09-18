export interface CreateDispatchRequest {
  ticketId: string
  tanqueId: string
  galones: number
  odometro: number
  observacion: string
}

export interface DispatchResult {
  despachoId: string
  ticketId: string
  numeroTicket: string
  galonesDespachados: number
  saldoResultanteTanque: number
  fechaHora: string
  estado: string
}

export interface DispatchApiResponse {
  data: DispatchResult
  meta: {
    traceId: string
  }
}