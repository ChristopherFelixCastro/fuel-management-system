import { authenticatedFetch } from './authenticated-fetch'

import type { AuthSession } from '../types/auth'
import type {
  CreateDispatchRequest,
  DispatchApiResponse,
  DispatchResult,
} from '../types/dispatch'
import {
  ApiError,
  type ApiErrorResponse,
} from '../types/ticket'

const API_URL =
  import.meta.env.VITE_API_URL ?? 'http://localhost:5080'

interface AuthActions {
  refreshSession: () => Promise<AuthSession | null>
}

export const dispatchApi = {
  async create(
    request: CreateDispatchRequest,
    session: AuthSession,
    auth: AuthActions,
  ): Promise<DispatchResult> {
    const response = await authenticatedFetch({
      session,
      refreshSession: auth.refreshSession,
      input: `${API_URL}/dispatches`,
      init: {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      },
    })

    if (!response.ok) {
      let code = 'DISPATCH_ERROR'
      let message =
        'No fue posible completar el despacho.'

      try {
        const result =
          (await response.json()) as ApiErrorResponse

        code = result.error?.code ?? code
        message =
          result.error?.message ?? message
      } catch {
        // Conservamos el mensaje genérico.
      }

      throw new ApiError(
        message,
        code,
        response.status,
      )
    }

    const result =
      (await response.json()) as DispatchApiResponse

    return result.data
  },
}