import type {
  AuthSession,
  LoginApiResponse,
  LoginCredentials,
} from '../types/auth'

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5080'

interface ApiErrorResponse {
  error?: {
    code?: string
    message?: string
    details?: unknown[]
  }
}

export interface AuthApi {
  login(credentials: LoginCredentials): Promise<AuthSession>
  logout(session: AuthSession): Promise<void>
}

export const authApi: AuthApi = {
  async login(credentials) {
    const response = await fetch(`${API_URL}/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(credentials),
    })

    if (!response.ok) {
      let message = 'No fue posible iniciar sesión.'

      try {
        const error = (await response.json()) as ApiErrorResponse

        if (error.error?.message) {
          message = error.error.message
        }
      } catch {
        // Conservamos el mensaje genérico.
      }

      throw new Error(message)
    }

    const result = (await response.json()) as LoginApiResponse

    if (result.data.rol !== 'DESPACHADOR') {
      throw new Error(
        'Esta aplicación está disponible únicamente para despachadores.',
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
      accessTokenExpiresAt: result.data.accessTokenExpiraEn,
      refreshToken: result.data.refreshToken,
      refreshTokenExpiresAt: result.data.refreshTokenExpiraEn,
    }
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
      // Aunque el backend no esté disponible, la sesión local se eliminará.
    }
  },
}