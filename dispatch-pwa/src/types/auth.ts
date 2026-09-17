export interface User {
  id: string
  name: string
  role: 'DESPACHADOR'
  stationId: string
}

export interface LoginCredentials {
  username: string
  password: string
}

export interface AuthSession {
  user: User
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
}

export interface LoginApiResponse {
  data: {
    accessToken: string
    accessTokenExpiraEn: string
    expiracion: string
    refreshToken: string
    refreshTokenExpiraEn: string
    usuario: string
    rol: string
    usuarioId: string
    estacionId: string
  }
  meta: {
    traceId: string
  }
}