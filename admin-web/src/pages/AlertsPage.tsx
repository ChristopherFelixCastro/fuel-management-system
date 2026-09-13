import React, { useState, useEffect } from 'react';
import { AlertDto } from '../types';
import { getAlerts, acknowledgeAlert } from '../services/api';
import { AlertTriangle, CheckCircle, Bell, RefreshCw } from 'lucide-react';

export const AlertsPage: React.FC = () => {
  const [alerts, setAlerts] = useState<AlertDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [filterState, setFilterState] = useState<string>('');

  const loadAlerts = async () => {
    setLoading(true);
    try {
      const data = await getAlerts(1, 30, filterState);
      setAlerts(data.items);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadAlerts();
  }, [filterState]);

  const handleAcknowledge = async (id: string) => {
    try {
      await acknowledgeAlert(id, 'Reconocido desde consola administrativa');
      loadAlerts();
    } catch (err) {
      alert('Error reconociendo alerta: ' + (err as Error).message);
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="page-header">
        <div>
          <h1 className="page-title">Alertas Operativas del Sistema</h1>
          <p className="page-subtitle">Monitoreo automático de stock bajo, discrepancias de inventario y fallas de integración</p>
        </div>
        <button className="btn btn-secondary" onClick={loadAlerts}>
          <RefreshCw size={16} /> Actualizar
        </button>
      </div>

      {/* Stats */}
      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-icon" style={{ background: 'rgba(239, 68, 68, 0.15)', color: '#F87171' }}>
            <AlertTriangle />
          </div>
          <div>
            <div className="stat-val">{alerts.filter(a => a.severidad === 'CRITICA' && a.estado === 'ACTIVA').length}</div>
            <div className="stat-lbl">Críticas Activas</div>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon" style={{ background: 'rgba(245, 158, 11, 0.15)', color: '#FBBF24' }}>
            <Bell />
          </div>
          <div>
            <div className="stat-val">{alerts.filter(a => a.estado === 'ACTIVA').length}</div>
            <div className="stat-lbl">Total Activas</div>
          </div>
        </div>
        <div className="stat-card">
          <div className="stat-icon" style={{ background: 'rgba(16, 185, 129, 0.15)', color: '#34D399' }}>
            <CheckCircle />
          </div>
          <div>
            <div className="stat-val">{alerts.filter(a => a.estado === 'RECONOCIDA').length}</div>
            <div className="stat-lbl">Reconocidas</div>
          </div>
        </div>
      </div>

      {/* Filter */}
      <div className="card" style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
        <span className="form-label">Filtrar por estado:</span>
        <select className="form-select" value={filterState} onChange={(e) => setFilterState(e.target.value)}>
          <option value="">Todas las alertas</option>
          <option value="ACTIVA">Solo Activas</option>
          <option value="RECONOCIDA">Reconocidas</option>
        </select>
      </div>

      {/* Alerts List */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
        {loading ? (
          <div className="card" style={{ textAlign: 'center', padding: '32px' }}>Cargando alertas...</div>
        ) : alerts.length === 0 ? (
          <div className="card" style={{ textAlign: 'center', padding: '32px', color: 'var(--text-muted)' }}>
            No hay alertas registradas con los criterios seleccionados.
          </div>
        ) : (
          alerts.map((alert) => (
            <div
              key={alert.id}
              className="card"
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                padding: '20px 24px',
                borderLeft: `4px solid ${alert.severidad === 'CRITICA' ? '#EF4444' : '#F59E0B'}`,
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
                <div
                  style={{
                    padding: '12px',
                    borderRadius: '12px',
                    background: alert.severidad === 'CRITICA' ? 'rgba(239, 68, 68, 0.15)' : 'rgba(245, 158, 11, 0.15)',
                    color: alert.severidad === 'CRITICA' ? '#F87171' : '#FBBF24',
                  }}
                >
                  <AlertTriangle size={24} />
                </div>
                <div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '4px' }}>
                    <span style={{ fontWeight: 700, fontSize: '1.05rem' }}>{alert.tipo}</span>
                    <span className={`status-pill ${alert.severidad}`}>{alert.severidad}</span>
                    <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{alert.modulo}</span>
                  </div>
                  <div style={{ fontSize: '0.9rem', color: 'var(--text-secondary)' }}>{alert.mensaje}</div>
                  <div style={{ fontSize: '0.78rem', color: 'var(--text-muted)', marginTop: '4px' }}>
                    Generada: {new Date(alert.fechaGeneracion).toLocaleString('es-ES')}
                    {alert.fechaReconocimiento && ` • Reconocida: ${new Date(alert.fechaReconocimiento).toLocaleString('es-ES')} por ${alert.reconocidoPor || 'usuario'}`}
                  </div>
                </div>
              </div>

              <div>
                {alert.estado === 'ACTIVA' ? (
                  <button className="btn btn-secondary" onClick={() => handleAcknowledge(alert.id)}>
                    <CheckCircle size={16} /> Reconocer
                  </button>
                ) : (
                  <span className="status-pill RECONOCIDA">RECONOCIDA</span>
                )}
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};
