import { ApiResponse, ApiErrorDetail } from '../types';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://localhost:7001/api/v1';

export class ApiError extends Error {
  status: number;
  errors: ApiErrorDetail[];

  constructor(message: string, status: number = 400, errors: ApiErrorDetail[] = []) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.errors = errors;
  }
}

export function isMockEnabled(): boolean {
  const stored = localStorage.getItem('fms_use_mock_api');
  if (stored !== null) {
    return stored === 'true';
  }
  return import.meta.env.VITE_USE_MOCK !== 'false';
}

export function setMockEnabled(enabled: boolean): void {
  localStorage.setItem('fms_use_mock_api', enabled ? 'true' : 'false');
}

export async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  const token = localStorage.getItem('fms_access_token');

  const headers = new Headers(options.headers || {});
  headers.set('Content-Type', 'application/json');
  headers.set('Accept', 'application/json');

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const url = `${API_BASE_URL}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;

  try {
    const response = await fetch(url, {
      ...options,
      headers,
    });

    // Sesión expirada o no autenticado
    if (response.status === 401) {
      localStorage.removeItem('fms_access_token');
      localStorage.removeItem('fms_auth_user');
      window.dispatchEvent(new CustomEvent('fms:auth-unauthorized'));
      throw new ApiError('Sesión expirada. Por favor inicie sesión nuevamente.', 401, [
        { code: 'UNAUTHENTICATED', detail: 'Token expirado o no provisto' },
      ]);
    }

    if (response.status === 403) {
      throw new ApiError('Acceso denegado. No posee permisos para realizar esta operación.', 403, [
        { code: 'FORBIDDEN', detail: 'Permisos insuficientes para el recurso' },
      ]);
    }

    const json = (await response.json()) as ApiResponse<T>;

    if (!response.ok || !json.success) {
      const errorMsg = json.message || 'Error al procesar la solicitud';
      throw new ApiError(errorMsg, response.status, json.errors || []);
    }

    return json;
  } catch (error) {
    if (error instanceof ApiError) {
      throw error;
    }
    const err = error as Error;
    throw new ApiError(err.message || 'Error de conexión con el servidor', 500, [
      { code: 'NETWORK_OR_SERVER_ERROR', detail: err.message },
    ]);
  }
}
