import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { Fuel, Lock, User, AlertCircle, Sparkles } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { UserRole } from '../types';

export const Login: React.FC = () => {
  const [usernameOrEmail, setUsernameOrEmail] = useState('');
  const [password, setPassword] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { login, isMock } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const from = (location.state as any)?.from?.pathname || '/dashboard';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setIsSubmitting(true);

    try {
      await login({ usernameOrEmail, password });
      navigate(from, { replace: true });
    } catch (err: any) {
      setErrorMessage(err.message || 'Credenciales inválidas. Por favor verifique sus datos.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const fillDemoCredentials = (role: UserRole) => {
    const creds: Record<UserRole, { user: string; pass: string }> = {
      ADMINISTRADOR: { user: 'admin', pass: 'Password123!' },
      SUPERVISOR: { user: 'supervisor', pass: 'Password123!' },
      DESPACHADOR: { user: 'despachador', pass: 'Password123!' },
      SOLICITANTE: { user: 'solicitante', pass: 'Password123!' },
      AUDITOR: { user: 'auditor', pass: 'Password123!' },
    };
    setUsernameOrEmail(creds[role].user);
    setPassword(creds[role].pass);
  };

  return (
    <div className="min-h-screen bg-linear-to-br from-[#062d4f] via-[#074b68] to-[#087e8b] flex flex-col justify-center py-12 sm:px-6 lg:px-8">
      <div className="sm:mx-auto sm:w-full sm:max-w-md text-center">
        <div className="inline-flex items-center justify-center p-3.5 bg-white/10 backdrop-blur-md rounded-2xl border border-white/20 shadow-xl mb-4">
          <Fuel className="w-10 h-10 text-cyan-300" />
        </div>
        <h2 className="text-3xl font-extrabold text-white tracking-tight">
          Portal Administrativo
        </h2>
        <p className="mt-2 text-sm text-cyan-100 font-medium">
          Gestión de Tickets Digitales e Inventario de Combustible
        </p>
      </div>

      <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div className="bg-white py-8 px-6 shadow-2xl rounded-2xl sm:px-10 border border-slate-100">
          {errorMessage && (
            <div className="mb-5 p-3.5 bg-rose-50 border border-rose-200 rounded-lg flex items-start gap-3 text-rose-800 text-sm">
              <AlertCircle className="w-5 h-5 text-rose-600 shrink-0 mt-0.5" />
              <span>{errorMessage}</span>
            </div>
          )}

          <form className="space-y-5" onSubmit={handleSubmit}>
            <div>
              <label
                htmlFor="usernameOrEmail"
                className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5"
              >
                Usuario o Correo Electrónico
              </label>
              <div className="relative rounded-lg shadow-2xs">
                <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                  <User className="w-4 h-4" />
                </div>
                <input
                  id="usernameOrEmail"
                  name="usernameOrEmail"
                  type="text"
                  required
                  value={usernameOrEmail}
                  onChange={e => setUsernameOrEmail(e.target.value)}
                  placeholder="ej. admin o usuario@empresa.com"
                  className="block w-full pl-10 pr-3 py-2.5 border border-slate-300 rounded-lg text-sm placeholder-slate-400 focus:outline-hidden focus:ring-2 focus:ring-[#087e8b] focus:border-[#087e8b] transition"
                />
              </div>
            </div>

            <div>
              <label
                htmlFor="password"
                className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5"
              >
                Contraseña
              </label>
              <div className="relative rounded-lg shadow-2xs">
                <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                  <Lock className="w-4 h-4" />
                </div>
                <input
                  id="password"
                  name="password"
                  type="password"
                  required
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  placeholder="••••••••"
                  className="block w-full pl-10 pr-3 py-2.5 border border-slate-300 rounded-lg text-sm placeholder-slate-400 focus:outline-hidden focus:ring-2 focus:ring-[#087e8b] focus:border-[#087e8b] transition"
                />
              </div>
            </div>

            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full flex justify-center py-2.5 px-4 border border-transparent rounded-lg shadow-sm text-sm font-semibold text-white bg-[#087e8b] hover:bg-[#066570] focus:outline-hidden focus:ring-2 focus:ring-offset-2 focus:ring-[#087e8b] transition disabled:opacity-50"
            >
              {isSubmitting ? 'Iniciando sesión...' : 'Ingresar al Portal'}
            </button>
          </form>

          {/* Quick presets for evaluation / demo */}
          <div className="mt-6 pt-5 border-t border-slate-200">
            <div className="flex items-center gap-1.5 text-xs text-slate-500 font-semibold mb-3">
              <Sparkles className="w-3.5 h-3.5 text-amber-500" />
              <span>Accesos rápidos para demostración:</span>
            </div>
            <div className="grid grid-cols-2 gap-2">
              <button
                type="button"
                onClick={() => fillDemoCredentials('ADMINISTRADOR')}
                className="text-left px-2.5 py-1.5 rounded-md bg-purple-50 hover:bg-purple-100 border border-purple-200 text-purple-800 text-xs font-medium transition"
              >
                👤 Administrador
              </button>
              <button
                type="button"
                onClick={() => fillDemoCredentials('SUPERVISOR')}
                className="text-left px-2.5 py-1.5 rounded-md bg-blue-50 hover:bg-blue-100 border border-blue-200 text-blue-800 text-xs font-medium transition"
              >
                📋 Supervisor
              </button>
              <button
                type="button"
                onClick={() => fillDemoCredentials('DESPACHADOR')}
                className="text-left px-2.5 py-1.5 rounded-md bg-amber-50 hover:bg-amber-100 border border-amber-200 text-amber-800 text-xs font-medium transition"
              >
                ⛽ Despachador
              </button>
              <button
                type="button"
                onClick={() => fillDemoCredentials('AUDITOR')}
                className="text-left px-2.5 py-1.5 rounded-md bg-slate-100 hover:bg-slate-200 border border-slate-300 text-slate-800 text-xs font-medium transition"
              >
                🔍 Auditor
              </button>
            </div>
            {isMock && (
              <p className="text-[11px] text-slate-400 mt-2 text-center">
                * Contraseña demo estándar: <code className="bg-slate-100 px-1 py-0.5 rounded">Password123!</code>
              </p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
