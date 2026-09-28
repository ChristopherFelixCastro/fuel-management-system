import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  CheckCircle2,
  XCircle,
  AlertCircle,
  Clock,
  Download,
  Fuel,
  Car,
  MapPin,
  Calendar,
  User,
  ShieldCheck,
  RefreshCw,
} from 'lucide-react';
import { PublicTicketService, PublicTicketData } from '../services/api';
import { saveDownloadedFile } from '../services/apiClient';

export const PublicTicket: React.FC = () => {
  const { token } = useParams<{ token: string }>();

  const [ticket, setTicket] = useState<PublicTicketData | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const [qrBlobUrl, setQrBlobUrl] = useState<string | null>(null);
  const [qrFileResponse, setQrFileResponse] = useState<any>(null);
  const [isLoadingQr, setIsLoadingQr] = useState<boolean>(false);

  const loadTicket = async () => {
    if (!token) {
      setError('Token de ticket no proporcionado en el enlace.');
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const res = await PublicTicketService.getByToken(token);
      setTicket(res.data);

      if (res.data.permiteDespacho) {
        loadQrImage();
      }
    } catch (err: any) {
      setError(
        err.message ||
          'No fue posible cargar el ticket. El enlace puede ser inválido o haber expirado.'
      );
    } finally {
      setIsLoading(false);
    }
  };

  const loadQrImage = async () => {
    if (!token) return;
    setIsLoadingQr(true);
    try {
      const fileRes = await PublicTicketService.getQrImage(token);
      setQrFileResponse(fileRes);
      const url = URL.createObjectURL(fileRes.blob);
      setQrBlobUrl(url);
    } catch (err: any) {
      console.error('Error al cargar la imagen QR pública:', err);
    } finally {
      setIsLoadingQr(false);
    }
  };

  useEffect(() => {
    loadTicket();
    return () => {
      if (qrBlobUrl) {
        URL.revokeObjectURL(qrBlobUrl);
      }
    };
  }, [token]);

  const handleDownloadQr = () => {
    if (qrFileResponse && ticket) {
      saveDownloadedFile({
        ...qrFileResponse,
        fileName: `LaBomba-${ticket.numeroTicket}-QR.png`,
      });
    }
  };

  const getStatusBadge = (estado: string) => {
    switch (estado.toUpperCase()) {
      case 'ACTIVO':
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-teal-50 text-[#087e8b] border border-teal-200">
            <CheckCircle2 className="w-3.5 h-3.5" />
            ACTIVO / AUTORIZADO
          </span>
        );
      case 'CONSUMIDO':
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-slate-100 text-slate-700 border border-slate-300">
            <Clock className="w-3.5 h-3.5" />
            CONSUMIDO / UTILIZADO
          </span>
        );
      case 'VENCIDO':
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-amber-50 text-amber-700 border border-amber-200">
            <AlertCircle className="w-3.5 h-3.5" />
            VENCIDO / EXPIRADO
          </span>
        );
      case 'ANULADO':
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-red-50 text-red-700 border border-red-200">
            <XCircle className="w-3.5 h-3.5" />
            ANULADO
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-slate-100 text-slate-600">
            {estado}
          </span>
        );
    }
  };

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col justify-between py-6 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full mx-auto space-y-5">
        {/* Header con Identidad La Bomba */}
        <div className="bg-[#062d4f] text-white p-5 rounded-2xl shadow-md flex items-center justify-between">
          <div className="flex items-center gap-3">
            <img
              src="/GasolinaLogo.png"
              alt="Logo La Bomba"
              className="w-10 h-10 object-contain bg-white rounded-lg p-1"
            />
            <div>
              <h1 className="text-lg font-extrabold tracking-tight">LA BOMBA</h1>
              <p className="text-xs text-cyan-300 font-medium">Ticket Digital de Combustible</p>
            </div>
          </div>
          <button
            type="button"
            onClick={loadTicket}
            disabled={isLoading}
            className="p-2 text-slate-300 hover:text-white hover:bg-white/10 rounded-lg transition disabled:opacity-50 cursor-pointer"
            title="Actualizar estado del ticket"
          >
            <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} />
          </button>
        </div>

        {/* Estado de Carga */}
        {isLoading && (
          <div className="bg-white p-8 rounded-2xl border border-slate-200 shadow-sm text-center space-y-3">
            <RefreshCw className="w-8 h-8 text-[#087e8b] animate-spin mx-auto" />
            <p className="text-sm font-semibold text-slate-700">Verificando ticket seguro...</p>
          </div>
        )}

        {/* Error */}
        {!isLoading && error && (
          <div className="bg-white p-6 rounded-2xl border border-red-200 shadow-sm text-center space-y-3">
            <div className="w-12 h-12 bg-red-50 text-red-600 rounded-full flex items-center justify-center mx-auto">
              <XCircle className="w-6 h-6" />
            </div>
            <h2 className="text-base font-bold text-slate-900">Enlace No Válido</h2>
            <p className="text-xs text-slate-600 leading-relaxed">{error}</p>
          </div>
        )}

        {/* Contenido del Ticket */}
        {!isLoading && ticket && (
          <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
            {/* Banner de Estado */}
            <div className="p-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
              <span className="text-xs font-semibold text-slate-500">Estado Oficial</span>
              {getStatusBadge(ticket.estado)}
            </div>

            {/* Mensaje Contextual de Estado */}
            {ticket.estado !== 'ACTIVO' && (
              <div
                className={`p-3.5 text-xs text-center font-medium ${
                  ticket.estado === 'CONSUMIDO'
                    ? 'bg-slate-100 text-slate-700'
                    : 'bg-amber-50 text-amber-800'
                }`}
              >
                {ticket.mensajeEstado}
              </div>
            )}

            {/* Código QR (Si está activo) */}
            {ticket.permiteDespacho && (
              <div className="p-6 text-center border-b border-slate-100 bg-slate-50/30">
                <div className="inline-block p-3 bg-white rounded-xl border border-slate-200 shadow-sm">
                  {isLoadingQr ? (
                    <div className="w-48 h-48 flex items-center justify-center">
                      <RefreshCw className="w-6 h-6 text-[#087e8b] animate-spin" />
                    </div>
                  ) : qrBlobUrl ? (
                    <img
                      src={qrBlobUrl}
                      alt={`QR Ticket ${ticket.numeroTicket}`}
                      className="w-48 h-48 object-contain mx-auto"
                    />
                  ) : (
                    <div className="w-48 h-48 flex items-center justify-center text-xs text-slate-400">
                      QR no disponible
                    </div>
                  )}
                </div>

                <p className="text-xs text-slate-500 font-medium mt-3">
                  Presenta este código al despachador de la estación.
                </p>

                {qrBlobUrl && (
                  <button
                    type="button"
                    onClick={handleDownloadQr}
                    className="mt-3 inline-flex items-center gap-1.5 px-3.5 py-1.5 text-xs font-semibold text-[#087e8b] bg-teal-50 hover:bg-teal-100 rounded-lg transition cursor-pointer"
                  >
                    <Download className="w-3.5 h-3.5" />
                    <span>Guardar Imagen QR</span>
                  </button>
                )}
              </div>
            )}

            {/* Detalles Operativos */}
            <div className="p-5 space-y-3.5 text-sm">
              <div className="flex items-center justify-between pb-3 border-b border-slate-100">
                <span className="text-xs font-semibold text-slate-500">Ticket No.</span>
                <span className="font-extrabold text-slate-950 text-base">{ticket.numeroTicket}</span>
              </div>

              <div className="flex items-start gap-3">
                <User className="w-4 h-4 text-slate-400 mt-0.5 shrink-0" />
                <div className="flex-1">
                  <div className="text-xs text-slate-500">Empleado Autorizado</div>
                  <div className="font-semibold text-slate-900">{ticket.empleado}</div>
                  {ticket.codigoEmpleado && (
                    <div className="text-xs text-slate-400">Código: {ticket.codigoEmpleado}</div>
                  )}
                </div>
              </div>

              <div className="flex items-start gap-3">
                <Car className="w-4 h-4 text-slate-400 mt-0.5 shrink-0" />
                <div className="flex-1">
                  <div className="text-xs text-slate-500">Vehículo</div>
                  <div className="font-semibold text-slate-900">{ticket.vehiculo}</div>
                  <div className="text-xs text-slate-500">
                    Placa: <strong className="text-slate-700">{ticket.placa}</strong> | Ficha:{' '}
                    <strong className="text-slate-700">{ticket.ficha}</strong>
                  </div>
                </div>
              </div>

              <div className="flex items-start gap-3">
                <Fuel className="w-4 h-4 text-[#087e8b] mt-0.5 shrink-0" />
                <div className="flex-1">
                  <div className="text-xs text-slate-500">Combustible & Cantidad</div>
                  <div className="font-semibold text-slate-900">{ticket.tipoCombustible}</div>
                  <div className="text-base font-extrabold text-[#087e8b]">
                    {ticket.cantidadAutorizada.toFixed(2)} galones
                  </div>
                </div>
              </div>

              <div className="flex items-start gap-3">
                <MapPin className="w-4 h-4 text-slate-400 mt-0.5 shrink-0" />
                <div className="flex-1">
                  <div className="text-xs text-slate-500">Estación Asignada</div>
                  <div className="font-semibold text-slate-900">{ticket.estacion}</div>
                </div>
              </div>

              <div className="flex items-start gap-3 pt-2 border-t border-slate-100">
                <Calendar className="w-4 h-4 text-amber-600 mt-0.5 shrink-0" />
                <div className="flex-1">
                  <div className="text-xs text-slate-500">Válido Hasta</div>
                  <div className="font-semibold text-slate-800">
                    {new Date(ticket.fechaExpiracion).toLocaleString('es-DO', {
                      dateStyle: 'medium',
                      timeStyle: 'short',
                    })}
                  </div>
                </div>
              </div>
            </div>

            {/* Pie de seguridad */}
            <div className="bg-slate-50 px-5 py-3 border-t border-slate-100 flex items-center justify-center gap-1.5 text-xs text-slate-500">
              <ShieldCheck className="w-4 h-4 text-teal-600" />
              <span>Verificado por La Bomba Core Security</span>
            </div>
          </div>
        )}
      </div>

      {/* Footer General */}
      <footer className="mt-8 text-center text-xs text-slate-400">
        © {new Date().getFullYear()} La Bomba — Sistema de Gestión y Suministro de Combustible.
      </footer>
    </div>
  );
};
