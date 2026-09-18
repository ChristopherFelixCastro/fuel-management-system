import { authenticatedFetch } from './authenticated-fetch'

import type { AuthSession } from '../types/auth'
import {
  ApiError,
  type ApiErrorResponse,
  type Ticket,
  type TicketValidationResponse,
} from '../types/ticket'

const API_URL =
  import.meta.env.VITE_API_URL ?? 'http://localhost:5080'

interface AuthActions {
  refreshSession: () => Promise<AuthSession | null>
}

export const ticketsApi = {
  async validateQr(
    qrPayload: string,
    session: AuthSession,
    auth: AuthActions,
  ): Promise<Ticket> {
    const response = await authenticatedFetch({
      session,
      refreshSession: auth.refreshSession,
      input: `${API_URL}/tickets/validate`,
      init: {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          qrPayload,
        }),
      },
    })



    if (!response.ok) {
      let code = 'UNKNOWN_ERROR'
      let message =
        'No fue posible validar el ticket.'

      try {
        const result =
          (await response.json()) as ApiErrorResponse

        code = result.error?.code ?? code
        message =
          result.error?.message ?? message
      } catch {
        // Se conserva el mensaje genérico.
      }

      throw new ApiError(
        message,
        code,
        response.status,
      )
    }

    const result =
      (await response.json()) as TicketValidationResponse

    return result.data
  },
}