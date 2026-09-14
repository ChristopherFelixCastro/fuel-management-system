import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { authApi } from '../api/auth.api'
import type { LoginCredentials, User } from '../types/auth'
const STORAGE_KEY = 'fuel-flow-dispatch-session'
interface AuthContextValue { user: User | null; isAuthenticated: boolean; isLoading: boolean; login: (credentials: LoginCredentials) => Promise<void>; logout: () => Promise<void> }
const AuthContext = createContext<AuthContextValue | undefined>(undefined)
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null); const [isLoading, setIsLoading] = useState(true)
  useEffect(() => { const stored = localStorage.getItem(STORAGE_KEY); if (stored) try { setUser(JSON.parse(stored) as User) } catch { localStorage.removeItem(STORAGE_KEY) }; setIsLoading(false) }, [])
  const value = useMemo<AuthContextValue>(() => ({ user, isAuthenticated: user !== null, isLoading,
    async login(credentials) { const currentUser = await authApi.login(credentials); localStorage.setItem(STORAGE_KEY, JSON.stringify(currentUser)); setUser(currentUser) },
    async logout() { await authApi.logout(); localStorage.removeItem(STORAGE_KEY); setUser(null) }
  }), [user, isLoading])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
export function useAuth() { const context = useContext(AuthContext); if (!context) throw new Error('useAuth debe usarse dentro de AuthProvider.'); return context }
