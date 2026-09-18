import React from 'react';
import { AlertCircle, CheckCircle2, AlertTriangle, Info, X } from 'lucide-react';
import { ApiErrorDetail } from '../../types';

export type AlertType = 'success' | 'error' | 'warning' | 'info';

interface AlertBannerProps {
  type?: AlertType;
  title?: string;
  message?: string;
  errors?: ApiErrorDetail[];
  onClose?: () => void;
  className?: string;
}

export const AlertBanner: React.FC<AlertBannerProps> = ({
  type = 'info',
  title,
  message,
  errors = [],
  onClose,
  className = '',
}) => {
  const styles = {
    success: {
      bg: 'bg-emerald-50 border-emerald-300 text-emerald-900',
      icon: <CheckCircle2 className="w-5 h-5 text-emerald-600 shrink-0" />,
    },
    error: {
      bg: 'bg-red-50 border-red-300 text-red-900',
      icon: <AlertCircle className="w-5 h-5 text-red-600 shrink-0" />,
    },
    warning: {
      bg: 'bg-amber-50 border-amber-300 text-amber-900',
      icon: <AlertTriangle className="w-5 h-5 text-amber-600 shrink-0" />,
    },
    info: {
      bg: 'bg-sky-50 border-sky-300 text-sky-900',
      icon: <Info className="w-5 h-5 text-sky-600 shrink-0" />,
    },
  };

  const current = styles[type];

  return (
    <div className={`p-4 border rounded-lg flex items-start gap-3 shadow-sm ${current.bg} ${className}`}>
      {current.icon}
      <div className="flex-1 text-sm">
        {title && <h4 className="font-semibold mb-1">{title}</h4>}
        {message && <p>{message}</p>}
        {errors && errors.length > 0 && (
          <ul className="mt-2 list-disc list-inside space-y-1 text-xs">
            {errors.map((err, idx) => (
              <li key={idx}>
                {err.field ? <strong className="capitalize">{err.field}: </strong> : null}
                {err.detail || err.code}
              </li>
            ))}
          </ul>
        )}
      </div>
      {onClose && (
        <button
          onClick={onClose}
          className="text-slate-400 hover:text-slate-700 transition p-0.5 rounded"
        >
          <X className="w-4 h-4" />
        </button>
      )}
    </div>
  );
};
