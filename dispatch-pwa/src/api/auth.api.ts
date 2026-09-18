import type {
  AuthApiResponse,
  AuthSession,
  LoginCredentials,
} from '../types/auth'

const API_URL =
  import.meta.env.VITE_API_URL ?? 'http://localhost:5080'

interface ApiErrorResponse {
  error?: {
    code?: string
    message?: string
    details?: unknown[]
  }
}

export interface AuthApi {
  login(
    credentials: LoginCredentials,
  ): Promise<AuthSession>

  refresh(
    session: AuthSession,
  ): Promise<AuthSession>

  logout(
    session: AuthSession,
  ): Promise<void>
}

function buildSession(
  result: AuthApiResponse,
): AuthSession {
  if (result.data.rol !== 'DESPACHADOR') {
    throw new Error(
      'Esta aplicación está disponible únicamente para despachadores.',
    )
  }

  if (!result.data.estacionId) {
    throw new Error(
      'El despachador no tiene una estación asignada.',
    )
  }

  return {
    user: {
      id: result.data.usuarioId,
      name: result.data.usuario,
      role: 'DESPACHADOR',
      stationId: result.data.estacionId,
    },
    accessToken: result.data.accessToken,
    accessTokenExpiresAt:
      result.data.accessTokenExpiraEn,
    refreshToken: result.data.refreshToken,
    refreshTokenExpiresAt:
      result.data.refreshTokenExpiraEn,
  }
}

async function readError(
  response: Response,
  fallback: string,
): Promise<string> {
  try {
    const result =
      (await response.json()) as ApiErrorResponse

    return result.error?.message ?? fallback
  } catch {
    return fallback
  }
}

export const authApi: AuthApi = {
  async login(credentials) {
    const response = await fetch(
      `${API_URL}/auth/login`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(credentials),
      },
    )

    if (!response.ok) {
      throw new Error(
        await readError(
          response,
          'No fue posible iniciar sesión.',
        ),
      )
    }

    const result =
      (await response.json()) as AuthApiResponse

    return buildSession(result)
  },

  async refresh(session) {
    const response = await fetch(
      `${API_URL}/auth/refresh`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          refreshToken: session.refreshToken,
        }),
      },
    )

    if (!response.ok) {
      throw new Error(
        await readError(
          response,
          'La sesión expiró. Inicie sesión nuevamente.',
        ),
      )
    }

    const result =
      (await response.json()) as AuthApiResponse

    return buildSession(result)
  },

  async logout(session) {
    try {
      await fetch(`${API_URL}/auth/logout`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${session.accessToken}`,
        },
        body: JSON.stringify({
          refreshToken: session.refreshToken,
        }),
      })
    } catch {
      // La sesión local se elimina incluso si
      // el servidor no está disponible.
    }
  },
}