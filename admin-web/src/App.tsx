import React, { useState } from 'react';
import { ClosureListPage } from './pages/ClosureListPage';
import { AlertsPage } from './pages/AlertsPage';
import { ConsumptionReportPage } from './pages/ConsumptionReportPage';
import { InventoryReportPage } from './pages/InventoryReportPage';
import { TraceabilityReportPage } from './pages/TraceabilityReportPage';
import {
  Fuel,
  Calculator,
  Bell,
  BarChart3,
  Layers,
  History,
  UserCheck,
  ShieldAlert
} from 'lucide-react';

export const App: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'closures' | 'alerts' | 'consumption' | 'inventory' | 'traceability'>('closures');

  return (
    <div className="app-container">
      {/* Sidebar */}
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-icon">
            <Fuel size={24} />
          </div>
          <div>
            <div className="brand-title">FuelManagement</div>
            <div style={{ fontSize: '0.72rem', color: 'var(--text-muted)' }}>Cierres & Reportes</div>
          </div>
        </div>

        <nav className="nav-menu">
          <div style={{ fontSize: '0.72rem', fontWeight: 700, color: 'var(--text-muted)', padding: '0 12px 6px 12px', letterSpacing: '0.05em' }}>
            MÓDULO OPERATIVO
          </div>
          <button
            className={`nav-item ${activeTab === 'closures' ? 'active' : ''}`}
            onClick={() => setActiveTab('closures')}
          >
            <Calculator size={18} /> Cierres Diarios
          </button>
          <button
            className={`nav-item ${activeTab === 'alerts' ? 'active' : ''}`}
            onClick={() => setActiveTab('alerts')}
          >
            <Bell size={18} /> Alertas Operativas
          </button>

          <div style={{ fontSize: '0.72rem', fontWeight: 700, color: 'var(--text-muted)', padding: '16px 12px 6px 12px', letterSpacing: '0.05em' }}>
            REPORTES Y EXPORTACIÓN
          </div>
          <button
            className={`nav-item ${activeTab === 'consumption' ? 'active' : ''}`}
            onClick={() => setActiveTab('consumption')}
          >
            <BarChart3 size={18} /> Consumo Diario
          </button>
          <button
            className={`nav-item ${activeTab === 'inventory' ? 'active' : ''}`}
            onClick={() => setActiveTab('inventory')}
          >
            <Layers size={18} /> Resumen Inventario
          </button>
          <button
            className={`nav-item ${activeTab === 'traceability' ? 'active' : ''}`}
            onClick={() => setActiveTab('traceability')}
          >
            <History size={18} /> Kardex Trazabilidad
          </button>
        </nav>

        <div style={{ marginTop: 'auto', paddingTop: '16px', borderTop: '1px solid var(--border-color)', display: 'flex', alignItems: 'center', gap: '12px', padding: '12px' }}>
          <div style={{ width: '36px', height: '36px', borderRadius: '50%', background: 'var(--gradient-purple)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <UserCheck size={18} />
          </div>
          <div>
            <div style={{ fontWeight: 600, fontSize: '0.85rem' }}>admin.dev</div>
            <div style={{ fontSize: '0.75rem', color: '#60A5FA' }}>Supervisor Cierres</div>
          </div>
        </div>
      </aside>

      {/* Main Area */}
      <main className="main-content">
        {activeTab === 'closures' && <ClosureListPage />}
        {activeTab === 'alerts' && <AlertsPage />}
        {activeTab === 'consumption' && <ConsumptionReportPage />}
        {activeTab === 'inventory' && <InventoryReportPage />}
        {activeTab === 'traceability' && <TraceabilityReportPage />}
      </main>
    </div>
  );
};
