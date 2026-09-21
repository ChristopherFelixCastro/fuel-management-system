import React, { useEffect, useState } from 'react';
import {
  ClipboardCheck,
  CheckCircle2,
  XCircle,
  QrCode,
  Download,
  AlertCircle,
  Clock,
  Filter,
  RefreshCw,
  X,
} from 'lucide-react';
import { RequestService, CatalogService } from '../services/api';
import { saveDownloadedFile } from '../services/apiClient';
import { FuelRequest, Station } from '../types';
import { LoadingSpinner } from '../components/common/LoadingSpinner';
import { AlertBanner } from '../components/common/AlertBanner';

export const RequestsManagement: React.FC = () => {
  const [requests, setRequests] = useState<FuelRequest[]>([]);
  const [stations, setStations] = useState<Station[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // Filtros
  const [filterEstado, setFilterEstado] = useState<string>('');

  // Modal Aprobar
  const [approveTarget, setApproveTarget] = useState<FuelRequest | null>(null);
  const [selectedStationId, setSelectedStationId] = useState<string>('');
  const [cantidadAutorizada, setCantidadAutorizada] = useState<string>('');
  const [fechaExpiracion, setFechaExpiracion] = useState<string>('');
  const [observacionesAprobacion, setObservacionesAprobacion] = useState<string>('');
  const [isApproving, setIsApproving] = useState<boolean>(false);
  const [approveError, setApproveError] = useState<string | null>(null);

  // Modal Rechazar
  const [rejectTarget, setRejectTarget] = useState<FuelRequest | null>(null);
  const [motivoRechazo, setMotivoRechazo] = useState<string>('');
  const [isRejecting, setIsRejecting] = useState<boolean>(false);
  const [rejectError, setRejectError] = useState<string | null>(null);

  // Modal QR
  const [selectedTicketRequest, setSelectedTicketRequest] = useState<FuelRequest | null>(null);
  const [qrBlobUrl, setQrBlobUrl] = useState<string | null>(null);
  const [qrFileResponse, setQrFileResponse] = useState<any>(null);
  const [isLoadingQr, setIsLoadingQr] = useState<boolean>(false);
  const [qrError, setQrError] = useState<string | null>(null);

  const loadData = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [requestsRes, stationsRes] = await Promise.all([
        RequestService.getAll({
          estado: filterEstado || undefined,
          pageSize: 100,
        }),
        CatalogService.getStations(),
      ]);

      setRequests(requestsRes.data.items || []);
      setStations((stationsRes.data || []).filter((s: Station) => s.isActive));
    } catch (err: any) {
      setError(err.message || 'Error al cargar las solicitudes para revisión.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filterEstado]);

  // Apertura modal Aprobar
  const handleOpenApprove = (req: FuelRequest) => {
    setApproveTarget(req);
    setSelectedStationId(stations.length > 0 ? stations[0].id : '');
    setCantidadAutorizada(String(req.cantidadSolicitada));

    // Fecha expiración por defecto: 3 días a futuro
    const defaultDate = new Date();
    defaultDate.setDate(defaultDate.getDate() + 3);
    setFechaExpiracion(defaultDate.toISOString().split('T')[0]);

    setObservacionesAprobacion('');
    setApproveError(null);
  };

  const handleApproveSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!approveTarget) return;

    if (!selectedStationId) {
      setApproveError('Debe seleccionar una estación de combustible autorizada.');
      return;
    }

    const cant = Number(cantidadAutorizada);
    if (isNaN(cant) || cant <= 0) {
      setApproveError('La cantidad autorizada debe ser mayor a cero.');
      return;
    }

    if (!fechaExpiracion) {
      setApproveError('Debe indicar una fecha de expiración para el ticket.');
      return;
    }

    const expDate = new Date(fechaExpiracion);
    if (expDate <= new Date()) {
      setApproveError('La fecha de expiración debe ser una fecha futura.');
      return;
    }

    setIsApproving(true);
    setApproveError(null);

    try {
      await RequestService.approve(approveTarget.id, {
        estacionId: selectedStationId,
        cantidadAutorizada: cant,
        fechaExpiracion: expDate.toISOString(),
        observaciones: observacionesAprobacion.trim() || undefined,
      });

      setSuccessMessage(`Solicitud aprobada con éxito. Se ha emitido el ticket de combustible.`);
      setApproveTarget(null);
      await loadData();
    } catch (err: any) {
      setApproveError(err.message || 'Error al aprobar la solicitud.');
    } finally {
      setIsApproving(false);
    }
  };

  // Apertura modal Rechazar
  const handleOpenReject = (req: FuelRequest) => {
    setRejectTarget(req);
    setMotivoRechazo('');
    setRejectError(null);
  };

  const handleRejectSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!rejectTarget) return;

    if (!motivoRechazo || motivoRechazo.trim().length < 3) {
      setRejectError('Debe especificar un motivo válido de al menos 3 caracteres.');
      return;
    }

    setIsRejecting(true);
    setRejectError(null);

    try {
      await RequestService.reject(rejectTarget.id, motivoRechazo.trim());
      setSuccessMessage('Solicitud rechazada.');
      setRejectTarget(null);
      await loadData();
    } catch (err: any) {
      setRejectError(err.message || 'Error al rechazar la solicitud.');
    } finally {
      setIsRejecting(false);
    }
  };

  // Ver QR
  const handleOpenQr = async (req: FuelRequest) => {
    setSelectedTicketRequest(req);
    setIsLoadingQr(true);
    setQrError(null);
    setQrBlobUrl(null);
    setQrFileResponse(null);

    try {
      let ticketId = req.ticketId;
      if (!ticketId) {
        const detail = await RequestService.getById(req.id);
        ticketId = detail.data.ticketId;
        if (detail.data.numeroTicket) {
          setSelectedTicketRequest(detail.data);
        }
      }

      if (!ticketId) {
        throw new Error('La solicitud está aprobada pero el ticket oficial aún no ha sido emitido.');
      }

      const fileRes = await RequestService.getTicketQr(ticketId);
      setQrFileResponse(fileRes);
      const url = URL.createObjectURL(fileRes.blob);
      setQrBlobUrl(url);
    } catch (err: any) {
      setQrError(err.message || 'No fue posible cargar el código QR del ticket.');
    } finally {
      setIsLoadingQr(false);
    }
  };

  const handleCloseQrModal = () => {
    if (qrBlobUrl) {
      URL.revokeObjectURL(qrBlobUrl);
    }
    setQrBlobUrl(null);
    setSelectedTicketRequest(null);
    setQrError(null);
  };

  const handleDownloadQr = () => {
    if (qrFileResponse) {
      saveDownloadedFile(qrFileResponse);
    }
  };

  const pendingCount = requests.filter((r) => r.estado === 'PENDIENTE').length;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-white p-6 rounded-xl border border-slate-200 shadow-sm">
        <div>
          <div className="flex items-center gap-2.5">
            <div className="p-2 bg-blue-50 text-blue-600 rounded-lg">
              <ClipboardCheck className="w-6 h-6" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-slate-950">Gestión de Solicitudes</h1>
              <p className="text-xs text-slate-500 mt-0.5">
                Bandeja de revisión, aprobación y emisión de tickets digitales de combustible
              </p>
            </div>
          </div>
        </div>

        <button
          onClick={() => loadData()}
          disabled={isLoading}
          className="inline-flex items-center gap-2 px-3.5 py-2 text-xs font-semibold text-slate-700 bg-slate-100 hover:bg-slate-200 rounded-lg transition disabled:opacity-50"
        >
          <RefreshCw className={`w-3.5 h-3.5 ${isLoading ? 'animate-spin text-[#087e8b]' : ''}`} />
          <span>Actualizar</span>
        </button>
      </div>

      {/* Alertas */}
      {error && (
        <AlertBanner
          type="error"
          title="Error de comunicación"
          message={error}
          onClose={() => setError(null)}
        />
      )}

      {successMessage && (
        <AlertBanner
          type="success"
          title="Acción completada"
          message={successMessage}
          onClose={() => setSuccessMessage(null)}
        />
      )}

      {/* Filtros */}
      <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <Filter className="w-4 h-4 text-slate-400" />
          <span className="text-xs font-semibold text-slate-700">Filtrar por Estado:</span>
          <select
            value={filterEstado}
            onChange={(e) => setFilterEstado(e.target.value)}
            className="text-xs py-1.5 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-none"
          >
            <option value="">Todos los Estados</option>
            <option value="PENDIENTE">PENDIENTE ({pendingCount})</option>
            <option value="APROBADA">APROBADA</option>
            <option value="RECHAZADA">RECHAZADA</option>
            <option value="CANCELADA">CANCELADA</option>
          </select>
        </div>

        {pendingCount > 0 && (
          <div className="inline-flex items-center gap-2 px-3 py-1 bg-amber-50 text-amber-700 text-xs font-semibold rounded-lg border border-amber-200">
            <Clock className="w-3.5 h-3.5" />
            <span>{pendingCount} solicitud(es) pendiente(s) de revisión</span>
          </div>
        )}
      </div>

      {/* Tabla de Solicitudes */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        {isLoading ? (
          <div className="py-12">
            <LoadingSpinner message="Consultando solicitudes de combustible..." />
          </div>
        ) : requests.length === 0 ? (
          <div className="py-16 text-center text-slate-400 text-xs">
            No se encontraron solicitudes registradas para el filtro seleccionado.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-[#e8f4f5] text-[#063e5b] text-xs uppercase font-semibold">
                <tr>
                  <th className="py-3 px-4">Fecha</th>
                  <th className="py-3 px-4">Empleado / Solicitante</th>
                  <th className="py-3 px-4">Vehículo</th>
                  <th className="py-3 px-4">Departamento</th>
                  <th className="py-3 px-4 text-right">Cant. Solicitada</th>
                  <th className="py-3 px-4 text-center">Estado</th>
                  <th className="py-3 px-4 text-center">Ticket</th>
                  <th className="py-3 px-4 text-right whitespace-nowrap">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {requests.map((req) => (
                  <tr key={req.id} className="hover:bg-slate-50 transition">
                    <td className="py-3 px-4 font-mono text-xs text-slate-600">
                      {new Date(req.fechaSolicitud).toLocaleDateString()}
                    </td>
                    <td className="py-3 px-4">
                      <div className="font-semibold text-slate-800 text-xs">
                        {req.empleadoNombre || 'Empleado'}
                      </div>
                      {req.observaciones && (
                        <div className="text-[11px] text-slate-400 truncate max-w-xs" title={req.observaciones}>
                          Obs: {req.observaciones}
                        </div>
                      )}
                    </td>
                    <td className="py-3 px-4">
                      <div className="font-semibold text-slate-800 text-xs">
                        {req.vehiculoPlaca || 'Sin Placa'}
                      </div>
                      <div className="text-[11px] text-slate-400">
                        {req.tipoCombustible || 'Combustible'}
                      </div>
                    </td>
                    <td className="py-3 px-4 text-xs text-slate-600">
                      {req.departamentoNombre || 'N/A'}
                    </td>
                    <td className="py-3 px-4 text-right font-mono font-bold text-slate-900 text-xs">
                      {req.cantidadSolicitada} gal
                      {req.cantidadAutorizada && req.cantidadAutorizada !== req.cantidadSolicitada && (
                        <span className="block text-[10px] text-emerald-600 font-medium">
                          Aut: {req.cantidadAutorizada} gal
                        </span>
                      )}
                    </td>
                    <td className="py-3 px-4 text-center">
                      <span
                        className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold border ${
                          req.estado === 'PENDIENTE'
                            ? 'bg-amber-50 text-amber-700 border-amber-200'
                            : req.estado === 'APROBADA'
                            ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                            : req.estado === 'RECHAZADA'
                            ? 'bg-rose-50 text-rose-700 border-rose-200'
                            : 'bg-slate-100 text-slate-700 border-slate-200'
                        }`}
                      >
                        {req.estado}
                      </span>
                      {req.motivoRechazo && (
                        <span
                          className="block text-[10px] text-rose-600 mt-1 max-w-xs truncate mx-auto"
                          title={req.motivoRechazo}
                        >
                          Motivo: {req.motivoRechazo}
                        </span>
                      )}
                    </td>
                    <td className="py-3 px-4 text-center">
                      {req.ticketId ? (
                        <button
                          onClick={() => handleOpenQr(req)}
                          className="inline-flex items-center gap-1.5 px-2.5 py-1 bg-teal-50 hover:bg-teal-100 text-[#087e8b] text-xs font-semibold rounded-lg border border-teal-200 transition"
                        >
                          <QrCode className="w-3.5 h-3.5" />
                          <span>{req.numeroTicket || 'Ver QR'}</span>
                        </button>
                      ) : (
                        <span className="text-[11px] text-slate-400">—</span>
                      )}
                    </td>
                    <td className="py-3 px-4 text-right whitespace-nowrap">
                      {req.estado === 'PENDIENTE' ? (
                        <div className="inline-flex items-center justify-end gap-2">
                          <button
                            type="button"
                            onClick={() => handleOpenApprove(req)}
                            className="inline-flex items-center gap-1 px-3 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-semibold rounded-lg shadow-sm transition cursor-pointer"
                            title="Aprobar y emitir ticket"
                          >
                            <CheckCircle2 className="w-3.5 h-3.5" />
                            <span>Aprobar</span>
                          </button>
                          <button
                            type="button"
                            onClick={() => handleOpenReject(req)}
                            className="inline-flex items-center gap-1 px-3 py-1.5 bg-rose-600 hover:bg-rose-700 text-white text-xs font-semibold rounded-lg shadow-sm transition cursor-pointer"
                            title="Rechazar solicitud"
                          >
                            <XCircle className="w-3.5 h-3.5" />
                            <span>Rechazar</span>
                          </button>
                        </div>
                      ) : (
                        <span className="text-xs text-slate-400 font-medium">Atendida</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Modal Aprobar Solicitud */}
      {approveTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm">
          <div className="bg-white rounded-xl shadow-2xl max-w-md w-full border border-slate-200 overflow-hidden">
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100">
              <div className="flex items-center gap-2.5">
                <div className="p-2 bg-emerald-50 text-emerald-600 rounded-lg">
                  <CheckCircle2 className="w-5 h-5" />
                </div>
                <h3 className="text-base font-bold text-slate-900">Aprobar Solicitud de Combustible</h3>
              </div>
              <button
                onClick={() => setApproveTarget(null)}
                className="text-slate-400 hover:text-slate-600 transition p-1 rounded-md"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleApproveSubmit} className="p-6 space-y-4">
              {approveError && (
                <div className="p-3 bg-rose-50 border border-rose-200 rounded-lg text-xs text-rose-700 flex items-start gap-2">
                  <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
                  <span>{approveError}</span>
                </div>
              )}

              <div className="p-3 bg-slate-50 border border-slate-200 rounded-lg text-xs space-y-1 text-slate-600">
                <p>
                  <strong>Empleado:</strong> {approveTarget.empleadoNombre}
                </p>
                <p>
                  <strong>Vehículo:</strong> {approveTarget.vehiculoPlaca} ({approveTarget.tipoCombustible})
                </p>
                <p>
                  <strong>Cantidad Solicitada:</strong> {approveTarget.cantidadSolicitada} galones
                </p>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Estación de Servicio Autorizada *
                </label>
                <select
                  required
                  value={selectedStationId}
                  onChange={(e) => setSelectedStationId(e.target.value)}
                  className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-emerald-500 focus:outline-none"
                >
                  <option value="">-- Seleccione Estación --</option>
                  {stations.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.name} ({s.code})
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Cantidad Autorizada (Galones) *
                </label>
                <input
                  type="number"
                  step="0.01"
                  min="0.01"
                  required
                  value={cantidadAutorizada}
                  onChange={(e) => setCantidadAutorizada(e.target.value)}
                  className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-emerald-500 focus:outline-none font-mono"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Fecha de Expiración del Ticket *
                </label>
                <input
                  type="date"
                  required
                  value={fechaExpiracion}
                  onChange={(e) => setFechaExpiracion(e.target.value)}
                  className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-emerald-500 focus:outline-none font-mono"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Observaciones de Aprobación
                </label>
                <textarea
                  rows={2}
                  maxLength={500}
                  placeholder="Instrucciones u observaciones opcionales..."
                  value={observacionesAprobacion}
                  onChange={(e) => setObservacionesAprobacion(e.target.value)}
                  className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-emerald-500 focus:outline-none"
                />
              </div>

              <div className="pt-3 border-t border-slate-100 flex items-center justify-between gap-2">
                <button
                  type="button"
                  onClick={() => {
                    const target = approveTarget;
                    setApproveTarget(null);
                    handleOpenReject(target);
                  }}
                  className="text-xs text-rose-600 hover:text-rose-700 font-semibold underline cursor-pointer"
                >
                  ¿Rechazar esta solicitud?
                </button>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => setApproveTarget(null)}
                    disabled={isApproving}
                    className="px-4 py-2 text-xs font-semibold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-lg transition"
                  >
                    Cancelar
                  </button>
                  <button
                    type="submit"
                    disabled={isApproving}
                    className="px-4 py-2 text-xs font-semibold text-white bg-emerald-600 hover:bg-emerald-700 rounded-lg shadow-sm transition disabled:opacity-50"
                  >
                    {isApproving ? 'Emitiendo Ticket...' : 'Confirmar Aprobación'}
                  </button>
                </div>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal Rechazar Solicitud */}
      {rejectTarget && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm">
          <div className="bg-white rounded-xl shadow-2xl max-w-md w-full border border-slate-200 overflow-hidden">
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100">
              <div className="flex items-center gap-2.5">
                <div className="p-2 bg-rose-50 text-rose-600 rounded-lg">
                  <XCircle className="w-5 h-5" />
                </div>
                <h3 className="text-base font-bold text-slate-900">Rechazar Solicitud de Combustible</h3>
              </div>
              <button
                onClick={() => setRejectTarget(null)}
                className="text-slate-400 hover:text-slate-600 transition p-1 rounded-md"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleRejectSubmit} className="p-6 space-y-4">
              {rejectError && (
                <div className="p-3 bg-rose-50 border border-rose-200 rounded-lg text-xs text-rose-700 flex items-start gap-2">
                  <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
                  <span>{rejectError}</span>
                </div>
              )}

              <p className="text-xs text-slate-600 leading-relaxed">
                Indique el motivo por el cual rechaza la solicitud de{' '}
                <strong>{rejectTarget.empleadoNombre}</strong> para el vehículo{' '}
                <strong>{rejectTarget.vehiculoPlaca}</strong>.
              </p>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Motivo del Rechazo *
                </label>
                <textarea
                  required
                  minLength={3}
                  maxLength={300}
                  rows={3}
                  placeholder="Ej. Cantidad excede el límite asignado para la ruta..."
                  value={motivoRechazo}
                  onChange={(e) => setMotivoRechazo(e.target.value)}
                  className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-rose-500 focus:outline-none"
                />
              </div>

              <div className="pt-3 border-t border-slate-100 flex items-center justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setRejectTarget(null)}
                  disabled={isRejecting}
                  className="px-4 py-2 text-xs font-semibold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-lg transition"
                >
                  Volver
                </button>
                <button
                  type="submit"
                  disabled={isRejecting}
                  className="px-4 py-2 text-xs font-semibold text-white bg-rose-600 hover:bg-rose-700 rounded-lg shadow-sm transition disabled:opacity-50"
                >
                  {isRejecting ? 'Rechazando...' : 'Confirmar Rechazo'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal Ver QR */}
      {selectedTicketRequest && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm">
          <div className="bg-white rounded-xl shadow-2xl max-w-sm w-full border border-slate-200 overflow-hidden text-center">
            <div className="flex items-center justify-between px-5 py-4 border-b border-slate-100">
              <div className="flex items-center gap-2">
                <QrCode className="w-5 h-5 text-[#087e8b]" />
                <h3 className="text-sm font-bold text-slate-900">Ticket y QR Emitido</h3>
              </div>
              <button
                onClick={handleCloseQrModal}
                className="text-slate-400 hover:text-slate-600 transition p-1 rounded-md"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            <div className="p-6 space-y-4">
              <div className="bg-slate-50 p-3 rounded-lg border border-slate-200 text-xs text-slate-700">
                <p className="font-mono font-bold text-sm text-[#087e8b]">
                  {selectedTicketRequest.numeroTicket}
                </p>
                <p className="mt-1 text-slate-500">
                  {selectedTicketRequest.empleadoNombre} · {selectedTicketRequest.vehiculoPlaca}
                </p>
              </div>

              {isLoadingQr ? (
                <div className="py-8">
                  <LoadingSpinner message="Consultando QR..." size="md" />
                </div>
              ) : qrError ? (
                <div className="p-4 bg-rose-50 border border-rose-200 rounded-lg text-xs text-rose-700">
                  {qrError}
                </div>
              ) : qrBlobUrl ? (
                <div className="space-y-3">
                  <div className="p-4 bg-white border border-slate-200 rounded-xl inline-block shadow-sm">
                    <img
                      src={qrBlobUrl}
                      alt="Código QR"
                      className="w-48 h-48 object-contain mx-auto"
                    />
                  </div>
                  <button
                    type="button"
                    onClick={handleDownloadQr}
                    className="inline-flex items-center gap-2 px-4 py-2 bg-[#087e8b] hover:bg-[#066570] text-white text-xs font-semibold rounded-lg shadow-sm transition"
                  >
                    <Download className="w-4 h-4" />
                    <span>Descargar Imagen QR</span>
                  </button>
                </div>
              ) : null}
            </div>

            <div className="px-5 py-3 bg-slate-50 border-t border-slate-100">
              <button
                type="button"
                onClick={handleCloseQrModal}
                className="w-full py-2 text-xs font-semibold text-slate-600 bg-white border border-slate-300 rounded-lg hover:bg-slate-50 transition"
              >
                Cerrar
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
