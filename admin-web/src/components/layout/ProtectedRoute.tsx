import React from 'react';
import { Navigate, useLocation, Link } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { UserRole } from '../../types';
import { LoadingSpinner } from '../common/LoadingSpinner';
import { ShieldAlert, ArrowLeft } from 'lucide-react';

interface ProtectedRouteProps {
  children: React.ReactNode;
  allowedRoles?: UserRole[];
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  allowedRoles,
}) => {
  const { user, isAuthenticated, isLoading, hasRole } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-[#eef4f7]">
        <LoadingSpinner message="Verificando sesión..." size="lg" />
      </div>
    );
  }

  if (!isAuthenticated || !user) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (allowedRoles && !hasRole(allowedRoles)) {
    return (
      <div className="max-w-xl mx-auto my-12 p-8 bg-white rounded-xl shadow-md border border-slate-200 text-center">
        <div className="w-16 h-16 bg-rose-100 text-rose-600 rounded-full flex items-center justify-center mx-auto mb-4">
          <ShieldAlert className="w-8 h-8" />
        </div>
        <h2 className="text-xl font-bold text-slate-900 mb-2">Acceso No Autorizado (403)</h2>
        <p className="text-slate-600 text-sm mb-6 leading-relaxed">
          Tu rol actual (<strong>{user.role}</strong>) no tiene los permisos requeridos para acceder a esta sección administrativa.
        </p>
        <Link
          to="/dashboard"
          className="inline-flex items-center gap-2 px-5 py-2.5 bg-[#087e8b] text-white text-sm font-medium rounded-lg hover:bg-[#066570] transition shadow-sm"
        >
          <ArrowLeft className="w-4 h-4" />
          Volver al Dashboard
        </Link>
      </div>
    );
  }

  return <>{children}</>;
};
