import React, { useState } from 'react';
import { 
  Menu, 
  LogOut, 
  Database, 
  Server, 
  User as UserIcon, 
  ChevronDown
} from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { RoleBadge } from '../common/Badge';
import { UserRole } from '../../types';

interface HeaderProps {
  onOpenMobileMenu: () => void;
}

export const Header: React.FC<HeaderProps> = ({ onOpenMobileMenu }) => {
  const { user, logout, isMock, toggleMock, switchDemoUser } = useAuth();
  const [showRoleMenu, setShowRoleMenu] = useState(false);

  const rolesList: UserRole[] = [
    'ADMINISTRADOR',
    'SUPERVISOR',
    'DESPACHADOR',
    'SOLICITANTE',
    'AUDITOR',
  ];

  return (
    <header className="sticky top-0 z-30 h-16 bg-white border-b border-slate-200 px-4 sm:px-6 flex items-center justify-between shadow-xs">
      {/* Left section: Hamburger button for mobile */}
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

      {/* Right section: Controls, Mock toggle, Role quick switch & Profile */}
      <div className="flex items-center gap-2 sm:gap-4">
        {/* Toggle between Mock and Real API */}
        <button
          onClick={toggleMock}
          title={isMock ? 'Cambiando a API REST real' : 'Cambiando a Datos Mock locales'}
          className={`flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg text-xs font-medium border transition-colors ${
            isMock
              ? 'bg-amber-50 text-amber-800 border-amber-300 hover:bg-amber-100'
              : 'bg-emerald-50 text-emerald-800 border-emerald-300 hover:bg-emerald-100'
          }`}
        >
          {isMock ? (
            <>
              <Database className="w-3.5 h-3.5 text-amber-600" />
              <span className="hidden md:inline">Modo: Mock Demo</span>
              <span className="md:hidden">Mock</span>
            </>
          ) : (
            <>
              <Server className="w-3.5 h-3.5 text-emerald-600" />
              <span className="hidden md:inline">Modo: API .NET 8</span>
              <span className="md:hidden">API</span>
            </>
          )}
        </button>

        {/* Demo Quick Role Switcher */}
        <div className="relative">
          <button
            onClick={() => setShowRoleMenu(!showRoleMenu)}
            className="hidden sm:flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg text-xs font-medium bg-slate-100 text-slate-700 hover:bg-slate-200 border border-slate-200 transition"
          >
            <span>Cambiar Rol Demo</span>
            <ChevronDown className="w-3 h-3 text-slate-500" />
          </button>

          {showRoleMenu && (
            <div className="absolute right-0 mt-2 w-48 bg-white border border-slate-200 rounded-lg shadow-lg py-1 z-50 animate-fade-in">
              <div className="px-3 py-1.5 text-[11px] font-semibold text-slate-400 uppercase tracking-wider border-b border-slate-100">
                Simular Usuario
              </div>
              {rolesList.map(r => (
                <button
                  key={r}
                  onClick={() => {
                    switchDemoUser(r);
                    setShowRoleMenu(false);
                  }}
                  className={`w-full text-left px-3 py-2 text-xs hover:bg-slate-50 flex items-center justify-between ${
                    user?.role === r ? 'font-bold text-[#087e8b]' : 'text-slate-700'
                  }`}
                >
                  <span>{r}</span>
                  {user?.role === r && <span className="text-[10px] text-emerald-600">Actual</span>}
                </button>
              ))}
            </div>
          )}
        </div>

        {/* User preview and Logout */}
        {user ? (
          <div className="flex items-center gap-3 pl-2 border-l border-slate-200">
            <div className="hidden md:flex flex-col text-right">
              <span className="text-xs font-semibold text-slate-800 leading-tight">
                {user.fullName}
              </span>
              <div className="mt-0.5">
                <RoleBadge role={user.role} />
              </div>
            </div>

            <button
              onClick={logout}
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
