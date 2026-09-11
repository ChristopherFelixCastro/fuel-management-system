import React from 'react';
import { UserRole } from '../../types';

interface RoleBadgeProps {
  role: UserRole;
}

export const RoleBadge: React.FC<RoleBadgeProps> = ({ role }) => {
  const roleStyles: Record<UserRole, string> = {
    ADMINISTRADOR: 'bg-purple-100 text-purple-800 border-purple-200',
    SUPERVISOR: 'bg-blue-100 text-blue-800 border-blue-200',
    DESPACHADOR: 'bg-amber-100 text-amber-800 border-amber-200',
    SOLICITANTE: 'bg-emerald-100 text-emerald-800 border-emerald-200',
    AUDITOR: 'bg-slate-100 text-slate-800 border-slate-200',
  };

  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold border ${
        roleStyles[role] || 'bg-gray-100 text-gray-800 border-gray-200'
      }`}
    >
      {role}
    </span>
  );
};

interface StatusBadgeProps {
  isActive: boolean;
  activeLabel?: string;
  inactiveLabel?: string;
}

export const StatusBadge: React.FC<StatusBadgeProps> = ({
  isActive,
  activeLabel = 'Activo',
  inactiveLabel = 'Inactivo',
}) => {
  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold border ${
        isActive
          ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
          : 'bg-rose-50 text-rose-700 border-rose-200'
      }`}
    >
      <span
        className={`w-1.5 h-1.5 rounded-full mr-1.5 ${
          isActive ? 'bg-emerald-500' : 'bg-rose-500'
        }`}
      />
      {isActive ? activeLabel : inactiveLabel}
    </span>
  );
};
