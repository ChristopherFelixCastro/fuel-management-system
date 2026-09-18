import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
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

  login: (
    credentials: LoginCredentials,
  ) => Promise<void>

  logout: () => Promise<void>

  refreshSession: () => Promise<AuthSession | null>

  clearSession: () => void
}

const AuthContext =
  createContext<AuthContextValue | undefined>(
    undefined,
  )

function saveSession(session: AuthSession) {
  localStorage.setItem(
    STORAGE_KEY,
    JSON.stringify(session),
  )
}

function removeSession() {
  localStorage.removeItem(STORAGE_KEY)
}

export function AuthProvider({
  children,
}: {
  children: ReactNode
}) {
  const [session, setSession] =
    useState<AuthSession | null>(null)

  const [isLoading, setIsLoading] =
    useState(true)

  /*
   * Si varias peticiones reciben 401 al mismo tiempo,
   * todas compartirán una única renovación.
   */
  const refreshPromiseRef =
    useRef<Promise<AuthSession | null> | null>(
      null,
    )

  useEffect(() => {
    const stored =
      localStorage.getItem(STORAGE_KEY)

    if (stored) {
      try {
        const parsed =
          JSON.parse(stored) as AuthSession

        const refreshExpiration = new Date(
          parsed.refreshTokenExpiresAt,
        ).getTime()

        if (
          Number.isFinite(refreshExpiration) &&
          refreshExpiration > Date.now()
        ) {
          /*
           * No eliminamos la sesión solo porque el
           * access token venció. El refresh token
           * todavía puede recuperarla.
           */
          setSession(parsed)
        } else {
          removeSession()
        }
      } catch {
        removeSession()
      }
    }

    setIsLoading(false)
  }, [])

  const clearSession = useCallback(() => {
    removeSession()
    setSession(null)
  }, [])

  const refreshSession =
    useCallback(async (): Promise<AuthSession | null> => {
      if (!session) {
        return null
      }

      const refreshExpiration = new Date(
        session.refreshTokenExpiresAt,
      ).getTime()

      if (
        !Number.isFinite(refreshExpiration) ||
        refreshExpiration <= Date.now()
      ) {
        clearSession()
        return null
      }

      /*
       * Evita ejecutar varias rotaciones del mismo
       * refresh token simultáneamente.
       */
      if (refreshPromiseRef.current) {
        return refreshPromiseRef.current
      }

      const refreshPromise = (async () => {
        try {
          const newSession =
            await authApi.refresh(session)

          saveSession(newSession)
          setSession(newSession)

          return newSession
        } catch {
          clearSession()
          return null
        } finally {
          refreshPromiseRef.current = null
        }
      })()

      refreshPromiseRef.current = refreshPromise

      return refreshPromise
    }, [session, clearSession])

  const login = useCallback(
    async (credentials: LoginCredentials) => {
      const newSession =
        await authApi.login(credentials)

      saveSession(newSession)
      setSession(newSession)
    },
    [],
  )

  const logout = useCallback(async () => {
    if (session) {
      await authApi.logout(session)
    }

    clearSession()
  }, [session, clearSession])

  const value = useMemo<AuthContextValue>(
    () => ({
      user: session?.user ?? null,
      session,
      isAuthenticated: session !== null,
      isLoading,
      login,
      logout,
      refreshSession,
      clearSession,
    }),
    [
      session,
      isLoading,
      login,
      logout,
      refreshSession,
      clearSession,
    ],
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