import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  UserCheck,
  Truck,
  Building2,
  ClipboardCheck,
  Fuel,
  ShieldAlert,
  FileBarChart,
  AlertTriangle,
} from 'lucide-react';
import { useAuth } from '../../context/AuthContext';

interface SidebarProps {
  isOpen: boolean;
  onCloseMobile: () => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ isOpen, onCloseMobile }) => {
  const { user, hasRole } = useAuth();

  const navItems = [
    {
      to: '/dashboard',
      label: 'Dashboard Ejecutivo',
      icon: <LayoutDashboard className="w-5 h-5" />,
      allowedRoles: ['ADMINISTRADOR', 'SUPERVISOR', 'AUDITOR', 'SOLICITANTE', 'DESPACHADOR'],
    },
    {
      to: '/users',
      label: 'Gestión de Usuarios',
      icon: <Users className="w-5 h-5" />,
      allowedRoles: ['ADMINISTRADOR'],
      adminOnly: true,
    },
    {
      to: '/employees',
      label: 'Empleados',
      icon: <UserCheck className="w-5 h-5" />,
      allowedRoles: ['ADMINISTRADOR', 'SUPERVISOR', 'AUDITOR'],
    },
    {
      to: '/vehicles',
      label: 'Vehículos y Flota',
      icon: <Truck className="w-5 h-5" />,
      allowedRoles: ['ADMINISTRADOR', 'SUPERVISOR', 'AUDITOR'],
    },
    {
      to: '/departments',
      label: 'Departamentos',
      icon: <Building2 className="w-5 h-5" />,
      allowedRoles: ['ADMINISTRADOR', 'SUPERVISOR', 'AUDITOR'],
    },
    {
      to: '/inventory',
      label: 'Inventario',
      icon: <Fuel className="w-5 h-5" />,
      allowedRoles: ['ADMINISTRADOR', 'SUPERVISOR', 'AUDITOR'],
    },
    {
      to: '/closures',
      label: 'Cierres diarios',
      icon: <ClipboardCheck className="w-5 h-5" />,
      allowedRoles: ['ADMINISTRADOR', 'SUPERVISOR', 'DESPACHADOR'],
    },
    {
        to: '/reports',
        label: 'Reportes',
        icon: <FileBarChart className="w-5 h-5" />,
        allowedRoles: [
          'ADMINISTRADOR',
          'SUPERVISOR',
          'AUDITOR',
        ],
      },
      {
        to: '/alerts',
        label: 'Alertas',
        icon: <AlertTriangle className="w-5 h-5" />,
        allowedRoles: [
          'ADMINISTRADOR',
          'SUPERVISOR',
          'AUDITOR',
        ],
      },
  ];

  const filteredNavItems = navItems.filter(item =>
    !user || hasRole(item.allowedRoles as any)
  );

  return (
    <>
      {/* Mobile backdrop */}
      {isOpen && (
        <div
          onClick={onCloseMobile}
          className="fixed inset-0 z-40 bg-slate-900/50 backdrop-blur-sm lg:hidden"
        />
      )}

      {/* Sidebar container */}
      <aside
        className={`fixed top-0 bottom-0 left-0 z-40 w-64 bg-[#062d4f] text-white flex flex-col transition-transform duration-300 ease-in-out lg:translate-x-0 ${
          isOpen ? 'translate-x-0' : '-translate-x-full'
        }`}
      >
        {/* Brand */}
        <div className="h-16 flex items-center gap-3 px-6 border-b border-[#0f436e] bg-[#04213a]">
          <div className="p-2 bg-[#087e8b] rounded-lg shadow-sm">
            <Fuel className="w-5 h-5 text-white" />
          </div>
          <div>
            <h1 className="font-bold text-sm leading-tight text-white tracking-wide">
              Combustible PWA
            </h1>
            <span className="text-[11px] text-[#75c5eb] font-medium tracking-wider uppercase">
              Portal Administrativo
            </span>
          </div>
        </div>

        {/* User preview banner in sidebar */}
        {user && (
          <div className="p-4 mx-3 my-3 bg-[#0a3a63] rounded-lg border border-[#144f82]">
            <p className="text-xs text-slate-300 font-medium truncate">{user.fullName}</p>
            <div className="flex items-center gap-2 mt-1">
              <span className="text-[10px] uppercase font-bold tracking-wider px-2 py-0.5 rounded bg-[#087e8b] text-white">
                {user.role}
              </span>
              {user.stationName && (
                <span className="text-[10px] text-slate-300 truncate" title={user.stationName}>
                  📍 {user.stationName}
                </span>
              )}
            </div>
          </div>
        )}

        {/* Navigation Menu */}
        <div className="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
          <div className="px-3 py-1 mb-2 text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
            Módulos del Sistema
          </div>

          {filteredNavItems.map(item => (
            <NavLink
              key={item.to}
              to={item.to}
              onClick={onCloseMobile}
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-[#087e8b] text-white shadow-sm'
                    : 'text-slate-300 hover:bg-[#0a3a63] hover:text-white'
                }`
              }
            >
              {item.icon}
              <span className="flex-1 truncate">{item.label}</span>
              {item.adminOnly && (
                <span className="text-[9px] bg-[#04213a] text-cyan-300 px-1.5 py-0.5 rounded border border-cyan-800">
                  Admin
                </span>
              )}
            </NavLink>
          ))}
        </div>

        {/* Footer info in sidebar */}
        <div className="p-4 border-t border-[#0f436e] bg-[#04213a]/60 text-xs text-slate-400">
          <div className="flex items-center gap-2 mb-1">
            <ShieldAlert className="w-3.5 h-3.5 text-[#0f9aa8]" />
            <span className="font-medium text-slate-300">Reto Tendencia · Angel</span>
          </div>
          <p className="text-[11px] text-slate-400 leading-tight">
            Portal web para control de entidades y dashboard ejecutivo.
          </p>
        </div>
      </aside>
    </>
  );
};
