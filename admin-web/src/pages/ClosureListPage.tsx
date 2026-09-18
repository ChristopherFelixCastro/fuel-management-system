import React, { useState, useEffect } from 'react';
import { ClosureDto, ClosurePreviewDto } from '../types';
import { getClosures, getClosurePreview, createClosure, approveClosure, rejectClosure, getClosurePdfUrl } from '../services/api';
import { Calendar, CheckCircle2, XCircle, FileText, Plus, RefreshCw, AlertTriangle, ShieldCheck } from 'lucide-react';

export const ClosureListPage: React.FC = () => {
  const [closures, setClosures] = useState<ClosureDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [filterState, setFilterState] = useState<string>('');
  const [showCreateModal, setShowCreateModal] = useState(false);

  // Form state
  const [tanqueId, setTanqueId] = useState('00000000-0000-0000-0000-000000000001');
  const [fecha, setFecha] = useState(new Date().toISOString().split('T')[0]);
  const [preview, setPreview] = useState<ClosurePreviewDto | null>(null);
  const [stockFisico, setStockFisico] = useState<string>('');
  const [motivoDiferencia, setMotivoDiferencia] = useState('');
  const [observaciones, setObservaciones] = useState('');
  const [previewLoading, setPreviewLoading] = useState(false);
  const [rejectingId, setRejectingId] = useState<string | null>(null);
  const [rejectMotivo, setRejectMotivo] = useState('');

  const loadClosures = async () => {
    setLoading(true);
    try {
      const data = await getClosures(1, 20, filterState);
      setClosures(data.items);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadClosures();
  }, [filterState]);

  const handlePreview = async () => {
    if (!tanqueId || !fecha) return;
    setPreviewLoading(true);
    try {
      const p = await getClosurePreview(tanqueId, fecha);
      setPreview(p);
      setStockFisico(p.stockTeoricoFinal.toString());
    } catch (err) {
      alert('Error calculando vista previa: ' + (err as Error).message);
    } finally {
      setPreviewLoading(false);
    }
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await createClosure({
        tanqueId,
        fechaCierre: fecha,
        stockFisicoFinal: parseFloat(stockFisico),
        motivoDiferencia: motivoDiferencia || undefined,
        observaciones: observaciones || undefined,
      });
      setShowCreateModal(false);
      loadClosures();
    } catch (err) {
      alert('Error creando cierre: ' + (err as Error).message);
    }
  };

  const handleApprove = async (id: string) => {
    if (!confirm('¿Está seguro de aprobar este cierre diario? Esta acción actualizará los saldos de inventario oficial.')) return;
    try {
      await approveClosure(id);
      loadClosures();
    } catch (err) {
      alert('Error aprobando cierre: ' + (err as Error).message);
    }
  };

  const handleReject = async (id: string) => {
    if (!rejectMotivo) {
      alert('Por favor ingrese el motivo del rechazo.');
      return;
    }
    try {
      await rejectClosure(id, rejectMotivo);
      setRejectingId(null);
      setRejectMotivo('');
      loadClosures();
    } catch (err) {
      alert('Error rechazando cierre: ' + (err as Error).message);
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="page-header">
        <div>
          <h1 className="page-title">Cierres Diarios de Tanque</h1>
          <p className="page-subtitle">Cálculo, revisión y aprobación de balancete de inventario físico vs teórico</p>
        </div>
        <div style={{ display: 'flex', gap: '12px' }}>
          <button className="btn btn-secondary" onClick={loadClosures}>
            <RefreshCw size={16} /> Actualizar
          </button>
          <button className="btn btn-primary" onClick={() => setShowCreateModal(true)}>
            <Plus size={16} /> Nuevo Cierre
          </button>
        </div>
      </div>

      {/* Filters & Stats */}
      <div className="card" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div style={{ display: 'flex', gap: '12px', alignItems: 'center' }}>
          <span className="form-label">Filtrar por estado:</span>
          <select className="form-select" value={filterState} onChange={(e) => setFilterState(e.target.value)}>
            <option value="">Todos los estados</option>
            <option value="PENDIENTE_APROBACION">Pendiente de Aprobación</option>
            <option value="APROBADO">Aprobados</option>
            <option value="RECHAZADO">Rechazados</option>
          </select>
        </div>
        <div style={{ display: 'flex', gap: '16px' }}>
          <span style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
            Total cierres: <strong style={{ color: '#FFF' }}>{closures.length}</strong>
          </span>
        </div>
      </div>

      {/* Table */}
      <div className="card" style={{ padding: 0 }}>
        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>Fecha Cierre</th>
                <th>Tanque / Estación</th>
                <th>Stock Inicial</th>
                <th>Movimientos (Neto)</th>
                <th>Stock Teórico</th>
                <th>Stock Físico</th>
                <th>Diferencia</th>
                <th>Estado</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={9} style={{ textAlign: 'center', padding: '32px' }}>Cargando cierres...</td>
                </tr>
              ) : closures.length === 0 ? (
                <tr>
                  <td colSpan={9} style={{ textAlign: 'center', padding: '32px', color: 'var(--text-muted)' }}>
                    No se encontraron registros de cierres diarios.
                  </td>
                </tr>
              ) : (
                closures.map((c) => (
                  <tr key={c.id}>
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', fontWeight: 600 }}>
                        <Calendar size={14} color="#60A5FA" />
                        {c.fechaCierre}
                      </div>
                    </td>
                    <td>
                      <div style={{ fontWeight: 600 }}>{c.tanqueNombre || c.tanqueCodigo}</div>
                      <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{c.estacionNombre} ({c.combustibleNombre})</div>
                    </td>
                    <td>{c.stockInicial?.toLocaleString('es-ES', { minimumFractionDigits: 2 })} L</td>
                    <td>
                      <div style={{ fontSize: '0.85rem' }}>
                        <span style={{ color: '#10B981' }}>+{(c.totalRecepciones + c.totalTransferenciasEntrada + c.totalAjustesPositivos).toFixed(2)}</span> /{' '}
                        <span style={{ color: '#EF4444' }}>-{(c.totalDespachos + c.totalTransferenciasSalida + c.totalAjustesNegativos).toFixed(2)}</span>
                      </div>
                    </td>
                    <td><strong>{c.stockTeoricoFinal?.toLocaleString('es-ES', { minimumFractionDigits: 2 })} L</strong></td>
                    <td><strong>{c.stockFisicoFinal?.toLocaleString('es-ES', { minimumFractionDigits: 2 })} L</strong></td>
                    <td>
                      <span style={{ color: Math.abs(c.diferencia) > 0.01 ? '#EF4444' : '#10B981', fontWeight: 700 }}>
                        {c.diferencia > 0 ? `+${c.diferencia.toFixed(2)}` : c.diferencia.toFixed(2)} L
                      </span>
                    </td>
                    <td>
                      <span className={`status-pill ${c.estado}`}>
                        {c.estado === 'PENDIENTE_APROBACION' ? 'PENDIENTE' : c.estado}
                      </span>
                    </td>
                    <td>
                      <div style={{ display: 'flex', gap: '8px' }}>
                        <a href={getClosurePdfUrl(c.id)} target="_blank" rel="noreferrer" className="btn btn-secondary" style={{ padding: '6px 10px', fontSize: '0.8rem' }}>
                          <FileText size={14} /> PDF
                        </a>
                        {c.estado === 'PENDIENTE_APROBACION' && (
                          <>
                            <button className="btn btn-success" style={{ padding: '6px 10px', fontSize: '0.8rem' }} onClick={() => handleApprove(c.id)}>
                              <CheckCircle2 size={14} /> Aprobar
                            </button>
                            <button className="btn btn-danger" style={{ padding: '6px 10px', fontSize: '0.8rem' }} onClick={() => setRejectingId(c.id)}>
                              <XCircle size={14} /> Rechazar
                            </button>
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Create Modal */}
      {showCreateModal && (
        <div style={{
          position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
          background: 'rgba(0,0,0,0.75)', backdropFilter: 'blur(8px)',
          display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000
        }}>
          <div className="card" style={{ width: '580px', maxWidth: '90%', maxHeight: '90vh', overflowY: 'auto' }}>
            <h2 style={{ fontSize: '1.25rem', marginBottom: '16px', display: 'flex', alignItems: 'center', gap: '8px' }}>
              <ShieldCheck color="#3B82F6" /> Registrar Nuevo Cierre Diario
            </h2>
            <form onSubmit={handleCreate} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
                <div className="form-group">
                  <label className="form-label">ID Tanque</label>
                  <input className="form-input" value={tanqueId} onChange={(e) => setTanqueId(e.target.value)} required />
                </div>
                <div className="form-group">
                  <label className="form-label">Fecha de Cierre</label>
                  <input type="date" className="form-input" value={fecha} onChange={(e) => setFecha(e.target.value)} required />
                </div>
              </div>

              <button type="button" className="btn btn-secondary" onClick={handlePreview} disabled={previewLoading}>
                {previewLoading ? 'Calculando...' : '🧮 Calcular Balancete Teórico'}
              </button>

              {preview && (
                <div style={{ background: 'rgba(15, 23, 42, 0.8)', padding: '16px', borderRadius: '12px', border: '1px solid var(--border-color)', fontSize: '0.85rem' }}>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px' }}>
                    <div>Stock Inicial: <strong>{preview.stockInicial.toFixed(2)} L</strong></div>
                    <div>Recepciones: <strong style={{ color: '#10B981' }}>+{preview.totalRecepciones.toFixed(2)} L</strong></div>
                    <div>Despachos: <strong style={{ color: '#EF4444' }}>-{preview.totalDespachos.toFixed(2)} L</strong></div>
                    <div>Ajustes Positivos: <strong style={{ color: '#10B981' }}>+{preview.totalAjustesPositivos.toFixed(2)} L</strong></div>
                    <div>Ajustes Negativos: <strong style={{ color: '#EF4444' }}>-{preview.totalAjustesNegativos.toFixed(2)} L</strong></div>
                    <div style={{ gridColumn: 'span 2', marginTop: '8px', paddingTop: '8px', borderTop: '1px solid var(--border-color)', fontSize: '0.95rem' }}>
                      Stock Teórico Calculado: <strong style={{ color: '#60A5FA' }}>{preview.stockTeoricoFinal.toFixed(2)} L</strong>
                    </div>
                  </div>
                </div>
              )}

              <div className="form-group">
                <label className="form-label">Stock Físico Medido (Litros por Varillaje)</label>
                <input type="number" step="0.01" className="form-input" value={stockFisico} onChange={(e) => setStockFisico(e.target.value)} required />
              </div>

              <div className="form-group">
                <label className="form-label">Motivo de Diferencia (si aplica)</label>
                <input className="form-input" placeholder="Ej: Merma por evaporación o calibración de bomba" value={motivoDiferencia} onChange={(e) => setMotivoDiferencia(e.target.value)} />
              </div>

              <div className="form-group">
                <label className="form-label">Observaciones</label>
                <textarea className="form-input" rows={2} value={observaciones} onChange={(e) => setObservaciones(e.target.value)} />
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px', marginTop: '12px' }}>
                <button type="button" className="btn btn-secondary" onClick={() => setShowCreateModal(false)}>Cancelar</button>
                <button type="submit" className="btn btn-primary">Guardar y Enviar a Revisión</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Reject Modal */}
      {rejectingId && (
        <div style={{
          position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
          background: 'rgba(0,0,0,0.75)', backdropFilter: 'blur(8px)',
          display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000
        }}>
          <div className="card" style={{ width: '450px' }}>
            <h3 style={{ marginBottom: '12px', display: 'flex', alignItems: 'center', gap: '8px', color: '#EF4444' }}>
              <AlertTriangle /> Rechazar Cierre Diario
            </h3>
            <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '16px' }}>
              Indique la razón por la que rechaza este balancete de cierre. El operador deberá corregir la medición física o revisar vales.
            </p>
            <div className="form-group" style={{ marginBottom: '16px' }}>
              <label className="form-label">Motivo de rechazo</label>
              <textarea className="form-input" rows={3} value={rejectMotivo} onChange={(e) => setRejectMotivo(e.target.value)} required />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px' }}>
              <button className="btn btn-secondary" onClick={() => setRejectingId(null)}>Cancelar</button>
              <button className="btn btn-danger" onClick={() => handleReject(rejectingId)}>Confirmar Rechazo</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
