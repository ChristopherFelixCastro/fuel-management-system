import { ApiErrorDetail, ApiResponse } from '../types';

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || 'http://localhost:5080';

const ACCESS_TOKEN_KEY = 'fms_access_token';
const REFRESH_TOKEN_KEY = 'fms_refresh_token';
const ACCESS_EXPIRATION_KEY = 'fms_access_token_expires_at';
const REFRESH_EXPIRATION_KEY = 'fms_refresh_token_expires_at';

interface CoreSuccessResponse<T> {
  data: T;
  meta: {
    traceId: string;
  };
}

interface CoreErrorResponse {
  error?: {
    code?: string;
    message?: string;
    details?: string[];
  };
  traceId?: string;
}

interface RefreshResponseData {
  accessToken: string;
  accessTokenExpiraEn: string;
  refreshToken: string;
  refreshTokenExpiraEn: string;
}

export class ApiError extends Error {
  status: number;
  errors: ApiErrorDetail[];

  constructor(
    message: string,
    status: number = 400,
    errors: ApiErrorDetail[] = [],
  ) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.errors = errors;
  }
}

export function isMockEnabled(): boolean {
  return localStorage.getItem('fms_use_mock_api') === 'true';
}

export function setMockEnabled(enabled: boolean): void {
  localStorage.setItem(
    'fms_use_mock_api',
    enabled ? 'true' : 'false',
  );
}

export function getAccessToken(): string | null {
  return localStorage.getItem(ACCESS_TOKEN_KEY);
}

export function getRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_TOKEN_KEY);
}

export function saveTokens(data: {
  accessToken: string;
  accessTokenExpiraEn: string;
  refreshToken: string;
  refreshTokenExpiraEn: string;
}): void {
  localStorage.setItem(ACCESS_TOKEN_KEY, data.accessToken);
  localStorage.setItem(REFRESH_TOKEN_KEY, data.refreshToken);
  localStorage.setItem(
    ACCESS_EXPIRATION_KEY,
    data.accessTokenExpiraEn,
  );
  localStorage.setItem(
    REFRESH_EXPIRATION_KEY,
    data.refreshTokenExpiraEn,
  );
}

export function clearTokens(): void {
  localStorage.removeItem(ACCESS_TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
  localStorage.removeItem(ACCESS_EXPIRATION_KEY);
  localStorage.removeItem(REFRESH_EXPIRATION_KEY);
}

let refreshPromise: Promise<boolean> | null = null;

async function readCoreError(
  response: Response,
  fallback: string,
): Promise<ApiError> {
  try {
    const body = (await response.json()) as CoreErrorResponse;

    const details: ApiErrorDetail[] =
      body.error?.details?.map(detail => ({
        code: body.error?.code || 'API_ERROR',
        detail,
      })) ?? [];

    return new ApiError(
      body.error?.message || fallback,
      response.status,
      details,
    );
  } catch {
    return new ApiError(fallback, response.status);
  }
}

async function refreshAccessToken(): Promise<boolean> {
  if (refreshPromise) {
    return refreshPromise;
  }

  refreshPromise = (async () => {
    const refreshToken = getRefreshToken();
    const refreshExpiration = localStorage.getItem(
      REFRESH_EXPIRATION_KEY,
    );

    if (!refreshToken || !refreshExpiration) {
      return false;
    }

    const expiration = new Date(refreshExpiration).getTime();

    if (
      !Number.isFinite(expiration) ||
      expiration <= Date.now()
    ) {
      return false;
    }

    try {
      const response = await fetch(
        `${API_BASE_URL}/auth/refresh`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Accept: 'application/json',
          },
          body: JSON.stringify({ refreshToken }),
        },
      );

      if (!response.ok) {
        return false;
      }

      const result =
        (await response.json()) as CoreSuccessResponse<RefreshResponseData>;

      saveTokens(result.data);
      return true;
    } catch {
      return false;
    }
  })();

  try {
    return await refreshPromise;
  } finally {
    refreshPromise = null;
  }
}

function buildHeaders(
  options: RequestInit,
  token: string | null,
): Headers {
  const headers = new Headers(options.headers || {});

  headers.set('Accept', 'application/json');

  if (
    options.body !== undefined &&
    !(options.body instanceof FormData)
  ) {
    headers.set('Content-Type', 'application/json');
  }

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  return headers;
}

export async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {},
): Promise<ApiResponse<T>> {
  const url =
    `${API_BASE_URL}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;

  const execute = (token: string | null) =>
    fetch(url, {
      ...options,
      headers: buildHeaders(options, token),
    });

  let response: Response;

  try {
    response = await execute(getAccessToken());
  } catch (error) {
    const err = error as Error;
    throw new ApiError(
      err.message || 'No fue posible conectar con el servidor.',
      0,
      [
        {
          code: 'NETWORK_ERROR',
          detail: err.message || 'Error de red',
        },
      ],
    );
  }

  if (response.status === 401 && getRefreshToken()) {
    const refreshed = await refreshAccessToken();

    if (refreshed) {
      response = await execute(getAccessToken());
    }
  }

  if (response.status === 401) {
    clearTokens();
    localStorage.removeItem('fms_auth_user');

    window.dispatchEvent(
      new CustomEvent('fms:auth-unauthorized'),
    );

    throw await readCoreError(
      response,
      'La sesión expiró. Inicie sesión nuevamente.',
    );
  }

  if (!response.ok) {
    throw await readCoreError(
      response,
      response.status === 403
        ? 'No posee permisos para realizar esta operación.'
        : 'Error al procesar la solicitud.',
    );
  }

  if (response.status === 204) {
    return {
      success: true,
      message: '',
      data: null as T,
      errors: [],
    };
  }

  const result =
    (await response.json()) as CoreSuccessResponse<T>;

  /*
   * Conservamos temporalmente ApiResponse<T> de Angel para
   * no tener que reescribir todas sus páginas simultáneamente.
   * La traducción del envelope ocurre únicamente aquí.
   */
  return {
    success: true,
    message: '',
    data: result.data,
    errors: [],
  };
}

export interface ApiFileResponse {
  blob: Blob;
  fileName: string;
  contentType: string;
}

function getFileNameFromDisposition(
  contentDisposition: string | null,
): string | null {
  if (!contentDisposition) {
    return null;
  }

  const utf8Match = contentDisposition.match(
    /filename\*=UTF-8''([^;]+)/i,
  );

  if (utf8Match?.[1]) {
    try {
      return decodeURIComponent(utf8Match[1]);
    } catch {
      return utf8Match[1];
    }
  }

  const regularMatch = contentDisposition.match(
    /filename="?([^";]+)"?/i,
  );

  return regularMatch?.[1] ?? null;
}

export async function apiDownload(
  endpoint: string,
): Promise<ApiFileResponse> {
  const url =
    `${API_BASE_URL}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;

  const execute = (token: string | null) =>
    fetch(url, {
      method: 'GET',
      headers: buildHeaders({}, token),
    });

  let response: Response;

  try {
    response = await execute(getAccessToken());
  } catch (error) {
    const err = error as Error;

    throw new ApiError(
      err.message || 'No fue posible conectar con el servidor.',
      0,
      [
        {
          code: 'NETWORK_ERROR',
          detail: err.message || 'Error de red',
        },
      ],
    );
  }

  if (response.status === 401 && getRefreshToken()) {
    const refreshed = await refreshAccessToken();

    if (refreshed) {
      response = await execute(getAccessToken());
    }
  }

  if (response.status === 401) {
    clearTokens();
    localStorage.removeItem('fms_auth_user');

    window.dispatchEvent(
      new CustomEvent('fms:auth-unauthorized'),
    );

    throw await readCoreError(
      response,
      'La sesión expiró. Inicie sesión nuevamente.',
    );
  }

  if (!response.ok) {
    throw await readCoreError(
      response,
      response.status === 403
        ? 'No posee permisos para realizar esta operación.'
        : 'Error al exportar el reporte.',
    );
  }

  const blob = await response.blob();

  const contentDisposition = response.headers.get(
    'Content-Disposition',
  );

  const fileName =
    getFileNameFromDisposition(contentDisposition) ??
    'reporte';

  return {
    blob,
    fileName,
    contentType:
      response.headers.get('Content-Type') ??
      'application/octet-stream',
  };
}

export function saveDownloadedFile(
  file: ApiFileResponse,
): void {
  const objectUrl = URL.createObjectURL(file.blob);

  const anchor = document.createElement('a');
  anchor.href = objectUrl;
  anchor.download = file.fileName;

  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();

  URL.revokeObjectURL(objectUrl);
}