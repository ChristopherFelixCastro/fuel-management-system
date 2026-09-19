import React, { useEffect, useState } from 'react';
import { Check, Eye, Filter, RefreshCw, X } from 'lucide-react';
import { AlertBanner } from '../components/common/AlertBanner';
import { LoadingSpinner } from '../components/common/LoadingSpinner';
import { useAuth } from '../context/AuthContext';
import { Closure, ClosureFilters, ClosureStatus, PaginatedResult, Tank } from '../types';
import { CatalogService, ClosureService } from '../services/api';

const statusStyle: Record<ClosureStatus, string> = {
  PENDIENTE_APROBACION: 'bg-amber-100 text-amber-800',
  APROBADO: 'bg-emerald-100 text-emerald-800',
  RECHAZADO: 'bg-rose-100 text-rose-800',
};

const statusLabel: Record<ClosureStatus, string> = {
  PENDIENTE_APROBACION: 'Pendiente',
  APROBADO: 'Aprobado',
  RECHAZADO: 'Rechazado',
};

const gal = (value: number) => `${Number(value).toLocaleString('es-DO', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} gal`;
const dateTime = (value?: string | null) => value ? new Date(value).toLocaleString('es-DO') : '—';

export const Closures: React.FC = () => {
  const { hasRole } = useAuth();
  const canReview = hasRole(['ADMINISTRADOR', 'SUPERVISOR']);
  const [filters, setFilters] = useState<ClosureFilters>({ page: 1, pageSize: 20 });
  const [result, setResult] = useState<PaginatedResult<Closure> | null>(null);
  const [tanks, setTanks] = useState<Tank[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [selected, setSelected] = useState<Closure | null>(null);
  const [isDetailLoading, setIsDetailLoading] = useState(false);
  const [rejecting, setRejecting] = useState<Closure | null>(null);
  const [reason, setReason] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [alert, setAlert] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const load = async (nextFilters = filters) => {
    setIsLoading(true);
    try {
      const response = await ClosureService.getAll(nextFilters);
      setResult(response.data);
    } catch (error: any) {
      setAlert({ type: 'error', message: error.message || 'No fue posible cargar los cierres diarios.' });
    } finally { setIsLoading(false); }
  };

  useEffect(() => { void load(); }, [filters.page]);
  useEffect(() => {
    void CatalogService.getTanks().then(response => setTanks(response.data)).catch(() => setTanks([]));
  }, []);

  const applyFilters = (event: React.FormEvent) => { event.preventDefault(); const next = { ...filters, page: 1 }; setFilters(next); void load(next); };
  const resetFilters = () => { const next = { page: 1, pageSize: filters.pageSize }; setFilters(next); void load(next); };
  const openDetail = async (id: string) => {
    setIsDetailLoading(true);
    try { const response = await ClosureService.getById(id); setSelected(response.data); }
    catch (error: any) { setAlert({ type: 'error', message: error.message || 'No fue posible consultar el detalle del cierre.' }); }
    finally { setIsDetailLoading(false); }
  };
  const approve = async (closure: Closure) => {
    if (!window.confirm(`¿Aprobar el cierre de ${closure.tankCode || 'este tanque'} del ${closure.closureDate}?`)) return;
    setIsSubmitting(true);
    try { await ClosureService.approve(closure.id); setAlert({ type: 'success', message: 'Cierre diario aprobado exitosamente.' }); setSelected(null); await load(); }
    catch (error: any) { setAlert({ type: 'error', message: error.message || 'No fue posible aprobar el cierre.' }); }
    finally { setIsSubmitting(false); }
  };
  const reject = async () => {
    if (!rejecting || !reason.trim()) return;
    setIsSubmitting(true);
    try { await ClosureService.reject(rejecting.id, reason.trim()); setAlert({ type: 'success', message: 'Cierre diario rechazado.' }); setRejecting(null); setSelected(null); setReason(''); await load(); }
    catch (error: any) { setAlert({ type: 'error', message: error.message || 'No fue posible rechazar el cierre.' }); }
    finally { setIsSubmitting(false); }
  };
  const pendingActions = (closure: Closure) => canReview && closure.status === 'PENDIENTE_APROBACION';

  return <div className="space-y-6">
    <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-white p-5 rounded-xl border border-slate-200 shadow-2xs">
      <div><h1 className="text-2xl font-bold text-[#062d4f] tracking-tight">Cierres diarios</h1><p className="text-sm text-slate-500 mt-0.5">Consulta y revisión de cierres diarios por tanque</p></div>
      <button onClick={() => void load()} className="inline-flex items-center gap-2 px-4 py-2.5 bg-[#087e8b] hover:bg-[#066570] text-white text-xs font-semibold rounded-lg shadow-sm transition self-start sm:self-auto"><RefreshCw className="w-4 h-4" />Actualizar</button>
    </div>
    {alert && <AlertBanner type={alert.type} message={alert.message} onClose={() => setAlert(null)} />}
    <form onSubmit={applyFilters} className="bg-white p-4 rounded-xl border border-slate-200 shadow-2xs grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-5 gap-3 items-end">
      <label className="text-xs font-semibold text-slate-700">Fecha desde<input type="date" value={filters.fechaDesde || ''} onChange={e => setFilters({ ...filters, fechaDesde: e.target.value || undefined })} className="mt-1 w-full py-2 px-3 border border-slate-300 rounded-lg" /></label>
      <label className="text-xs font-semibold text-slate-700">Fecha hasta<input type="date" value={filters.fechaHasta || ''} onChange={e => setFilters({ ...filters, fechaHasta: e.target.value || undefined })} className="mt-1 w-full py-2 px-3 border border-slate-300 rounded-lg" /></label>
      <label className="text-xs font-semibold text-slate-700">Tanque<select value={filters.tanqueId || ''} onChange={e => setFilters({ ...filters, tanqueId: e.target.value || undefined })} className="mt-1 w-full py-2 px-3 border border-slate-300 rounded-lg"><option value="">Todos los tanques</option>{tanks.map(tank => <option key={tank.id} value={tank.id}>{tank.code}{tank.name ? ` — ${tank.name}` : ''}</option>)}</select></label>
      <label className="text-xs font-semibold text-slate-700">Estado<select value={filters.estado || ''} onChange={e => setFilters({ ...filters, estado: (e.target.value || undefined) as ClosureStatus | undefined })} className="mt-1 w-full py-2 px-3 border border-slate-300 rounded-lg"><option value="">Todos los estados</option>{Object.entries(statusLabel).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
      <div className="flex gap-2"><button type="submit" className="inline-flex items-center gap-2 px-4 py-2 text-xs font-semibold text-white bg-[#087e8b] hover:bg-[#066570] rounded-lg"><Filter className="w-4 h-4" />Filtrar</button><button type="button" onClick={resetFilters} className="px-4 py-2 text-xs font-semibold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-lg">Limpiar</button></div>
    </form>
    <div className="bg-white rounded-xl border border-slate-200 shadow-2xs overflow-hidden">
      {isLoading ? <LoadingSpinner message="Cargando cierres..." /> : !result?.items.length ? <div className="p-8 text-center text-slate-500 text-sm">No se encontraron cierres con esos filtros.</div> : <div className="overflow-x-auto"><table className="w-full text-left text-sm"><thead className="bg-[#e8f4f5] text-[#063e5b] text-xs uppercase font-semibold"><tr><th className="py-3 px-4">Fecha</th><th className="py-3 px-4">Tanque / Estación</th><th className="py-3 px-4 text-right">Teórico</th><th className="py-3 px-4 text-right">Físico</th><th className="py-3 px-4 text-right">Diferencia</th><th className="py-3 px-4 text-center">Estado</th><th className="py-3 px-4 text-right">Acciones</th></tr></thead><tbody className="divide-y divide-slate-100">{result.items.map(closure => <tr key={closure.id} className="hover:bg-slate-50"><td className="py-3 px-4 font-medium text-slate-800">{closure.closureDate}</td><td className="py-3 px-4"><p className="font-mono font-bold text-slate-800">{closure.tankCode || '—'}</p><p className="text-xs text-slate-500">{closure.stationName || closure.tankName || '—'}</p></td><td className="py-3 px-4 text-right">{gal(closure.theoreticalFinalStock)}</td><td className="py-3 px-4 text-right">{gal(closure.physicalFinalStock)}</td><td className={`py-3 px-4 text-right font-semibold ${closure.difference === 0 ? 'text-slate-700' : closure.difference > 0 ? 'text-emerald-700' : 'text-rose-700'}`}>{gal(closure.difference)}</td><td className="py-3 px-4 text-center"><span className={`inline-flex px-2.5 py-1 rounded-full text-[10px] uppercase font-bold tracking-wide ${statusStyle[closure.status]}`}>{statusLabel[closure.status]}</span></td><td className="py-3 px-4 text-right"><button onClick={() => void openDetail(closure.id)} title="Ver detalle" className="p-1.5 text-slate-500 hover:text-[#087e8b] hover:bg-slate-100 rounded-lg"><Eye className="w-4 h-4" /></button></td></tr>)}</tbody></table></div>}
      {result && result.totalPages > 1 && <div className="flex items-center justify-between p-4 border-t text-xs text-slate-600"><span>{result.totalCount} cierre{result.totalCount === 1 ? '' : 's'} · página {result.page} de {result.totalPages}</span><div className="flex gap-2"><button disabled={result.page <= 1} onClick={() => setFilters({ ...filters, page: result.page - 1 })} className="px-3 py-1.5 rounded bg-slate-100 disabled:opacity-50">Anterior</button><button disabled={result.page >= result.totalPages} onClick={() => setFilters({ ...filters, page: result.page + 1 })} className="px-3 py-1.5 rounded bg-slate-100 disabled:opacity-50">Siguiente</button></div></div>}
    </div>
    {(selected || isDetailLoading) && <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs"><div className="bg-white rounded-xl shadow-2xl max-w-4xl w-full max-h-[90vh] overflow-y-auto"><div className="flex items-center justify-between px-6 py-4 border-b"><div><h2 className="text-lg font-bold text-[#062d4f]">Detalle del cierre</h2>{selected && <p className="text-xs text-slate-500">{selected.tankCode} · {selected.closureDate}</p>}</div><button onClick={() => setSelected(null)} className="p-1 text-slate-400 hover:text-slate-600"><X className="w-5 h-5" /></button></div>{isDetailLoading || !selected ? <LoadingSpinner message="Consultando detalle..." /> : <div className="p-6 space-y-6"><div className="flex flex-wrap gap-2"><span className={`px-2.5 py-1 rounded-full text-[10px] uppercase font-bold ${statusStyle[selected.status]}`}>{statusLabel[selected.status]}</span><span className="text-xs text-slate-500 self-center">Creado por {selected.createdBy || '—'} el {dateTime(selected.createdAt)}</span></div><div className="grid grid-cols-1 sm:grid-cols-3 gap-4"><Detail label="Tanque" value={`${selected.tankCode || '—'}${selected.tankName ? ` — ${selected.tankName}` : ''}`} /><Detail label="Estación" value={selected.stationName || '—'} /><Detail label="Revisado por" value={selected.reviewedBy ? `${selected.reviewedBy} · ${dateTime(selected.reviewedAt)}` : 'Sin revisión'} /></div><div className="grid grid-cols-2 sm:grid-cols-4 gap-3">{[['Stock inicial', selected.openingStock], ['Recepciones', selected.totalReceipts], ['Transferencias entrada', selected.totalTransfersIn], ['Transferencias salida', selected.totalTransfersOut], ['Despachos', selected.totalDispatches], ['Ajustes positivos', selected.totalPositiveAdjustments], ['Ajustes negativos', selected.totalNegativeAdjustments], ['Stock teórico final', selected.theoreticalFinalStock], ['Stock físico final', selected.physicalFinalStock], ['Diferencia', selected.difference]].map(([label, value]) => <div key={String(label)} className="p-3 rounded-lg bg-slate-50 border border-slate-100"><p className="text-[10px] uppercase font-bold tracking-wide text-slate-500">{label}</p><p className="mt-1 text-sm font-semibold text-slate-800">{gal(value as number)}</p></div>)}</div>{(selected.differenceReason || selected.rejectionReason || selected.observations) && <div className="space-y-3 text-sm"><Detail label="Motivo de diferencia" value={selected.differenceReason || '—'} /><Detail label="Motivo de rechazo" value={selected.rejectionReason || '—'} /><Detail label="Observaciones" value={selected.observations || '—'} /></div>}{pendingActions(selected) && <div className="pt-4 border-t flex justify-end gap-2"><button disabled={isSubmitting} onClick={() => setRejecting(selected)} className="px-4 py-2 text-xs font-semibold text-rose-700 bg-rose-50 hover:bg-rose-100 rounded-lg">Rechazar</button><button disabled={isSubmitting} onClick={() => void approve(selected)} className="inline-flex items-center gap-2 px-4 py-2 text-xs font-semibold text-white bg-[#087e8b] hover:bg-[#066570] rounded-lg"><Check className="w-4 h-4" />Aprobar</button></div>}</div>}</div></div>}
    {rejecting && <div className="fixed inset-0 z-[60] flex items-center justify-center p-4 bg-slate-900/60"><div className="bg-white rounded-xl shadow-2xl max-w-md w-full"><div className="flex justify-between items-center px-6 py-4 border-b"><h2 className="font-bold text-slate-900">Rechazar cierre</h2><button onClick={() => setRejecting(null)} className="text-slate-400 hover:text-slate-600"><X className="w-5 h-5" /></button></div><div className="p-6 space-y-4"><p className="text-sm text-slate-600">Indique el motivo del rechazo para {rejecting.tankCode}.</p><textarea autoFocus required rows={4} value={reason} onChange={e => setReason(e.target.value)} className="w-full text-sm p-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden" placeholder="Motivo obligatorio..." /><div className="flex justify-end gap-2"><button onClick={() => setRejecting(null)} className="px-4 py-2 text-xs font-semibold text-slate-600 bg-slate-100 rounded-lg">Cancelar</button><button disabled={!reason.trim() || isSubmitting} onClick={() => void reject()} className="px-4 py-2 text-xs font-semibold text-white bg-rose-600 hover:bg-rose-700 rounded-lg disabled:opacity-50">{isSubmitting ? 'Rechazando...' : 'Confirmar rechazo'}</button></div></div></div></div>}
  </div>;
};

const Detail: React.FC<{ label: string; value: string }> = ({ label, value }) => <div><p className="text-[10px] uppercase font-bold tracking-wide text-slate-500">{label}</p><p className="mt-1 text-sm text-slate-800 whitespace-pre-wrap">{value}</p></div>;
