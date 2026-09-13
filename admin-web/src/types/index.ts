export interface ClosureDto {
  id: string;
  tanqueId: string;
  tanqueCodigo: string;
  tanqueNombre?: string;
  estacionId: string;
  estacionCodigo: string;
  estacionNombre: string;
  combustibleNombre: string;
  fechaCierre: string;
  estado: string;
  stockInicial: number;
  totalRecepciones: number;
  totalTransferenciasEntrada: number;
  totalTransferenciasSalida: number;
  totalDespachos: number;
  totalAjustesPositivos: number;
  totalAjustesNegativos: number;
  stockTeoricoFinal: number;
  stockFisicoFinal: number;
  diferencia: number;
  creadoPor: string;
  revisadoPor?: string;
  fechaCreacion: string;
  fechaRevision?: string;
  motivoDiferencia?: string;
  motivoRechazo?: string;
  observaciones?: string;
}

export interface ClosurePreviewDto {
  tanqueId: string;
  fecha: string;
  stockInicial: number;
  totalRecepciones: number;
  totalTransferenciasEntrada: number;
  totalTransferenciasSalida: number;
  totalDespachos: number;
  totalAjustesPositivos: number;
  totalAjustesNegativos: number;
  stockTeoricoFinal: number;
}

export interface CreateClosureRequest {
  tanqueId: string;
  fechaCierre: string;
  stockFisicoFinal: number;
  motivoDiferencia?: string;
  observaciones?: string;
}

export interface AlertDto {
  id: string;
  tipo: string;
  severidad: string;
  modulo: string;
  mensaje: string;
  entidadId?: string;
  estado: string;
  fechaGeneracion: string;
  fechaReconocimiento?: string;
  reconocidoPor?: string;
}

export interface VwConsumoDiario {
  fecha: string;
  tanqueId: string;
  tanqueNombre?: string;
  combustibleTipo?: string;
  totalDespachadoLitros?: number;
  cantidadDespachos: number;
}

export interface VwTanqueResumen {
  id: string;
  tanqueNombre: string;
  codigo: string;
  combustibleTipo: string;
  capacidadTotal: number;
  stockActual: number;
  porcentajeOcupacion: number;
  estadoStock: string;
}

export interface VwMovimientosTanque {
  movimientoId: string;
  tanqueId: string;
  tanqueCodigo: string;
  tanqueNombre?: string;
  estacionId: string;
  estacionCodigo: string;
  estacionNombre: string;
  tipoCombustibleId: number;
  combustibleCodigo: string;
  combustibleNombre: string;
  tipoMovimiento: string;
  cantidad: number;
  saldoAnterior: number;
  saldoPosterior: number;
  registradoPorUsuarioId: string;
  registradoPor: string;
  recepcionId?: string;
  despachoId?: string;
  transferenciaId?: string;
  ajusteId?: string;
  fechaMovimiento: string;
  observaciones?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}
