import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
} from 'react';

import {
  AuthUser,
  LoginCredentials,
  UserRole,
} from '../types';

import { AuthService } from '../services/api';

import {
  clearTokens,
  getAccessToken,
  getRefreshToken,
  saveTokens,
} from '../services/apiClient';

interface AuthContextType {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  isMock: boolean;
  login: (credentials: LoginCredentials) => Promise<void>;
  logout: () => Promise<void>;
  hasRole: (roles: UserRole[]) => boolean;
}

const AuthContext =
  createContext<AuthContextType | undefined>(undefined);

const USER_KEY = 'fms_auth_user';

function saveUser(user: AuthUser): void {
  localStorage.setItem(USER_KEY, JSON.stringify(user));
}

function clearLocalSession(): void {
  clearTokens();
  localStorage.removeItem(USER_KEY);
}

function readStoredUser(): AuthUser | null {
  const raw = localStorage.getItem(USER_KEY);

  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as AuthUser;
  } catch {
    localStorage.removeItem(USER_KEY);
    return null;
  }
}

export const AuthProvider: React.FC<{
  children: React.ReactNode;
}> = ({ children }) => {
  const [user, setUser] = useState<AuthUser | null>(
    null,
  );

  const [isLoading, setIsLoading] =
    useState<boolean>(true);

  /*
   * La versión integrada trabaja contra el Core real.
   * Se mantiene la propiedad por compatibilidad visual
   * con componentes existentes de Angel.
   */
  const isMock = false;

  const clearSession = useCallback(() => {
    clearLocalSession();
    setUser(null);
  }, []);

  useEffect(() => {
    const initialize = async () => {
      const storedUser = readStoredUser();

      if (
        !storedUser ||
        (!getAccessToken() && !getRefreshToken())
      ) {
        clearLocalSession();
        setIsLoading(false);
        return;
      }

      /*
       * /auth/me valida la sesión real. Si el access token
       * expiró, apiRequest intentará refresh automáticamente.
       */
      try {
        const response = await AuthService.getMe();
        const profile = response.data;

        const authenticatedUser: AuthUser = {
          id: profile.id,
          username: profile.usuario,
          email: profile.email,
          fullName:
            profile.empleadoNombre || profile.usuario,
          role: profile.rol,
          employeeId: profile.empleadoId ?? null,
          stationId:
            profile.estacionId ??
            profile.stationId ??
            null,
          stationName:
            profile.estacionNombre ?? null,
        };

        saveUser(authenticatedUser);
        setUser(authenticatedUser);
      } catch {
        clearSession();
      } finally {
        setIsLoading(false);
      }
    };

    void initialize();

    const handleUnauthorized = () => {
      clearSession();
    };

    window.addEventListener(
      'fms:auth-unauthorized',
      handleUnauthorized,
    );

    return () => {
      window.removeEventListener(
        'fms:auth-unauthorized',
        handleUnauthorized,
      );
    };
  }, [clearSession]);

  const login = useCallback(
    async (credentials: LoginCredentials) => {
      setIsLoading(true);

      try {
        const loginResponse =
          await AuthService.login(credentials);

        const auth = loginResponse.data;

        saveTokens({
          accessToken: auth.accessToken,
          accessTokenExpiraEn:
            auth.accessTokenExpiraEn,
          refreshToken: auth.refreshToken,
          refreshTokenExpiraEn:
            auth.refreshTokenExpiraEn,
        });

        /*
         * Consultamos /auth/me después del login para obtener
         * el perfil oficial completo y no reconstruirlo a
         * partir de información parcial del JWT.
         */
        const profileResponse =
          await AuthService.getMe();

        const profile = profileResponse.data;

        const authenticatedUser: AuthUser = {
          id: profile.id,
          username: profile.usuario,
          email: profile.email,
          fullName:
            profile.empleadoNombre || profile.usuario,
          role: profile.rol,
          employeeId: profile.empleadoId ?? null,
          stationId:
            profile.estacionId ??
            profile.stationId ??
            null,
          stationName:
            profile.estacionNombre ?? null,
        };

        saveUser(authenticatedUser);
        setUser(authenticatedUser);
      } catch (error) {
        clearSession();
        throw error;
      } finally {
        setIsLoading(false);
      }
    },
    [clearSession],
  );

  const logout = useCallback(async () => {
    const refreshToken = getRefreshToken();

    try {
      if (refreshToken && getAccessToken()) {
        await AuthService.logout(refreshToken);
      }
    } catch {
      // El cierre local debe ocurrir aunque el Core no responda.
    } finally {
      clearSession();
    }
  }, [clearSession]);

  const hasRole = useCallback(
    (roles: UserRole[]): boolean => {
      if (!user) {
        return false;
      }

      return roles.includes(user.role);
    },
    [user],
  );

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: user !== null,
        isLoading,
        isMock,
        login,
        logout,
        hasRole,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error(
      'useAuth debe utilizarse dentro de un AuthProvider',
    );
  }

  return context;
};
