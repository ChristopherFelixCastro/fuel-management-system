import React, { createContext, useContext, useState, useEffect } from 'react';
import { AuthUser, LoginCredentials, UserRole } from '../types';
import { AuthService } from '../services/api';
import { isMockEnabled, setMockEnabled } from '../services/apiClient';

interface AuthContextType {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  isMock: boolean;
  login: (credentials: LoginCredentials) => Promise<void>;
  logout: () => Promise<void>;
  hasRole: (roles: UserRole[]) => boolean;
  toggleMock: () => void;
  switchDemoUser: (role: UserRole) => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [isMock, setIsMock] = useState<boolean>(isMockEnabled());

  useEffect(() => {
    const initAuth = () => {
      const storedUser = localStorage.getItem('fms_auth_user');
      const token = localStorage.getItem('fms_access_token');
      if (storedUser && token) {
        try {
          setUser(JSON.parse(storedUser));
        } catch {
          localStorage.removeItem('fms_auth_user');
          localStorage.removeItem('fms_access_token');
        }
      }
      setIsLoading(false);
    };

    initAuth();

    const handleUnauthorized = () => {
      setUser(null);
    };

    window.addEventListener('fms:auth-unauthorized', handleUnauthorized);
    return () => {
      window.removeEventListener('fms:auth-unauthorized', handleUnauthorized);
    };
  }, []);

  const login = async (credentials: LoginCredentials) => {
    setIsLoading(true);
    try {
      const response = await AuthService.login(credentials);
      const { accessToken, user: authUser } = response.data;
      localStorage.setItem('fms_access_token', accessToken);
      localStorage.setItem('fms_auth_user', JSON.stringify(authUser));
      setUser(authUser);
    } finally {
      setIsLoading(false);
    }
  };

  const logout = async () => {
    try {
      await AuthService.logout();
    } catch {
      // Ignorar errores al cerrar sesión
    } finally {
      localStorage.removeItem('fms_access_token');
      localStorage.removeItem('fms_auth_user');
      setUser(null);
    }
  };

  const hasRole = (roles: UserRole[]): boolean => {
    if (!user) return false;
    return roles.includes(user.role);
  };

  const toggleMock = () => {
    const next = !isMock;
    setIsMock(next);
    setMockEnabled(next);
  };

  // Ayudante de demostración rápida para cambiar rol de usuario al instante
  const switchDemoUser = async (role: UserRole) => {
    const roleUsernameMap: Record<UserRole, string> = {
      ADMINISTRADOR: 'admin',
      SUPERVISOR: 'supervisor',
      DESPACHADOR: 'despachador',
      SOLICITANTE: 'solicitante',
      AUDITOR: 'auditor',
    };
    await login({ usernameOrEmail: roleUsernameMap[role], password: 'Password123!' });
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        isMock,
        login,
        logout,
        hasRole,
        toggleMock,
        switchDemoUser,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth debe utilizarse dentro de un AuthProvider');
  }
  return context;
};
