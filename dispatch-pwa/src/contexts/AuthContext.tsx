import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'

import { authApi } from '../api/auth.api'
import type {
  AuthSession,
  LoginCredentials,
  User,
} from '../types/auth'

const STORAGE_KEY = 'fuel-flow-dispatch-session'

interface AuthContextValue {
  user: User | null
  session: AuthSession | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (credentials: LoginCredentials) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const stored = localStorage.getItem(STORAGE_KEY)

    if (stored) {
      try {
        const parsed = JSON.parse(stored) as AuthSession

        const expiration = new Date(
          parsed.accessTokenExpiresAt,
        ).getTime()

        if (expiration > Date.now()) {
          setSession(parsed)
        } else {
          localStorage.removeItem(STORAGE_KEY)
        }
      } catch {
        localStorage.removeItem(STORAGE_KEY)
      }
    }

    setIsLoading(false)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user: session?.user ?? null,
      session,
      isAuthenticated: session !== null,
      isLoading,

      async login(credentials) {
        const newSession = await authApi.login(credentials)

        localStorage.setItem(
          STORAGE_KEY,
          JSON.stringify(newSession),
        )

        setSession(newSession)
      },

      async logout() {
        if (session) {
          await authApi.logout(session)
        }

        localStorage.removeItem(STORAGE_KEY)
        setSession(null)
      },
    }),
    [session, isLoading],
  )

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)

  if (!context) {
    throw new Error(
      'useAuth debe usarse dentro de AuthProvider.',
    )
  }

  return context
}