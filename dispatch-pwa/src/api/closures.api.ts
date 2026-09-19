import { authenticatedFetch } from './authenticated-fetch'

import type { AuthSession } from '../types/auth'
import type {
  ApiDataResponse,
  Closure,
  ClosurePreview,
  CreateDailyClosureRequest,
} from '../types/closure'
import {
  ApiError,
  type ApiErrorResponse,
} from '../types/ticket'

const API_URL =
  import.meta.env.VITE_API_URL ?? 'http://localhost:5080'

interface AuthActions {
  refreshSession: () => Promise<AuthSession | null>
}

async function throwApiError(
  response: Response,
  fallback: string,
): Promise<never> {
  let code = 'CLOSURE_ERROR'
  let message = fallback

  try {
    const result =
      (await response.json()) as ApiErrorResponse

    code = result.error?.code ?? code
    message = result.error?.message ?? message
  } catch {
    // Conservamos el mensaje gen�rico.
  }

  throw new ApiError(
    message,
    code,
    response.status,
  )
}

export const closuresApi = {
  async preview(
    tankId: string,
    date: string,
    session: AuthSession,
    auth: AuthActions,
  ): Promise<ClosurePreview> {
    const url = new URL(
      `${API_URL}/closures/daily/preview`,
    )

    url.searchParams.set('tanqueId', tankId)
    url.searchParams.set('fecha', date)

    const response = await authenticatedFetch({
      session,
      refreshSession: auth.refreshSession,
      input: url,
    })

    if (!response.ok) {
      return throwApiError(
        response,
        'No fue posible calcular el cierre diario.',
      )
    }

    const result =
      (await response.json()) as ApiDataResponse<ClosurePreview>

    return result.data
  },

  async create(
    request: CreateDailyClosureRequest,
    session: AuthSession,
    auth: AuthActions,
  ): Promise<Closure> {
    const response = await authenticatedFetch({
      session,
      refreshSession: auth.refreshSession,
      input: `${API_URL}/closures/daily`,
      init: {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      },
    })

    if (!response.ok) {
      return throwApiError(
        response,
        'No fue posible registrar el cierre diario.',
      )
    }

    const result =
      (await response.json()) as ApiDataResponse<Closure>

    return result.data
  },
}
