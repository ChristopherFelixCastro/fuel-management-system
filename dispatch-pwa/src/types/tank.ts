export interface Tank {
  id: string
  estacionId: string
  estacionNombre: string
  tipoCombustibleId: number
  combustibleNombre: string
  codigo: string
  nombre: string
  capacidadMaxima: number
  stockActual: number
  nivelCritico: number
  activo: boolean
}

export interface TanksApiResponse {
  data: Tank[]
  meta: {
    traceId: string
  }
}