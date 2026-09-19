import React, { useEffect, useState } from 'react';
import {
  AlertTriangle,
  Filter,
  RefreshCw,
} from 'lucide-react';

import { AlertBanner } from '../components/common/AlertBanner';
import { LoadingSpinner } from '../components/common/LoadingSpinner';
import {
  AlertService,
  CatalogService,
} from '../services/api';

import {
  LowInventoryAlert,
  LowInventoryAlertFilters,
  PaginatedResult,
  Station,
} from '../types';

const gal = (value: number) =>
  `${Number(value).toLocaleString('es-DO', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })} gal`;

export const Alerts: React.FC = () => {
  const [stations, setStations] =
    useState<Station[]>([]);

  const [filters, setFilters] =
    useState<LowInventoryAlertFilters>({
      page: 1,
      pageSize: 20,
    });

  const [result, setResult] =
    useState<PaginatedResult<LowInventoryAlert> | null>(
      null,
    );

  const [isLoading, setIsLoading] =
    useState(false);

  const [error, setError] =
    useState<string | null>(null);

  useEffect(() => {
    void CatalogService.getStations()
      .then(response => {
        setStations(response.data);
      })
      .catch(() => {
        setStations([]);
      });
  }, []);

  const loadAlerts = async (
    currentFilters: LowInventoryAlertFilters = filters,
  ) => {
    setIsLoading(true);
    setError(null);

    try {
      const response =
        await AlertService.getLowInventory(
          currentFilters,
        );

      setResult(response.data);
    } catch (error: unknown) {
      setError(
        error instanceof Error
          ? error.message
          : 'No fue posible consultar las alertas.',
      );
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadAlerts();
    // Carga inicial.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const applyFilters = (
    event: React.FormEvent,
  ) => {
    event.preventDefault();

    const next = {
      ...filters,
      page: 1,
    };

    setFilters(next);
    void loadAlerts(next);
  };

  const changePage = (page: number) => {
    const next = {
      ...filters,
      page,
    };

    setFilters(next);
    void loadAlerts(next);
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 bg-white p-5 rounded-xl border border-slate-200 shadow-sm">
        <div>
          <div className="flex items-center gap-3">
            <div className="p-2.5 rounded-lg bg-amber-50 text-amber-600">
              <AlertTriangle className="w-6 h-6" />
            </div>

            <div>
              <h1 className="text-2xl font-bold text-[#062d4f] tracking-tight">
                Alertas de inventario
              </h1>

              <p className="text-sm text-slate-500 mt-0.5">
                Tanques que alcanzaron o están por debajo
                de su nivel crítico
              </p>
            </div>
          </div>
        </div>

        <button
          type="button"
          onClick={() => void loadAlerts()}
          disabled={isLoading}
          className="inline-flex items-center justify-center gap-2 px-4 py-2.5 bg-[#087e8b] hover:bg-[#066570] text-white text-xs font-semibold rounded-lg disabled:opacity-50"
        >
          <RefreshCw className="w-4 h-4" />
          Actualizar
        </button>
      </div>

      {error && (
        <AlertBanner
          type="error"
          message={error}
          onClose={() => setError(null)}
        />
      )}

      <form
        onSubmit={applyFilters}
        className="bg-white p-4 rounded-xl border border-slate-200 grid grid-cols-1 md:grid-cols-2 gap-3 items-end"
      >
        <label className="text-xs font-semibold text-slate-700">
          Estación

          <select
            value={filters.estacionId || ''}
            onChange={event =>
              setFilters({
                ...filters,
                estacionId:
                  event.target.value || undefined,
              })
            }
            className="mt-1 w-full py-2 px-3 border border-slate-300 rounded-lg"
          >
            <option value="">
              Todas las estaciones
            </option>

            {stations.map(station => (
              <option
                key={station.id}
                value={station.id}
              >
                {station.name}
              </option>
            ))}
          </select>
        </label>

        <button
          type="submit"
          className="inline-flex items-center justify-center gap-2 px-4 py-2 text-xs font-semibold text-white bg-[#087e8b] hover:bg-[#066570] rounded-lg"
        >
          <Filter className="w-4 h-4" />
          Filtrar
        </button>
      </form>

      <div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
        {isLoading ? (
          <LoadingSpinner message="Consultando alertas..." />
        ) : !result?.items.length ? (
          <div className="py-14 px-6 text-center">
            <div className="mx-auto w-12 h-12 rounded-full bg-emerald-50 flex items-center justify-center mb-4">
              <AlertTriangle className="w-6 h-6 text-emerald-600" />
            </div>

            <h2 className="font-semibold text-slate-800">
              Sin alertas de inventario
            </h2>

            <p className="text-sm text-slate-500 mt-1">
              Actualmente no hay tanques activos en
              nivel crítico de combustible.
            </p>
          </div>
        ) : (
          <>
            <div className="px-5 py-4 border-b border-slate-200 flex items-center justify-between">
              <div>
                <h2 className="font-semibold text-[#062d4f]">
                  Inventario crítico
                </h2>

                <p className="text-xs text-slate-500 mt-0.5">
                  {result.totalCount}{' '}
                  {result.totalCount === 1
                    ? 'tanque requiere atención'
                    : 'tanques requieren atención'}
                </p>
              </div>

              <span className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-red-50 text-red-700 rounded-full text-xs font-bold">
                <AlertTriangle className="w-3.5 h-3.5" />
                CRÍTICO
              </span>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="bg-[#e8f4f5] text-[#063e5b] text-xs uppercase">
                  <tr>
                    <th className="p-3">
                      Tanque
                    </th>

                    <th className="p-3">
                      Estación
                    </th>

                    <th className="p-3">
                      Combustible
                    </th>

                    <th className="p-3 text-right">
                      Stock actual
                    </th>

                    <th className="p-3 text-right">
                      Nivel crítico
                    </th>

                    <th className="p-3 text-right">
                      Déficit
                    </th>

                    <th className="p-3">
                      Severidad
                    </th>
                  </tr>
                </thead>

                <tbody className="divide-y">
                  {result.items.map(item => (
                    <tr
                      key={item.tanqueId}
                      className="hover:bg-red-50/30"
                    >
                      <td className="p-3">
                        <p className="font-mono font-semibold text-slate-900">
                          {item.tanqueCodigo}
                        </p>

                        <p className="text-xs text-slate-500">
                          {item.tanqueNombre || '—'}
                        </p>
                      </td>

                      <td className="p-3">
                        {item.estacion}
                      </td>

                      <td className="p-3">
                        {item.combustible}
                      </td>

                      <td className="p-3 text-right font-semibold text-red-700">
                        {gal(
                          item.stockActualGalones,
                        )}
                      </td>

                      <td className="p-3 text-right">
                        {gal(
                          item.nivelCriticoGalones,
                        )}
                      </td>

                      <td className="p-3 text-right font-semibold">
                        {gal(
                          item.deficitGalones,
                        )}
                      </td>

                      <td className="p-3">
                        <span className="inline-flex px-2 py-1 rounded-full bg-red-50 text-red-700 text-[10px] font-bold">
                          {item.severidad}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {result.totalPages > 1 && (
              <div className="flex items-center justify-between p-4 border-t text-xs text-slate-600">
                <span>
                  {result.totalCount} registros · página{' '}
                  {result.page} de {result.totalPages}
                </span>

                <div className="flex gap-2">
                  <button
                    type="button"
                    disabled={result.page <= 1}
                    onClick={() =>
                      changePage(
                        result.page - 1,
                      )
                    }
                    className="px-3 py-1.5 rounded bg-slate-100 disabled:opacity-50"
                  >
                    Anterior
                  </button>

                  <button
                    type="button"
                    disabled={
                      result.page >=
                      result.totalPages
                    }
                    onClick={() =>
                      changePage(
                        result.page + 1,
                      )
                    }
                    className="px-3 py-1.5 rounded bg-slate-100 disabled:opacity-50"
                  >
                    Siguiente
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
};