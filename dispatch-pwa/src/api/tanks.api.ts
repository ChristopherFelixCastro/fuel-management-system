import { authenticatedFetch } from './authenticated-fetch'

import type { AuthSession } from '../types/auth'
import type {
  Tank,
  TanksApiResponse,
} from '../types/tank'
import {
  ApiError,
  type ApiErrorResponse,
} from '../types/ticket'

const API_URL =
  import.meta.env.VITE_API_URL ?? 'http://localhost:5080'

interface AuthActions {
  refreshSession: () => Promise<AuthSession | null>
}

export const tanksApi = {
 async getStationTanks(
  stationId: string,
  session: AuthSession,
  auth: AuthActions,
): Promise<Tank[]> {
  const response = await authenticatedFetch({
    session,
    refreshSession: auth.refreshSession,
    input: `${API_URL}/masters/estaciones/${stationId}/tanques`,
  })

  if (!response.ok) {
    let code = 'TANKS_ERROR'
    let message =
      'No fue posible consultar los tanques de la estación.'

    try {
      const result =
        (await response.json()) as ApiErrorResponse

      code = result.error?.code ?? code
      message = result.error?.message ?? message
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
    (await response.json()) as TanksApiResponse

  return result.data.filter(
    (tank) => tank.activo,
  )
},
  async getCompatibleTanks(
    stationId: string,
    fuelTypeId: number,
    session: AuthSession,
    auth: AuthActions,
  ): Promise<Tank[]> {
    const url = new URL(
      `${API_URL}/masters/estaciones/${stationId}/tanques`,
    )

    url.searchParams.set(
      'tipoCombustibleId',
      fuelTypeId.toString(),
    )

    const response = await authenticatedFetch({
      session,
      refreshSession: auth.refreshSession,
      input: url,
    })

    if (!response.ok) {
      let code = 'TANKS_ERROR'
      let message =
        'No fue posible consultar los tanques disponibles.'

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
      (await response.json()) as TanksApiResponse

    return result.data.filter(
      (tank) => tank.activo,
    )
  },
}