import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  UserCheck,
  Truck,
  Building2,
  ClipboardCheck,
  ClipboardList,
  FileText,
  Fuel,
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
      to: '/my-requests',
      label: 'Mis Solicitudes',
      icon: <FileText className="w-5 h-5" />,
      allowedRoles: ['SOLICITANTE'],
    },
    {
      to: '/dashboard',
      label: 'Dashboard Ejecutivo',
      icon: <LayoutDashboard className="w-5 h-5" />,
      allowedRoles: ['ADMINISTRADOR', 'SUPERVISOR', 'AUDITOR', 'DESPACHADOR'],
    },
    {
      to: '/requests',
      label: 'Gestión de Solicitudes',
      icon: <ClipboardList className="w-5 h-5" />,
      allowedRoles: ['ADMINISTRADOR', 'SUPERVISOR'],
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
        className={`fixed top-0 bottom-0 left-0 z-40 w-64 bg-slate-950 text-white flex flex-col transition-transform duration-300 ease-in-out lg:translate-x-0 ${
          isOpen ? 'translate-x-0' : '-translate-x-full'
        }`}
      >
        {/* Brand */}
        <div className="h-16 flex items-center gap-3 px-5 border-b border-slate-800 bg-slate-950">
          <div className="h-10 w-10 shrink-0 overflow-hidden rounded-xl border border-slate-700 bg-white p-1 shadow-sm">
            <img
              src="/GasolinaLogo.png"
              alt="Logo de La Bomba"
              className="h-full w-full object-contain"
            />
          </div>
          <div className="min-w-0">
            <h1 className="truncate font-bold text-base leading-tight text-white tracking-tight">
              La Bomba
            </h1>
            <span className="text-[10px] text-slate-400 font-semibold tracking-[0.14em] uppercase">
              Administración
            </span>
          </div>
        </div>

        {/* User preview banner in sidebar */}
        {user && (
          <div className="p-4 mx-3 my-3 bg-slate-900 rounded-xl border border-slate-800">
            <p className="text-xs text-slate-200 font-semibold truncate">{user.fullName}</p>
            <div className="flex items-center gap-2 mt-1">
              <span className="text-[10px] uppercase font-bold tracking-wider px-2 py-0.5 rounded bg-[#087e8b] text-white">
                {user.role}
              </span>
              {user.stationName && (
                <span className="text-[10px] text-slate-300 truncate" title={user.stationName}>
                  {user.stationName}
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
                    : 'text-slate-300 hover:bg-slate-900 hover:text-white'
                }`
              }
            >
              {item.icon}
              <span className="flex-1 truncate">{item.label}</span>
              {item.adminOnly && (
                <span className="text-[9px] bg-slate-950 text-cyan-200 px-1.5 py-0.5 rounded border border-cyan-800">
                  Admin
                </span>
              )}
            </NavLink>
          ))}
        </div>

      </aside>
    </>
  );
};
