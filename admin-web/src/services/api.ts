import {
  ApiResponse,
  PagedResult,
  ClosureDto,
  ClosurePreviewDto,
  CreateClosureRequest,
  AlertDto,
  VwConsumoDiario,
  VwTanqueResumen,
  VwMovimientosTanque,
} from '../types';

 const API_BASE = 'http://localhost:5132/api/v1';

async function fetchJson<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      'X-Dev-UserId': 'aea377dc-e365-4779-bf82-5f74b334f09b',
      'X-Dev-UserName': 'admin.dev',
      ...options?.headers,
    },
  });

  const json = await res.json();
  if (!res.ok || !json.success) {
    throw new Error(json.errors?.[0] || json.message || 'Error en la petición API');
  }

  return json.data as T;
}

// Cierres
export const getClosures = (page = 1, pageSize = 20, estado?: string) =>
  fetchJson<PagedResult<ClosureDto>>(`${API_BASE}/closures?page=${page}&pageSize=${pageSize}${estado ? `&estado=${estado}` : ''}`);

export const getClosureById = (id: string) =>
  fetchJson<ClosureDto>(`${API_BASE}/closures/${id}`);

export const getClosurePreview = (tanqueId: string, fecha: string) =>
  fetchJson<ClosurePreviewDto>(`${API_BASE}/closures/preview?tanqueId=${tanqueId}&fecha=${fecha}`);

export const createClosure = (req: CreateClosureRequest) =>
  fetchJson<string>(`${API_BASE}/closures`, {
    method: 'POST',
    body: JSON.stringify(req),
  });

export const approveClosure = (id: string) =>
  fetchJson<string>(`${API_BASE}/closures/${id}/approve`, { method: 'POST' });

export const rejectClosure = (id: string, motivo: string) =>
  fetchJson<string>(`${API_BASE}/closures/${id}/reject`, {
    method: 'POST',
    body: JSON.stringify({ motivoRechazo: motivo }),
  });

export const getClosurePdfUrl = (id: string) => `${API_BASE}/closures/${id}/pdf`;

// Alertas
export const getAlerts = (page = 1, pageSize = 20, estado?: string) =>
  fetchJson<PagedResult<AlertDto>>(`${API_BASE}/alerts?page=${page}&pageSize=${pageSize}${estado ? `&estado=${estado}` : ''}`);

export const acknowledgeAlert = (id: string, nota?: string) =>
  fetchJson<AlertDto>(`${API_BASE}/alerts/${id}/acknowledge`, {
    method: 'PATCH',
    body: JSON.stringify({ nota }),
  });

// Reportes
export const getConsumptionReport = (page = 1, pageSize = 50) =>
  fetchJson<PagedResult<VwConsumoDiario>>(`${API_BASE}/reports/consumption?page=${page}&pageSize=${pageSize}`);

export const getInventoryReport = (page = 1, pageSize = 50) =>
  fetchJson<PagedResult<VwTanqueResumen>>(`${API_BASE}/reports/inventory?page=${page}&pageSize=${pageSize}`);

export const getTraceabilityReport = (page = 1, pageSize = 50) =>
  fetchJson<PagedResult<VwMovimientosTanque>>(`${API_BASE}/reports/traceability?page=${page}&pageSize=${pageSize}`);

export const getExportUrl = (reportType: string, format: 'pdf' | 'xlsx' | 'csv') =>
  `${API_BASE}/exports/${reportType}?format=${format}`;
