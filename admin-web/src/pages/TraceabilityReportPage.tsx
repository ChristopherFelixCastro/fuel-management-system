import React, { useState, useEffect } from 'react';
import { VwMovimientosTanque } from '../types';
import { getTraceabilityReport, getExportUrl } from '../services/api';
import { FileSpreadsheet, FileText, FileCode, History } from 'lucide-react';

const getReferencia = (row: VwMovimientosTanque): string => {
  if (row.despachoId) return `DESPACHO-${row.despachoId.slice(0, 8)}`;
  if (row.recepcionId) return `RECEPCION-${row.recepcionId.slice(0, 8)}`;
  if (row.transferenciaId) return `TRANSFERENCIA-${row.transferenciaId.slice(0, 8)}`;
  if (row.ajusteId) return `AJUSTE-${row.ajusteId.slice(0, 8)}`;
  return row.observaciones || '-';
};

export const TraceabilityReportPage: React.FC = () => {
  const [data, setData] = useState<VwMovimientosTanque[]>([]);
  const [loading, setLoading] = useState(true);

  const loadData = async () => {
    setLoading(true);
    try {
      const res = await getTraceabilityReport(1, 50);
      setData(res.items);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="page-header">
        <div>
          <h1 className="page-title">Reporte de Trazabilidad Kardex</h1>
          <p className="page-subtitle">Auditoría completa de movimientos de entrada, salida y transferencias</p>
        </div>
        <div style={{ display: 'flex', gap: '10px' }}>
          <a href={getExportUrl('traceability', 'pdf')} target="_blank" rel="noreferrer" className="btn btn-secondary">
            <FileText size={16} color="#EF4444" /> Exportar PDF
          </a>
          <a href={getExportUrl('traceability', 'xlsx')} target="_blank" rel="noreferrer" className="btn btn-secondary">
            <FileSpreadsheet size={16} color="#10B981" /> Exportar XLSX
          </a>
          <a href={getExportUrl('traceability', 'csv')} target="_blank" rel="noreferrer" className="btn btn-secondary">
            <FileCode size={16} color="#3B82F6" /> Exportar CSV
          </a>
        </div>
      </div>

      <div className="card" style={{ padding: 0 }}>
        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>Fecha/Hora</th>
                <th>Tanque</th>
                <th>Combustible</th>
                <th>Tipo Movimiento</th>
                <th>Cantidad</th>
                <th>Stock Anterior</th>
                <th>Stock Resultante</th>
                <th>Documento Ref</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', padding: '32px' }}>Cargando kardex de movimientos...</td>
                </tr>
              ) : data.length === 0 ? (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', padding: '32px', color: 'var(--text-muted)' }}>
                    No hay movimientos en la trazabilidad.
                  </td>
                </tr>
              ) : (
                data.map((row) => (
                  <tr key={row.movimientoId}>
                    <td>{new Date(row.fechaMovimiento).toLocaleString('es-ES')}</td>
                    <td><strong>{row.tanqueNombre || row.tanqueCodigo}</strong></td>
                    <td><span className="status-pill warning">{row.combustibleNombre}</span></td>
                    <td>
                      <span className={`status-pill ${row.tipoMovimiento.includes('ENTRADA') || row.tipoMovimiento.includes('RECEPCION') ? 'success' : 'danger'}`}>
                        {row.tipoMovimiento}
                      </span>
                    </td>
                    <td><strong>{row.cantidad?.toLocaleString('es-ES', { minimumFractionDigits: 2 })} L</strong></td>
                    <td>{row.saldoAnterior?.toLocaleString('es-ES', { minimumFractionDigits: 2 })} L</td>
                    <td><strong style={{ color: '#60A5FA' }}>{row.saldoPosterior?.toLocaleString('es-ES', { minimumFractionDigits: 2 })} L</strong></td>
                    <td><code>{getReferencia(row)}</code></td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};