import React, { useState, useEffect } from 'react';
import { VwTanqueResumen } from '../types';
import { getInventoryReport, getExportUrl } from '../services/api';
import { FileSpreadsheet, FileText, FileCode, Layers } from 'lucide-react';

export const InventoryReportPage: React.FC = () => {
  const [data, setData] = useState<VwTanqueResumen[]>([]);
  const [loading, setLoading] = useState(true);

  const loadData = async () => {
    setLoading(true);
    try {
      const res = await getInventoryReport(1, 50);
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
          <h1 className="page-title">Reporte de Inventario de Tanques</h1>
          <p className="page-subtitle">Estado en tiempo real de capacidad, volumen actual y nivel crítico por tanque</p>
        </div>
        <div style={{ display: 'flex', gap: '10px' }}>
          <a href={getExportUrl('inventory', 'pdf')} target="_blank" rel="noreferrer" className="btn btn-secondary">
            <FileText size={16} color="#EF4444" /> Exportar PDF
          </a>
          <a href={getExportUrl('inventory', 'xlsx')} target="_blank" rel="noreferrer" className="btn btn-secondary">
            <FileSpreadsheet size={16} color="#10B981" /> Exportar XLSX
          </a>
          <a href={getExportUrl('inventory', 'csv')} target="_blank" rel="noreferrer" className="btn btn-secondary">
            <FileCode size={16} color="#3B82F6" /> Exportar CSV
          </a>
        </div>
      </div>

      <div className="card" style={{ padding: 0 }}>
        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>Tanque</th>
                <th>Código</th>
                <th>Tipo Combustible</th>
                <th>Capacidad Máxima</th>
                <th>Stock Actual</th>
                <th>% Ocupación</th>
                <th>Estado</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: '32px' }}>Cargando inventario de tanques...</td>
                </tr>
              ) : data.length === 0 ? (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: '32px', color: 'var(--text-muted)' }}>
                    No hay tanques registrados.
                  </td>
                </tr>
              ) : (
                data.map((row) => (
                  <tr key={row.id}>
                    <td><strong>{row.tanqueNombre}</strong></td>
                    <td><code>{row.codigo}</code></td>
                    <td>{row.combustibleTipo}</td>
                    <td>{row.capacidadTotal?.toLocaleString('es-ES')} L</td>
                    <td><strong style={{ color: '#60A5FA' }}>{row.stockActual?.toLocaleString('es-ES')} L</strong></td>
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <div style={{ flex: 1, height: '8px', background: 'rgba(255,255,255,0.1)', borderRadius: '4px', overflow: 'hidden' }}>
                          <div style={{ width: `${row.porcentajeOcupacion}%`, height: '100%', background: row.porcentajeOcupacion < 20 ? '#EF4444' : '#10B981' }} />
                        </div>
                        <span style={{ fontSize: '0.8rem', width: '45px' }}>{row.porcentajeOcupacion?.toFixed(1)}%</span>
                      </div>
                    </td>
                    <td>
                      <span className={`status-pill ${row.estadoStock === 'CRITICO' ? 'danger' : row.estadoStock === 'NORMAL' ? 'success' : 'warning'}`}>
                        {row.estadoStock || 'NORMAL'}
                      </span>
                    </td>
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
