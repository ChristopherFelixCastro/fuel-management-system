import { UserRole } from '../types';

export const ROUTE_PERMISSIONS: Record<string, UserRole[]> = {
  '/my-requests': ['SOLICITANTE'],
  '/dashboard': ['ADMINISTRADOR', 'SUPERVISOR', 'AUDITOR', 'DESPACHADOR'],
  '/requests': ['ADMINISTRADOR', 'SUPERVISOR'],
  '/users': ['ADMINISTRADOR'],
  '/employees': ['ADMINISTRADOR', 'SUPERVISOR', 'AUDITOR'],
  '/vehicles': ['ADMINISTRADOR', 'SUPERVISOR', 'AUDITOR'],
  '/departments': ['ADMINISTRADOR', 'SUPERVISOR', 'AUDITOR'],
  '/inventory': ['ADMINISTRADOR', 'SUPERVISOR', 'AUDITOR'],
  '/closures': ['ADMINISTRADOR', 'SUPERVISOR', 'DESPACHADOR'],
  '/reports': ['ADMINISTRADOR', 'SUPERVISOR', 'AUDITOR'],
  '/alerts': ['ADMINISTRADOR', 'SUPERVISOR', 'AUDITOR'],
};

/**
 * Retorna la ruta inicial por defecto correspondiente al rol del usuario.
 */
export function getDefaultRouteForRole(role?: UserRole | string | null): string {
  if (role === 'SOLICITANTE') {
    return '/my-requests';
  }
  return '/dashboard';
}

/**
 * Normaliza un pathname (e.g. "/users/edit/1" -> "/users").
 */
export function normalizePath(pathname?: string | null): string {
  if (!pathname) return '';
  const clean = pathname.split('?')[0].split('#')[0].trim();
  if (clean === '' || clean === '/') return '/';
  const segments = clean.replace(/^\/+/, '').split('/');
  return `/${segments[0]}`;
}

/**
 * Determina si una ruta específica está permitida para un rol dado.
 */
export function isRouteAllowedForRole(pathname?: string | null, role?: UserRole | string | null): boolean {
  if (!role || !pathname) return false;
  const baseRoute = normalizePath(pathname);

  // La raíz "/" redirige dinámicamente según el rol en HomeRedirect
  if (baseRoute === '/' || baseRoute === '') {
    return true;
  }

  const allowedRoles = ROUTE_PERMISSIONS[baseRoute];
  if (!allowedRoles) {
    return false;
  }

  return allowedRoles.includes(role as UserRole);
}

/**
 * Determina la ruta de destino tras un inicio de sesión.
 * Solo reutiliza 'requestedPath' si el nuevo usuario tiene permisos reales para ella;
 * de lo contrario, prioriza la ruta inicial por defecto del nuevo rol.
 */
export function determineInitialRoute(
  role?: UserRole | string | null,
  requestedPath?: string | null
): string {
  if (
    requestedPath &&
    requestedPath !== '/login' &&
    requestedPath !== '/' &&
    isRouteAllowedForRole(requestedPath, role)
  ) {
    return requestedPath;
  }

  return getDefaultRouteForRole(role);
}
