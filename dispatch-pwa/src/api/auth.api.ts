import type { LoginCredentials, User } from '../types/auth'
export interface AuthApi { login(credentials: LoginCredentials): Promise<User>; logout(): Promise<void> }
const MOCK_USER: User = { id: 'dispatcher-001', name: 'Christopher', role: 'dispatcher', stationName: 'Estación principal' }
export const authApi: AuthApi = {
  async login({ username, password }) { await new Promise((resolve) => setTimeout(resolve, 350)); if (!username.trim() || !password.trim()) throw new Error('Ingresa tu usuario y contraseña.'); return { ...MOCK_USER, name: username.trim() } },
  async logout() { await Promise.resolve() }
}
