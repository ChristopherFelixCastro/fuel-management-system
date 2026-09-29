import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Menu,
  LogOut,
  User as UserIcon,
} from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { RoleBadge } from '../common/Badge';
import { ConfirmModal } from '../common/ConfirmModal';

interface HeaderProps {
  onOpenMobileMenu: () => void;
}

export const Header: React.FC<HeaderProps> = ({
  onOpenMobileMenu,
}) => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [showLogoutConfirm, setShowLogoutConfirm] = useState(false);
  const [isLoggingOut, setIsLoggingOut] = useState(false);

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

        <div className="flex items-center gap-2.5 lg:hidden">
          <div className="h-8 w-8 overflow-hidden rounded-lg border border-slate-200 bg-white p-1">
            <img
              src="/GasolinaLogo.png"
              alt="Logo de La Bomba"
              className="h-full w-full object-contain"
            />
          </div>
          <div className="flex flex-col">
            <span className="text-sm font-bold leading-tight text-slate-900">
              La Bomba
            </span>
            <span className="text-[10px] font-semibold uppercase tracking-wider text-slate-500">
              Administración
            </span>
          </div>
        </div>

        <div className="hidden lg:flex flex-col">
          <span className="text-xs text-slate-400 font-semibold uppercase tracking-wider">
            Portal administrativo
          </span>
          <span className="text-sm font-semibold text-slate-800">
            Administración de La Bomba
          </span>
        </div>
      </div>

      {/* Right section */}
      <div className="flex items-center gap-2 sm:gap-4">
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
              onClick={() => setShowLogoutConfirm(true)}
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

      <ConfirmModal
        isOpen={showLogoutConfirm}
        title="Cerrar sesión"
        message="¿Estás seguro de que deseas salir de la plataforma La Bomba?"
        confirmLabel="Cerrar sesión"
        cancelLabel="Cancelar"
        isDestructive={true}
        isLoading={isLoggingOut}
        onConfirm={async () => {
          if (isLoggingOut) return;
          setIsLoggingOut(true);
          try {
            await logout();
            navigate('/login', { replace: true, state: null });
          } finally {
            setIsLoggingOut(false);
            setShowLogoutConfirm(false);
          }
        }}
        onClose={() => {
          if (!isLoggingOut) {
            setShowLogoutConfirm(false);
          }
        }}
      />
    </header>
  );
};
