import React, { useState, useEffect } from 'react';
import { VwConsumoDiario } from '../types';
import { getConsumptionReport, getExportUrl } from '../services/api';
import { Download, FileSpreadsheet, FileText, FileCode, RefreshCw } from 'lucide-react';

export const ConsumptionReportPage: React.FC = () => {
  const [data, setData] = useState<VwConsumoDiario[]>([]);
  const [loading, setLoading] = useState(true);

  const loadData = async () => {
    setLoading(true);
    try {
      const res = await getConsumptionReport(1, 50);
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
          <h1 className="page-title">Reporte de Consumo Diario</h1>
          <p className="page-subtitle">Consolidador de despachos por fecha, tanque y combustible</p>
        </div>
        <div style={{ display: 'flex', gap: '10px' }}>
          <a href={getExportUrl('consumption', 'pdf')} target="_blank" rel="noreferrer" className="btn btn-secondary">
            <FileText size={16} color="#EF4444" /> Exportar PDF
          </a>
          <a href={getExportUrl('consumption', 'xlsx')} target="_blank" rel="noreferrer" className="btn btn-secondary">
            <FileSpreadsheet size={16} color="#10B981" /> Exportar XLSX
          </a>
          <a href={getExportUrl('consumption', 'csv')} target="_blank" rel="noreferrer" className="btn btn-secondary">
            <FileCode size={16} color="#3B82F6" /> Exportar CSV
          </a>
        </div>
      </div>

      <div className="card" style={{ padding: 0 }}>
        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>Fecha</th>
                <th>Tanque</th>
                <th>Tipo Combustible</th>
                <th>Total Despachado (Litros)</th>
                <th>Nº Despachos</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={5} style={{ textAlign: 'center', padding: '32px' }}>Cargando reporte de consumo...</td>
                </tr>
              ) : data.length === 0 ? (
                <tr>
                  <td colSpan={5} style={{ textAlign: 'center', padding: '32px', color: 'var(--text-muted)' }}>
                    No hay datos de consumo registrados.
                  </td>
                </tr>
              ) : (
                data.map((row, idx) => (
                  <tr key={idx}>
                    <td><strong>{String(row.fecha).slice(0, 10)}</strong></td>
                    <td>{row.tanqueNombre || row.tanqueId}</td>
                    <td><span className="status-pill warning">{row.combustibleTipo || 'DIESEL'}</span></td>
                    <td><strong style={{ color: '#60A5FA' }}>{(row.totalDespachadoLitros || 0).toLocaleString('es-ES', { minimumFractionDigits: 2 })} L</strong></td>
                    <td>{row.cantidadDespachos} despachos</td>
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
