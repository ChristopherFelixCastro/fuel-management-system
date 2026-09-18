import React, { useEffect, useState } from 'react';
import {
  Fuel,
  Ticket,
  AlertTriangle,
  RefreshCw,
  Clock,
  TrendingUp,
  Building2,
  Filter,
} from 'lucide-react';
import { DashboardSummary, Station } from '../types';
import { DashboardService, CatalogService } from '../services/api';
import { LoadingSpinner } from '../components/common/LoadingSpinner';
import { AlertBanner } from '../components/common/AlertBanner';

export const Dashboard: React.FC = () => {
  const [data, setData] = useState<DashboardSummary | null>(null);
  const [stations, setStations] = useState<Station[]>([]);
  const [selectedStation, setSelectedStation] = useState<string>('');
  const [dateFrom, setDateFrom] = useState<string>('');
  const [dateTo, setDateTo] = useState<string>('');
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const fetchDashboardData = async (overrides?: {
    stationId?: string;
    from?: string;
    to?: string;
  }) => {
    setIsLoading(true);
    setError(null);

    try {
      const stationId =
        overrides?.stationId !== undefined
          ? overrides.stationId
          : selectedStation;

      const from =
        overrides?.from !== undefined
          ? overrides.from
          : dateFrom;

      const to =
        overrides?.to !== undefined
          ? overrides.to
          : dateTo;

      const [dashRes, stationsRes] = await Promise.all([
        DashboardService.getSummary({
          stationId: stationId || undefined,
          from: from || undefined,
          to: to || undefined,
        }),
        CatalogService.getStations(),
      ]);

      setData(dashRes.data);
      setStations(stationsRes.data);
    } catch (err: any) {
      setError(
        err.message ||
          'Error al cargar los indicadores del dashboard.'
      );
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchDashboardData();
    // La carga inicial se realiza una sola vez.
    // Los filtros se aplican explícitamente mediante el botón "Filtrar".
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleApplyFilters = (e: React.FormEvent) => {
    e.preventDefault();
    fetchDashboardData();
  };

  const handleClearFilters = () => {
    setSelectedStation('');
    setDateFrom('');
    setDateTo('');

    fetchDashboardData({
      stationId: '',
      from: '',
      to: '',
    });
  };

  const totalDepartmentGallons =
    data?.consumptionByDepartment.reduce(
      (sum, item) => sum + item.gallons,
      0
    ) ?? 0;

  return (
    <div className="space-y-6">
      {/* Header y controles */}
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 bg-white p-5 rounded-xl border border-slate-200 shadow-2xs">
        <div>
          <h1 className="text-2xl font-bold text-[#062d4f] tracking-tight">
            Dashboard Ejecutivo
          </h1>

          <p className="text-sm text-slate-500 mt-0.5">
            Monitoreo en tiempo real de inventario, tickets y despachos
            autorizados
          </p>
        </div>

        <button
          onClick={() => fetchDashboardData()}
          disabled={isLoading}
          className="inline-flex items-center gap-2 px-4 py-2 text-xs font-semibold text-slate-700 bg-slate-100 hover:bg-slate-200 rounded-lg transition self-start md:self-auto disabled:opacity-50"
        >
          <RefreshCw
            className={`w-3.5 h-3.5 ${
              isLoading
                ? 'animate-spin text-[#087e8b]'
                : ''
            }`}
          />

          <span>Actualizar Indicadores</span>
        </button>
      </div>

      {/* Filtros */}
      <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-2xs">
        <form
          onSubmit={handleApplyFilters}
          className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4 items-end"
        >
          <div>
            <label className="block text-xs font-semibold text-slate-600 mb-1">
              Estación de Servicio
            </label>

            <select
              value={selectedStation}
              onChange={(e) =>
                setSelectedStation(e.target.value)
              }
              className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
            >
              <option value="">
                Todas las Estaciones
              </option>

              {stations.map((station) => (
                <option
                  key={station.id}
                  value={station.id}
                >
                  {station.name}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-600 mb-1">
              Desde (Fecha)
            </label>

            <input
              type="date"
              value={dateFrom}
              onChange={(e) =>
                setDateFrom(e.target.value)
              }
              className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-600 mb-1">
              Hasta (Fecha)
            </label>

            <input
              type="date"
              value={dateTo}
              onChange={(e) =>
                setDateTo(e.target.value)
              }
              className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
            />
          </div>

          <div className="flex items-center gap-2">
            <button
              type="submit"
              disabled={isLoading}
              className="flex-1 py-2 px-4 bg-[#087e8b] hover:bg-[#066570] text-white text-xs font-semibold rounded-lg transition flex items-center justify-center gap-1.5 shadow-2xs disabled:opacity-50"
            >
              <Filter className="w-3.5 h-3.5" />
              Filtrar
            </button>

            {(selectedStation || dateFrom || dateTo) && (
              <button
                type="button"
                onClick={handleClearFilters}
                disabled={isLoading}
                className="py-2 px-3 bg-slate-100 hover:bg-slate-200 text-slate-600 text-xs font-medium rounded-lg transition disabled:opacity-50"
              >
                Limpiar
              </button>
            )}
          </div>
        </form>
      </div>

      {error && (
        <AlertBanner
          type="error"
          title="Error de carga"
          message={error}
          onClose={() => setError(null)}
        />
      )}

      {isLoading && !data ? (
        <LoadingSpinner message="Consultando indicadores de inventario y consumo..." />
      ) : data ? (
        <>
          {/* Indicadores principales */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            {/* Tickets activos */}
            <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-2xs flex items-center gap-4">
              <div className="p-3.5 bg-blue-50 text-blue-600 rounded-xl">
                <Ticket className="w-6 h-6" />
              </div>

              <div>
                <span className="text-xs font-medium text-slate-500 uppercase tracking-wider">
                  Tickets Activos
                </span>

                <h3 className="text-2xl font-bold text-slate-900 mt-0.5">
                  {data.activeTickets}
                </h3>

                <span className="text-[11px] text-blue-600 font-medium">
                  Autorizados para despacho
                </span>
              </div>
            </div>

            {/* Solicitudes pendientes */}
            <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-2xs flex items-center gap-4">
              <div className="p-3.5 bg-amber-50 text-amber-600 rounded-xl">
                <Clock className="w-6 h-6" />
              </div>

              <div>
                <span className="text-xs font-medium text-slate-500 uppercase tracking-wider">
                  Solicitudes Pendientes
                </span>

                <h3 className="text-2xl font-bold text-slate-900 mt-0.5">
                  {data.pendingRequests}
                </h3>

                <span className="text-[11px] text-amber-600 font-medium">
                  Requieren aprobación
                </span>
              </div>
            </div>

            {/* Despachos de hoy */}
            <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-2xs flex items-center gap-4">
              <div className="p-3.5 bg-emerald-50 text-emerald-600 rounded-xl">
                <TrendingUp className="w-6 h-6" />
              </div>

              <div>
                <span className="text-xs font-medium text-slate-500 uppercase tracking-wider">
                  Despachos de Hoy
                </span>

                <h3 className="text-2xl font-bold text-slate-900 mt-0.5">
                  {data.dispatchesToday}{' '}
                  <span className="text-xs font-normal text-slate-400">
                    (
                    {data.gallonsDispatchedToday.toLocaleString()}
                    {' '}gal)
                  </span>
                </h3>

                <span className="text-[11px] text-emerald-600 font-medium">
                  Operaciones validadas
                </span>
              </div>
            </div>

            {/* Alertas de inventario */}
            <div
              className={`p-5 rounded-xl border shadow-2xs flex items-center gap-4 ${
                data.lowInventoryTanks > 0
                  ? 'bg-rose-50/70 border-rose-200'
                  : 'bg-white border-slate-200'
              }`}
            >
              <div
                className={`p-3.5 rounded-xl ${
                  data.lowInventoryTanks > 0
                    ? 'bg-rose-100 text-rose-600'
                    : 'bg-slate-100 text-slate-600'
                }`}
              >
                <AlertTriangle className="w-6 h-6" />
              </div>

              <div>
                <span className="text-xs font-medium text-slate-500 uppercase tracking-wider">
                  Alertas Nivel Crítico
                </span>

                <h3
                  className={`text-2xl font-bold mt-0.5 ${
                    data.lowInventoryTanks > 0
                      ? 'text-rose-700'
                      : 'text-slate-900'
                  }`}
                >
                  {data.lowInventoryTanks}
                </h3>

                <span
                  className={`text-[11px] font-medium ${
                    data.lowInventoryTanks > 0
                      ? 'text-rose-600'
                      : 'text-slate-500'
                  }`}
                >
                  {data.lowInventoryTanks > 0
                    ? 'Requiere reabastecimiento'
                    : 'Existencias estables'}
                </span>
              </div>
            </div>
          </div>

          {/* Inventario por combustible */}
          <div className="bg-white rounded-xl border border-slate-200 shadow-2xs overflow-hidden">
            <div className="p-5 border-b border-slate-100 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
              <div>
                <h2 className="text-base font-bold text-slate-900">
                  Resumen de Inventario por Combustible
                </h2>

                <p className="text-xs text-slate-500">
                  Existencia física consolidada y capacidad total por tipo
                  de combustible.
                </p>
              </div>

              <span className="self-start sm:self-auto px-2.5 py-1 text-xs font-semibold bg-cyan-50 text-[#087e8b] rounded-md border border-cyan-200">
                Fuente: Tanques / Ledger
              </span>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="bg-[#e8f4f5] text-[#063e5b] text-xs uppercase font-semibold">
                  <tr>
                    <th className="py-3 px-4">
                      Combustible
                    </th>

                    <th className="py-3 px-4 text-right">
                      Existencia Física
                    </th>

                    <th className="py-3 px-4 text-right">
                      Capacidad Total
                    </th>

                    <th className="py-3 px-4 text-right">
                      Nivel de Ocupación
                    </th>
                  </tr>
                </thead>

                <tbody className="divide-y divide-slate-100">
                  {data.inventoryByFuel.length === 0 ? (
                    <tr>
                      <td
                        colSpan={4}
                        className="py-8 px-4 text-center text-xs text-slate-400"
                      >
                        No hay información de inventario disponible para
                        los filtros seleccionados.
                      </td>
                    </tr>
                  ) : (
                    data.inventoryByFuel.map((item) => (
                      <tr
                        key={item.fuelTypeId}
                        className="hover:bg-slate-50 transition"
                      >
                        <td className="py-3.5 px-4 font-semibold text-slate-800">
                          <div className="flex items-center gap-2">
                            <Fuel className="w-4 h-4 text-[#087e8b]" />
                            <span>
                              {item.fuelTypeName}
                            </span>
                          </div>
                        </td>

                        <td className="py-3.5 px-4 text-right font-mono font-medium text-slate-700">
                          {item.currentStock.toLocaleString()} gal
                        </td>

                        <td className="py-3.5 px-4 text-right font-mono text-slate-700">
                          {item.totalCapacity.toLocaleString()} gal
                        </td>

                        <td className="py-3.5 px-4 text-right">
                          <span className="font-mono font-bold text-slate-900">
                            {item.percentage.toFixed(1)}%
                          </span>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>

          {/* Consumo y alertas */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Consumo por departamento */}
            <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-2xs">
              <div className="flex items-center gap-2 mb-4">
                <Building2 className="w-5 h-5 text-[#087e8b]" />

                <h3 className="text-base font-bold text-slate-900">
                  Consumo por Departamento
                </h3>
              </div>

              <div className="space-y-4">
                {data.consumptionByDepartment.length === 0 ? (
                  <p className="text-xs text-slate-400 py-4 text-center">
                    No hay consumo registrado para el período seleccionado.
                  </p>
                ) : (
                  data.consumptionByDepartment.map((department) => {
                    const percentage =
                      totalDepartmentGallons > 0
                        ? (department.gallons * 100) /
                          totalDepartmentGallons
                        : 0;

                    return (
                      <div key={department.name}>
                        <div className="flex justify-between gap-4 text-xs font-semibold mb-1">
                          <span className="text-slate-700">
                            {department.name}
                          </span>

                          <span className="text-slate-900 whitespace-nowrap">
                            {department.gallons.toLocaleString()} gal (
                            {percentage.toFixed(1)}%)
                          </span>
                        </div>

                        <div className="w-full bg-slate-100 rounded-full h-2.5 overflow-hidden">
                          <div
                            className="bg-[#087e8b] h-2.5 rounded-full transition-all duration-500"
                            style={{
                              width: `${Math.min(
                                percentage,
                                100
                              )}%`,
                            }}
                          />
                        </div>
                      </div>
                    );
                  })
                )}
              </div>
            </div>

            {/* Alertas operacionales */}
            <div className="bg-white p-5 rounded-xl border border-slate-200 shadow-2xs">
              <div className="flex items-center gap-2 mb-4">
                <AlertTriangle className="w-5 h-5 text-amber-600" />

                <h3 className="text-base font-bold text-slate-900">
                  Alertas Operacionales Recientes
                </h3>
              </div>

              <div className="space-y-3">
                {data.recentAlerts.length === 0 ? (
                  <p className="text-xs text-slate-400 py-4 text-center">
                    No hay alertas activas.
                  </p>
                ) : (
                  data.recentAlerts.map((alert) => (
                    <div
                      key={alert.tankId}
                      className="p-3 rounded-lg border text-xs flex items-start gap-2.5 bg-rose-50 border-rose-200 text-rose-900"
                    >
                      <AlertTriangle className="w-4 h-4 mt-0.5 shrink-0 text-rose-600" />

                      <div className="flex-1">
                        <p className="font-medium leading-relaxed">
                          {alert.message}
                        </p>

                        <span className="text-[10px] text-slate-500 mt-1 block">
                          {alert.stationName} · {alert.fuelTypeName} ·{' '}
                          {alert.currentStock.toLocaleString()} gal
                        </span>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </div>
          </div>
        </>
      ) : null}
    </div>
  );
};
