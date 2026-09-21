import React, { useEffect, useState } from 'react';
import {
  FileText,
  Plus,
  QrCode,
  Clock,
  CheckCircle2,
  XCircle,
  AlertCircle,
  Download,
  Ban,
  RefreshCw,
  X,
} from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { RequestService, CatalogService } from '../services/api';
import { saveDownloadedFile } from '../services/apiClient';
import { FuelRequest } from '../types';
import { LoadingSpinner } from '../components/common/LoadingSpinner';
import { AlertBanner } from '../components/common/AlertBanner';
import { ConfirmModal } from '../components/common/ConfirmModal';

interface VehicleOption {
  id: string;
  placa: string;
  ficha: string;
  marca?: string | null;
  modelo?: string | null;
  capacidadTanque: number;
  tipoCombustibleId: number;
  combustibleNombre?: string | null;
  departamentoId: string;
  departamentoNombre?: string | null;
  activo: boolean;
}

export const MyRequests: React.FC = () => {
  const { user } = useAuth();

  const [requests, setRequests] = useState<FuelRequest[]>([]);
  const [vehicles, setVehicles] = useState<VehicleOption[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  // Modal Nueva Solicitud
  const [isCreateOpen, setIsCreateOpen] = useState<boolean>(false);
  const [selectedVehicleId, setSelectedVehicleId] = useState<string>('');
  const [cantidad, setCantidad] = useState<string>('');
  const [observaciones, setObservaciones] = useState<string>('');
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [formError, setFormError] = useState<string | null>(null);

  // Modal Cancelar Solicitud
  const [cancelTarget, setCancelTarget] = useState<FuelRequest | null>(null);
  const [isCanceling, setIsCanceling] = useState<boolean>(false);

  // Modal Ticket QR
  const [selectedTicketRequest, setSelectedTicketRequest] = useState<FuelRequest | null>(null);
  const [qrBlobUrl, setQrBlobUrl] = useState<string | null>(null);
  const [qrFileResponse, setQrFileResponse] = useState<any>(null);
  const [isLoadingQr, setIsLoadingQr] = useState<boolean>(false);
  const [qrError, setQrError] = useState<string | null>(null);

  const loadData = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [requestsRes, vehiclesRes] = await Promise.all([
        RequestService.getAll({ pageSize: 50 }),
        CatalogService.getVehicles(),
      ]);
      setRequests(requestsRes.data.items || []);
      setVehicles((vehiclesRes.data || []).filter((v: VehicleOption) => v.activo));
    } catch (err: any) {
      setError(err.message || 'Error al cargar las solicitudes del empleado.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleOpenCreate = () => {
    setSelectedVehicleId('');
    setCantidad('');
    setObservaciones('');
    setFormError(null);
    setIsCreateOpen(true);
  };

  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    const vehicle = vehicles.find((v) => v.id === selectedVehicleId);
    if (!vehicle) {
      setFormError('Por favor seleccione un vehículo válido.');
      return;
    }

    const cantidadNum = Number(cantidad);
    if (isNaN(cantidadNum) || cantidadNum <= 0) {
      setFormError('La cantidad solicitada debe ser un número positivo.');
      return;
    }

    if (cantidadNum > vehicle.capacidadTanque) {
      setFormError(
        `La cantidad (${cantidadNum} gal) no puede superar la capacidad del tanque (${vehicle.capacidadTanque} gal).`
      );
      return;
    }

    setIsSubmitting(true);
    try {
      await RequestService.create({
        empleadoId: user?.employeeId || '',
        vehiculoId: vehicle.id,
        departamentoId: vehicle.departamentoId,
        cantidadSolicitada: cantidadNum,
        observaciones: observaciones.trim() || undefined,
        tipoSolicitud: 'MANUAL',
      });

      setSuccessMessage('Solicitud registrada exitosamente. Quedó en estado PENDIENTE de revisión.');
      setIsCreateOpen(false);
      await loadData();
    } catch (err: any) {
      setFormError(err.message || 'No fue posible registrar la solicitud.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOpenCancel = (req: FuelRequest) => {
    setCancelTarget(req);
  };

  const handleConfirmCancel = async () => {
    if (!cancelTarget) return;
    setIsCanceling(true);
    try {
      await RequestService.cancel(cancelTarget.id);
      setSuccessMessage(`Solicitud cancelada correctamente.`);
      setCancelTarget(null);
      await loadData();
    } catch (err: any) {
      setError(err.message || 'Error al cancelar la solicitud.');
    } finally {
      setIsCanceling(false);
    }
  };

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

  const selectedVehicle = vehicles.find((v) => v.id === selectedVehicleId);

  // Estadísticas rápidas
  const totalRequests = requests.length;
  const pendingRequests = requests.filter((r) => r.estado === 'PENDIENTE').length;
  const approvedRequests = requests.filter((r) => r.estado === 'APROBADA').length;
  const rejectedRequests = requests.filter((r) => r.estado === 'RECHAZADA').length;

  return (
    <div className="space-y-6">
      {/* Header del Portal */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-white p-6 rounded-xl border border-slate-200 shadow-sm">
        <div>
          <div className="flex items-center gap-2.5">
            <div className="p-2 bg-teal-50 text-[#087e8b] rounded-lg">
              <FileText className="w-6 h-6" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-slate-950">Portal del Solicitante</h1>
              <p className="text-xs text-slate-500 mt-0.5">
                Autoservicio de combustible para {user?.fullName || user?.username}
              </p>
            </div>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => loadData()}
            disabled={isLoading}
            className="inline-flex items-center gap-2 px-3.5 py-2.5 text-xs font-semibold text-slate-700 bg-slate-100 hover:bg-slate-200 rounded-lg transition disabled:opacity-50 cursor-pointer"
            title="Actualizar estado de solicitudes"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isLoading ? 'animate-spin text-[#087e8b]' : ''}`} />
            <span>Actualizar</span>
          </button>
          <button
            type="button"
            onClick={handleOpenCreate}
            className="inline-flex items-center justify-center gap-2 px-4 py-2.5 bg-[#087e8b] hover:bg-[#066570] text-white text-sm font-semibold rounded-lg shadow-sm transition cursor-pointer"
          >
            <Plus className="w-4 h-4" />
            <span>Nueva Solicitud</span>
          </button>
        </div>
      </div>

      {/* Alertas globales */}
      {error && (
        <AlertBanner
          type="error"
          title="Error en el portal"
          message={error}
          onClose={() => setError(null)}
        />
      )}

      {successMessage && (
        <AlertBanner
          type="success"
          title="Operación exitosa"
          message={successMessage}
          onClose={() => setSuccessMessage(null)}
        />
      )}

      {/* Indicadores de Autoservicio */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex items-center gap-3">
          <div className="p-3 bg-slate-100 text-slate-700 rounded-lg">
            <FileText className="w-5 h-5" />
          </div>
          <div>
            <span className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
              Total Enviadas
            </span>
            <p className="text-xl font-bold text-slate-900">{totalRequests}</p>
          </div>
        </div>

        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex items-center gap-3">
          <div className="p-3 bg-amber-50 text-amber-600 rounded-lg">
            <Clock className="w-5 h-5" />
          </div>
          <div>
            <span className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
              Pendientes
            </span>
            <p className="text-xl font-bold text-slate-900">{pendingRequests}</p>
          </div>
        </div>

        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex items-center gap-3">
          <div className="p-3 bg-emerald-50 text-emerald-600 rounded-lg">
            <CheckCircle2 className="w-5 h-5" />
          </div>
          <div>
            <span className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
              Aprobadas / Tickets
            </span>
            <p className="text-xl font-bold text-slate-900">{approvedRequests}</p>
          </div>
        </div>

        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex items-center gap-3">
          <div className="p-3 bg-rose-50 text-rose-600 rounded-lg">
            <XCircle className="w-5 h-5" />
          </div>
          <div>
            <span className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
              Rechazadas
            </span>
            <p className="text-xl font-bold text-slate-900">{rejectedRequests}</p>
          </div>
        </div>
      </div>

      {/* Tabla de Mis Solicitudes */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="p-5 border-b border-slate-100 flex items-center justify-between">
          <div>
            <h2 className="text-base font-bold text-slate-900">Historial de Solicitudes</h2>
            <p className="text-xs text-slate-500">
              Consulte el estado de sus solicitudes, tickets emitidos y códigos QR.
            </p>
          </div>
        </div>

        {isLoading ? (
          <div className="py-12">
            <LoadingSpinner message="Cargando solicitudes del empleado..." />
          </div>
        ) : requests.length === 0 ? (
          <div className="py-16 px-4 text-center">
            <div className="w-12 h-12 bg-slate-100 text-slate-400 rounded-full flex items-center justify-center mx-auto mb-3">
              <FileText className="w-6 h-6" />
            </div>
            <h3 className="text-sm font-semibold text-slate-900">No tiene solicitudes registradas</h3>
            <p className="text-xs text-slate-500 mt-1 max-w-sm mx-auto">
              Haga clic en «Nueva Solicitud» para requerir combustible para su vehículo autorizado.
            </p>
            <button
              onClick={handleOpenCreate}
              className="mt-4 inline-flex items-center gap-2 px-3.5 py-2 bg-[#087e8b] hover:bg-[#066570] text-white text-xs font-semibold rounded-lg shadow-sm transition"
            >
              <Plus className="w-3.5 h-3.5" />
              <span>Crear mi primera solicitud</span>
            </button>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-[#e8f4f5] text-[#063e5b] text-xs uppercase font-semibold">
                <tr>
                  <th className="py-3 px-4">Fecha</th>
                  <th className="py-3 px-4">Vehículo</th>
                  <th className="py-3 px-4">Departamento</th>
                  <th className="py-3 px-4 text-right">Cantidad</th>
                  <th className="py-3 px-4 text-center">Estado</th>
                  <th className="py-3 px-4 text-center">Ticket / QR</th>
                  <th className="py-3 px-4 text-right">Acciones</th>
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
                        {req.vehiculoPlaca || 'Sin Placa'}
                      </div>
                      <div className="text-[11px] text-slate-400">
                        {req.tipoCombustible || 'Combustible'}
                      </div>
                    </td>
                    <td className="py-3 px-4 text-xs text-slate-600">
                      {req.departamentoNombre || 'N/A'}
                    </td>
                    <td className="py-3 px-4 text-right">
                      <span className="font-mono font-bold text-slate-900 text-xs">
                        {req.cantidadSolicitada} gal
                      </span>
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
                          className="block text-[10px] text-rose-600 mt-1 max-w-xs truncate"
                          title={req.motivoRechazo}
                        >
                          Motivo: {req.motivoRechazo}
                        </span>
                      )}
                    </td>
                    <td className="py-3 px-4 text-center whitespace-nowrap">
                      {req.estado === 'APROBADA' ? (
                        <button
                          type="button"
                          onClick={() => handleOpenQr(req)}
                          className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-teal-50 hover:bg-teal-100 text-[#087e8b] text-xs font-semibold rounded-lg border border-teal-200 transition shadow-xs cursor-pointer"
                          title="Ver y descargar Ticket QR"
                        >
                          <QrCode className="w-3.5 h-3.5" />
                          <span>{req.numeroTicket || 'Ver Ticket QR'}</span>
                        </button>
                      ) : req.estado === 'PENDIENTE' ? (
                        <span className="text-[11px] text-amber-600 font-medium">Pendiente de emisión</span>
                      ) : (
                        <span className="text-[11px] text-slate-400">No aplica</span>
                      )}
                    </td>
                    <td className="py-3 px-4 text-right">
                      {req.estado === 'PENDIENTE' && (
                        <button
                          onClick={() => handleOpenCancel(req)}
                          className="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium text-rose-600 hover:bg-rose-50 rounded-md transition"
                          title="Cancelar solicitud"
                        >
                          <Ban className="w-3.5 h-3.5" />
                          <span>Cancelar</span>
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Modal: Nueva Solicitud */}
      {isCreateOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm">
          <div className="bg-white rounded-xl shadow-2xl max-w-lg w-full border border-slate-200 overflow-hidden">
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100">
              <div className="flex items-center gap-2.5">
                <div className="p-2 bg-teal-50 text-[#087e8b] rounded-lg">
                  <Plus className="w-5 h-5" />
                </div>
                <h3 className="text-base font-bold text-slate-900">Nueva Solicitud de Combustible</h3>
              </div>
              <button
                onClick={() => setIsCreateOpen(false)}
                className="text-slate-400 hover:text-slate-600 transition p-1 rounded-md"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleCreateSubmit} className="p-6 space-y-4">
              {formError && (
                <div className="p-3 bg-rose-50 border border-rose-200 rounded-lg text-xs text-rose-700 flex items-start gap-2">
                  <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
                  <span>{formError}</span>
                </div>
              )}

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Vehículo Autorizado *
                </label>
                <select
                  required
                  value={selectedVehicleId}
                  onChange={(e) => setSelectedVehicleId(e.target.value)}
                  className="w-full text-xs py-2.5 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-none"
                >
                  <option value="">-- Seleccione el vehículo --</option>
                  {vehicles.map((v) => (
                    <option key={v.id} value={v.id}>
                      {v.placa} - {v.marca || ''} {v.modelo || ''} ({v.combustibleNombre || 'Combustible'}, Tanque: {v.capacidadTanque} gal) · Dept: {v.departamentoNombre || 'General'}
                    </option>
                  ))}
                </select>
              </div>

              {selectedVehicle && (
                <div className="p-3 bg-slate-50 border border-slate-200 rounded-lg space-y-1.5 text-xs text-slate-600">
                  <div className="flex justify-between">
                    <span className="text-slate-400">Tipo de Combustible:</span>
                    <span className="font-semibold text-slate-800">
                      {selectedVehicle.combustibleNombre || 'No especificado'}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-400">Capacidad Máxima del Tanque:</span>
                    <span className="font-semibold text-slate-800">
                      {selectedVehicle.capacidadTanque} galones
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-400">Departamento Asignado:</span>
                    <span className="font-semibold text-slate-800">
                      {selectedVehicle.departamentoNombre || 'General'}
                    </span>
                  </div>
                </div>
              )}

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Cantidad Solicitada (Galones) *
                </label>
                <input
                  type="number"
                  step="0.01"
                  min="0.01"
                  max={selectedVehicle ? selectedVehicle.capacidadTanque : undefined}
                  required
                  placeholder="Ej. 15.5"
                  value={cantidad}
                  onChange={(e) => setCantidad(e.target.value)}
                  className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-none font-mono"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Observaciones / Justificación
                </label>
                <textarea
                  rows={2}
                  maxLength={500}
                  placeholder="Motivo del viaje o detalle operacional..."
                  value={observaciones}
                  onChange={(e) => setObservaciones(e.target.value)}
                  className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-none"
                />
              </div>

              <div className="pt-3 border-t border-slate-100 flex items-center justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setIsCreateOpen(false)}
                  disabled={isSubmitting}
                  className="px-4 py-2 text-xs font-semibold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-lg transition"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="px-4 py-2 text-xs font-semibold text-white bg-[#087e8b] hover:bg-[#066570] rounded-lg shadow-sm transition disabled:opacity-50"
                >
                  {isSubmitting ? 'Registrando...' : 'Enviar Solicitud'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal: Ver Ticket y Código QR */}
      {selectedTicketRequest && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm">
          <div className="bg-white rounded-xl shadow-2xl max-w-sm w-full border border-slate-200 overflow-hidden text-center">
            <div className="flex items-center justify-between px-5 py-4 border-b border-slate-100">
              <div className="flex items-center gap-2">
                <QrCode className="w-5 h-5 text-[#087e8b]" />
                <h3 className="text-sm font-bold text-slate-900">Ticket Digital Emitido</h3>
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
                  {selectedTicketRequest.numeroTicket || 'Ticket Emitido'}
                </p>
                <p className="mt-1 text-slate-500">
                  {selectedTicketRequest.vehiculoPlaca} · {selectedTicketRequest.cantidadAutorizada || selectedTicketRequest.cantidadSolicitada} galones
                </p>
              </div>

              {isLoadingQr ? (
                <div className="py-8">
                  <LoadingSpinner message="Generando código QR oficial..." size="md" />
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
                      alt="Código QR del Ticket"
                      className="w-48 h-48 object-contain mx-auto"
                    />
                  </div>
                  <p className="text-[11px] text-slate-500 leading-tight">
                    Presente este código QR al operador en la estación de servicio para la validación en línea y despacho.
                  </p>
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

      {/* Modal: Confirmar Cancelación */}
      <ConfirmModal
        isOpen={Boolean(cancelTarget)}
        title="Cancelar Solicitud de Combustible"
        message={`¿Está seguro de que desea cancelar su solicitud para el vehículo ${cancelTarget?.vehiculoPlaca}? Esta acción no se puede deshacer.`}
        confirmLabel="Sí, Cancelar Solicitud"
        cancelLabel="Volver"
        isDestructive={true}
        isLoading={isCanceling}
        onConfirm={handleConfirmCancel}
        onClose={() => setCancelTarget(null)}
      />
    </div>
  );
};
