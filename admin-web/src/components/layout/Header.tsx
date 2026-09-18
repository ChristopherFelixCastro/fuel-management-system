import React from 'react';
import {
  Menu,
  LogOut,
  Server,
  User as UserIcon,
} from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { RoleBadge } from '../common/Badge';

interface HeaderProps {
  onOpenMobileMenu: () => void;
}

export const Header: React.FC<HeaderProps> = ({
  onOpenMobileMenu,
}) => {
  const { user, logout } = useAuth();

  return (
    <header className="sticky top-0 z-30 h-16 bg-white border-b border-slate-200 px-4 sm:px-6 flex items-center justify-between shadow-xs">
      {/* Left section */}
      <div className="flex items-center gap-3">
        <button
          onClick={onOpenMobileMenu}
          className="p-2 -ml-2 rounded-lg text-slate-600 hover:bg-slate-100 lg:hidden focus:outline-none focus:ring-2 focus:ring-[#087e8b]"
          aria-label="Abrir menú"
        >
          <Menu className="w-5 h-5" />
        </button>

        <div className="hidden sm:flex flex-col">
          <span className="text-xs text-slate-400 font-medium uppercase tracking-wider">
            Sistema de Gestión de Combustible
          </span>
          <span className="text-sm font-semibold text-slate-800">
            Administración Central y Maestros
          </span>
        </div>
      </div>

      {/* Right section */}
      <div className="flex items-center gap-2 sm:gap-4">
        {/* Indicador informativo de conexión */}
        <div
          className="hidden sm:flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg text-xs font-medium bg-emerald-50 text-emerald-800 border border-emerald-300"
          title="Portal conectado al API principal"
        >
          <Server className="w-3.5 h-3.5 text-emerald-600" />
          <span className="hidden md:inline">
            API .NET 8
          </span>
          <span className="md:hidden">API</span>
        </div>

        {/* Usuario autenticado */}
        {user ? (
          <div className="flex items-center gap-3 pl-2 border-l border-slate-200">
            <div className="hidden md:flex flex-col text-right">
              <span className="text-xs font-semibold text-slate-800 leading-tight">
                {user.fullName}
              </span>

              <span className="text-[11px] text-slate-500 leading-tight">
                @{user.username}
              </span>

              <div className="mt-0.5">
                <RoleBadge role={user.role} />
              </div>
            </div>

            <button
              onClick={() => void logout()}
              title="Cerrar sesión"
              className="p-2 rounded-lg text-slate-500 hover:text-red-600 hover:bg-red-50 transition"
              aria-label="Cerrar sesión"
            >
              <LogOut className="w-4 h-4" />
            </button>
          </div>
        ) : (
          <div className="flex items-center gap-1.5 text-slate-400 text-xs">
            <UserIcon className="w-4 h-4" />
            <span>Sin autenticar</span>
          </div>
        )}
      </div>
    </header>
  );
};
