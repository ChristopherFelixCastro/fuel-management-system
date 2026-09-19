import React, { useEffect, useState } from 'react';
import {
  BarChart3,
  Download,
  Filter,
  History,
  RefreshCw,
  Warehouse,
} from 'lucide-react';

import { AlertBanner } from '../components/common/AlertBanner';
import { LoadingSpinner } from '../components/common/LoadingSpinner';
import { CatalogService, ReportService } from '../services/api';

import {
  ConsumptionReportFilters,
  ConsumptionReportItem,
  FuelType,
  InventoryReportFilters,
  InventoryReportItem,
  PaginatedResult,
  ReportFormat,
  Station,
  Tank,
  TraceabilityReportFilters,
  TraceabilityReportItem,
} from '../types';

type ReportTab = 'consumption' | 'inventory' | 'traceability';

const gal = (value: number) =>
  `${Number(value).toLocaleString('es-DO', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })} gal`;

const dateTime = (value: string) =>
  new Date(value).toLocaleString('es-DO');

const tabs: Array<{
  id: ReportTab;
  label: string;
  icon: React.ReactNode;
}> = [
  {
    id: 'consumption',
    label: 'Consumo',
    icon: <BarChart3 className="w-4 h-4" />,
  },
  {
    id: 'inventory',
    label: 'Inventario',
    icon: <Warehouse className="w-4 h-4" />,
  },
  {
    id: 'traceability',
    label: 'Trazabilidad',
    icon: <History className="w-4 h-4" />,
  },
];

const movementTypes = [
  'RECEPCION',
  'DESPACHO',
  'TRANSFERENCIA_ENTRADA',
  'TRANSFERENCIA_SALIDA',
  'MERMA',
  'AJUSTE_POSITIVO',
  'AJUSTE_NEGATIVO',
];

export const Reports: React.FC = () => {
  const [tab, setTab] = useState<ReportTab>('consumption');

  const [tanks, setTanks] = useState<Tank[]>([]);
  const [stations, setStations] = useState<Station[]>([]);
  const [fuelTypes, setFuelTypes] = useState<FuelType[]>([]);

  const [consumptionFilters, setConsumptionFilters] =
    useState<ConsumptionReportFilters>({
      page: 1,
      pageSize: 20,
    });

  const [inventoryFilters, setInventoryFilters] =
    useState<InventoryReportFilters>({
      page: 1,
      pageSize: 20,
    });

  const [traceabilityFilters, setTraceabilityFilters] =
    useState<TraceabilityReportFilters>({
      page: 1,
      pageSize: 20,
    });

  const [consumption, setConsumption] =
    useState<PaginatedResult<ConsumptionReportItem> | null>(null);

  const [inventory, setInventory] =
    useState<PaginatedResult<InventoryReportItem> | null>(null);

  const [traceability, setTraceability] =
    useState<PaginatedResult<TraceabilityReportItem> | null>(null);

  const [isLoading, setIsLoading] = useState(false);

  const [exporting, setExporting] =
    useState<ReportFormat | null>(null);

  const [alert, setAlert] = useState<{
    type: 'success' | 'error';
    message: string;
  } | null>(null);

  useEffect(() => {
    void Promise.all([
      CatalogService.getTanks(),
      CatalogService.getStations(),
      CatalogService.getFuelTypes(),
    ])
      .then(([tankResponse, stationResponse, fuelResponse]) => {
        setTanks(tankResponse.data);
        setStations(stationResponse.data);
        setFuelTypes(fuelResponse.data);
      })
      .catch(() => {
        setTanks([]);
        setStations([]);
        setFuelTypes([]);
      });
  }, []);

  const loadConsumption = async (
    filters: ConsumptionReportFilters = consumptionFilters,
  ) => {
    setIsLoading(true);

    try {
      const response = await ReportService.getConsumption(filters);
      setConsumption(response.data);
    } catch (error: unknown) {
      setAlert({
        type: 'error',
        message:
          error instanceof Error
            ? error.message
            : 'No fue posible cargar el reporte de consumo.',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const loadInventory = async (
    filters: InventoryReportFilters = inventoryFilters,
  ) => {
    setIsLoading(true);

    try {
      const response = await ReportService.getInventory(filters);
      setInventory(response.data);
    } catch (error: unknown) {
      setAlert({
        type: 'error',
        message:
          error instanceof Error
            ? error.message
            : 'No fue posible cargar el reporte de inventario.',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const loadTraceability = async (
    filters: TraceabilityReportFilters = traceabilityFilters,
  ) => {
    setIsLoading(true);

    try {
      const response = await ReportService.getTraceability(filters);
      setTraceability(response.data);
    } catch (error: unknown) {
      setAlert({
        type: 'error',
        message:
          error instanceof Error
            ? error.message
            : 'No fue posible cargar el reporte de trazabilidad.',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const loadCurrent = () => {
    if (tab === 'consumption') {
      return loadConsumption();
    }

    if (tab === 'inventory') {
      return loadInventory();
    }

    return loadTraceability();
  };

  useEffect(() => {
    if (tab === 'consumption') {
      void loadConsumption();
      return;
    }

    if (tab === 'inventory') {
      void loadInventory();
      return;
    }

    void loadTraceability();
    // Carga únicamente al cambiar de pestaña.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab]);

  const exportCurrent = async (format: ReportFormat) => {
    setExporting(format);

    try {
      if (tab === 'consumption') {
        await ReportService.exportConsumption(
          consumptionFilters,
          format,
        );
      } else if (tab === 'inventory') {
        await ReportService.exportInventory(
          inventoryFilters,
          format,
        );
      } else {
        await ReportService.exportTraceability(
          traceabilityFilters,
          format,
        );
      }

      setAlert({
        type: 'success',
        message: `Reporte exportado correctamente en ${format.toUpperCase()}.`,
      });
    } catch (error: unknown) {
      setAlert({
        type: 'error',
        message:
          error instanceof Error
            ? error.message
            : 'No fue posible exportar el reporte.',
      });
    } finally {
      setExporting(null);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col xl:flex-row xl:items-center xl:justify-between gap-4 bg-white p-5 rounded-xl border border-slate-200 shadow-sm">
        <div>
          <h1 className="text-2xl font-bold text-[#062d4f] tracking-tight">
            Reportes operacionales
          </h1>

          <p className="text-sm text-slate-500 mt-0.5">
            Consumo, inventario y trazabilidad de combustible
          </p>
        </div>

        <div className="flex flex-wrap gap-2">
          {(['pdf', 'xlsx', 'csv'] as ReportFormat[]).map(
            format => (
              <button
                key={format}
                type="button"
                disabled={exporting !== null}
                onClick={() => void exportCurrent(format)}
                className="inline-flex items-center gap-2 px-3 py-2.5 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-semibold rounded-lg disabled:opacity-50"
              >
                <Download className="w-4 h-4" />

                {exporting === format
                  ? 'Exportando...'
                  : format.toUpperCase()}
              </button>
            ),
          )}

          <button
            type="button"
            onClick={() => void loadCurrent()}
            disabled={isLoading}
            className="inline-flex items-center gap-2 px-4 py-2.5 bg-[#087e8b] hover:bg-[#066570] text-white text-xs font-semibold rounded-lg disabled:opacity-50"
          >
            <RefreshCw className="w-4 h-4" />
            Actualizar
          </button>
        </div>
      </div>

      {alert && (
        <AlertBanner
          type={alert.type}
          message={alert.message}
          onClose={() => setAlert(null)}
        />
      )}

      <div className="bg-white rounded-xl border border-slate-200 p-1 flex flex-wrap gap-1">
        {tabs.map(item => (
          <button
            key={item.id}
            type="button"
            onClick={() => setTab(item.id)}
            className={`inline-flex items-center gap-2 px-4 py-2.5 rounded-lg text-xs font-semibold transition ${
              tab === item.id
                ? 'bg-[#087e8b] text-white'
                : 'text-slate-600 hover:bg-slate-100'
            }`}
          >
            {item.icon}
            {item.label}
          </button>
        ))}
      </div>

      {tab === 'consumption' && (
        <ConsumptionSection
          filters={consumptionFilters}
          setFilters={setConsumptionFilters}
          tanks={tanks}
          result={consumption}
          loading={isLoading}
          load={loadConsumption}
        />
      )}

      {tab === 'inventory' && (
        <InventorySection
          filters={inventoryFilters}
          setFilters={setInventoryFilters}
          tanks={tanks}
          stations={stations}
          fuelTypes={fuelTypes}
          result={inventory}
          loading={isLoading}
          load={loadInventory}
        />
      )}

      {tab === 'traceability' && (
        <TraceabilitySection
          filters={traceabilityFilters}
          setFilters={setTraceabilityFilters}
          tanks={tanks}
          result={traceability}
          loading={isLoading}
          load={loadTraceability}
        />
      )}
    </div>
  );
};

const ConsumptionSection: React.FC<{
  filters: ConsumptionReportFilters;
  setFilters: React.Dispatch<
    React.SetStateAction<ConsumptionReportFilters>
  >;
  tanks: Tank[];
  result: PaginatedResult<ConsumptionReportItem> | null;
  loading: boolean;
  load: (filters?: ConsumptionReportFilters) => Promise<void>;
}> = ({
  filters,
  setFilters,
  tanks,
  result,
  loading,
  load,
}) => {
  const apply = (event: React.FormEvent) => {
    event.preventDefault();

    const next = {
      ...filters,
      page: 1,
    };

    setFilters(next);
    void load(next);
  };

  return (
    <>
      <form
        onSubmit={apply}
        className="bg-white p-4 rounded-xl border border-slate-200 grid grid-cols-1 md:grid-cols-4 gap-3 items-end"
      >
        <DateField
          label="Fecha desde"
          value={filters.fechaDesde}
          onChange={value =>
            setFilters({
              ...filters,
              fechaDesde: value,
            })
          }
        />

        <DateField
          label="Fecha hasta"
          value={filters.fechaHasta}
          onChange={value =>
            setFilters({
              ...filters,
              fechaHasta: value,
            })
          }
        />

        <TankField
          tanks={tanks}
          value={filters.tanqueId}
          onChange={value =>
            setFilters({
              ...filters,
              tanqueId: value,
            })
          }
        />

        <FilterButton />
      </form>

      <ReportTable loading={loading}>
        {!result?.items.length ? (
          <Empty />
        ) : (
          <table className="w-full text-left text-sm">
            <thead className="bg-[#e8f4f5] text-[#063e5b] text-xs uppercase">
              <tr>
                <th className="p-3">Fecha</th>
                <th className="p-3">Tanque</th>
                <th className="p-3">Combustible</th>
                <th className="p-3 text-right">Despachos</th>
                <th className="p-3 text-right">Galones</th>
              </tr>
            </thead>

            <tbody className="divide-y">
              {result.items.map((item, index) => (
                <tr
                  key={`${item.fecha}-${item.tanqueId}-${index}`}
                  className="hover:bg-slate-50"
                >
                  <td className="p-3">{item.fecha}</td>

                  <td className="p-3">
                    <p className="font-mono font-semibold">
                      {item.tanqueCodigo}
                    </p>

                    <p className="text-xs text-slate-500">
                      {item.tanqueNombre || '—'}
                    </p>
                  </td>

                  <td className="p-3">{item.combustible}</td>

                  <td className="p-3 text-right">
                    {item.cantidadDespachos}
                  </td>

                  <td className="p-3 text-right font-semibold">
                    {gal(item.totalDespachadoGalones)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        <Pagination
          result={result}
          onPage={page => {
            const next = {
              ...filters,
              page,
            };

            setFilters(next);
            void load(next);
          }}
        />
      </ReportTable>
    </>
  );
};

const InventorySection: React.FC<{
  filters: InventoryReportFilters;
  setFilters: React.Dispatch<
    React.SetStateAction<InventoryReportFilters>
  >;
  tanks: Tank[];
  stations: Station[];
  fuelTypes: FuelType[];
  result: PaginatedResult<InventoryReportItem> | null;
  loading: boolean;
  load: (filters?: InventoryReportFilters) => Promise<void>;
}> = ({
  filters,
  setFilters,
  tanks,
  stations,
  fuelTypes,
  result,
  loading,
  load,
}) => {
  const apply = (event: React.FormEvent) => {
    event.preventDefault();

    const next = {
      ...filters,
      page: 1,
    };

    setFilters(next);
    void load(next);
  };

  return (
    <>
      <form
        onSubmit={apply}
        className="bg-white p-4 rounded-xl border border-slate-200 grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-3 items-end"
      >
        <TankField
          tanks={tanks}
          value={filters.tanqueId}
          onChange={value =>
            setFilters({
              ...filters,
              tanqueId: value,
            })
          }
        />

        <label className="text-xs font-semibold text-slate-700">
          Combustible

          <select
            value={filters.tipoCombustibleId ?? ''}
            onChange={event =>
              setFilters({
                ...filters,
                tipoCombustibleId: event.target.value
                  ? Number(event.target.value)
                  : undefined,
              })
            }
            className="mt-1 w-full py-2 px-3 border border-slate-300 rounded-lg"
          >
            <option value="">Todos</option>

            {fuelTypes.map(fuel => (
              <option
                key={fuel.id}
                value={fuel.id}
              >
                {fuel.name}
              </option>
            ))}
          </select>
        </label>

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
            <option value="">Todas</option>

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

        <FilterButton />
      </form>

      <ReportTable loading={loading}>
        {!result?.items.length ? (
          <Empty />
        ) : (
          <table className="w-full text-left text-sm">
            <thead className="bg-[#e8f4f5] text-[#063e5b] text-xs uppercase">
              <tr>
                <th className="p-3">Tanque</th>
                <th className="p-3">Estación</th>
                <th className="p-3">Combustible</th>
                <th className="p-3 text-right">Capacidad</th>
                <th className="p-3 text-right">Stock</th>
                <th className="p-3 text-right">Crítico</th>
                <th className="p-3 text-right">Ocupación</th>
                <th className="p-3">Estado</th>
              </tr>
            </thead>

            <tbody className="divide-y">
              {result.items.map(item => (
                <tr
                  key={item.tanqueId}
                  className="hover:bg-slate-50"
                >
                  <td className="p-3 font-mono font-semibold">
                    {item.tanqueCodigo}
                  </td>

                  <td className="p-3">{item.estacion}</td>

                  <td className="p-3">{item.combustible}</td>

                  <td className="p-3 text-right">
                    {gal(item.capacidadMaximaGalones)}
                  </td>

                  <td className="p-3 text-right font-semibold">
                    {gal(item.stockActualGalones)}
                  </td>

                  <td className="p-3 text-right">
                    {gal(item.nivelCriticoGalones)}
                  </td>

                  <td className="p-3 text-right">
                    {Number(
                      item.porcentajeOcupacion,
                    ).toLocaleString('es-DO', {
                      maximumFractionDigits: 2,
                    })}
                    %
                  </td>

                  <td className="p-3">
                    <span className="inline-flex px-2 py-1 rounded-full bg-slate-100 text-slate-700 text-[10px] font-bold">
                      {item.estadoStock}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        <Pagination
          result={result}
          onPage={page => {
            const next = {
              ...filters,
              page,
            };

            setFilters(next);
            void load(next);
          }}
        />
      </ReportTable>
    </>
  );
};

const TraceabilitySection: React.FC<{
  filters: TraceabilityReportFilters;
  setFilters: React.Dispatch<
    React.SetStateAction<TraceabilityReportFilters>
  >;
  tanks: Tank[];
  result: PaginatedResult<TraceabilityReportItem> | null;
  loading: boolean;
  load: (filters?: TraceabilityReportFilters) => Promise<void>;
}> = ({
  filters,
  setFilters,
  tanks,
  result,
  loading,
  load,
}) => {
  const apply = (event: React.FormEvent) => {
    event.preventDefault();

    const next = {
      ...filters,
      page: 1,
    };

    setFilters(next);
    void load(next);
  };

  return (
    <>
      <form
        onSubmit={apply}
        className="bg-white p-4 rounded-xl border border-slate-200 grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-5 gap-3 items-end"
      >
        <DateField
          label="Fecha desde"
          value={filters.fechaDesde}
          onChange={value =>
            setFilters({
              ...filters,
              fechaDesde: value,
            })
          }
        />

        <DateField
          label="Fecha hasta"
          value={filters.fechaHasta}
          onChange={value =>
            setFilters({
              ...filters,
              fechaHasta: value,
            })
          }
        />

        <TankField
          tanks={tanks}
          value={filters.tanqueId}
          onChange={value =>
            setFilters({
              ...filters,
              tanqueId: value,
            })
          }
        />

        <label className="text-xs font-semibold text-slate-700">
          Movimiento

          <select
            value={filters.tipoMovimiento || ''}
            onChange={event =>
              setFilters({
                ...filters,
                tipoMovimiento:
                  event.target.value || undefined,
              })
            }
            className="mt-1 w-full py-2 px-3 border border-slate-300 rounded-lg"
          >
            <option value="">Todos</option>

            {movementTypes.map(type => (
              <option
                key={type}
                value={type}
              >
               {type.replace(/_/g, ' ')}
              </option>
            ))}
          </select>
        </label>

        <FilterButton />
      </form>

      <ReportTable loading={loading}>
        {!result?.items.length ? (
          <Empty />
        ) : (
          <table className="w-full text-left text-sm">
            <thead className="bg-[#e8f4f5] text-[#063e5b] text-xs uppercase">
              <tr>
                <th className="p-3">Fecha</th>
                <th className="p-3">Tanque</th>
                <th className="p-3">Movimiento</th>
                <th className="p-3 text-right">Cantidad</th>
                <th className="p-3 text-right">Anterior</th>
                <th className="p-3 text-right">Posterior</th>
                <th className="p-3">Registrado por</th>
              </tr>
            </thead>

            <tbody className="divide-y">
              {result.items.map(item => (
                <tr
                  key={item.movimientoId}
                  className="hover:bg-slate-50"
                >
                  <td className="p-3 whitespace-nowrap">
                    {dateTime(item.fechaMovimiento)}
                  </td>

                  <td className="p-3">
                    <p className="font-mono font-semibold">
                      {item.tanqueCodigo}
                    </p>

                    <p className="text-xs text-slate-500">
                      {item.estacion}
                    </p>
                  </td>

                  <td className="p-3">
                    {item.tipoMovimiento.replace(/_/g, ' ')}
                  </td>

                  <td className="p-3 text-right font-semibold">
                    {gal(item.cantidadGalones)}
                  </td>

                  <td className="p-3 text-right">
                    {gal(item.saldoAnteriorGalones)}
                  </td>

                  <td className="p-3 text-right">
                    {gal(item.saldoPosteriorGalones)}
                  </td>

                  <td className="p-3">
                    {item.registradoPor}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        <Pagination
          result={result}
          onPage={page => {
            const next = {
              ...filters,
              page,
            };

            setFilters(next);
            void load(next);
          }}
        />
      </ReportTable>
    </>
  );
};

const DateField: React.FC<{
  label: string;
  value?: string;
  onChange: (value?: string) => void;
}> = ({
  label,
  value,
  onChange,
}) => (
  <label className="text-xs font-semibold text-slate-700">
    {label}

    <input
      type="date"
      value={value || ''}
      onChange={event =>
        onChange(
          event.target.value || undefined,
        )
      }
      className="mt-1 w-full py-2 px-3 border border-slate-300 rounded-lg"
    />
  </label>
);

const TankField: React.FC<{
  tanks: Tank[];
  value?: string;
  onChange: (value?: string) => void;
}> = ({
  tanks,
  value,
  onChange,
}) => (
  <label className="text-xs font-semibold text-slate-700">
    Tanque

    <select
      value={value || ''}
      onChange={event =>
        onChange(
          event.target.value || undefined,
        )
      }
      className="mt-1 w-full py-2 px-3 border border-slate-300 rounded-lg"
    >
      <option value="">
        Todos los tanques
      </option>

      {tanks.map(tank => (
        <option
          key={tank.id}
          value={tank.id}
        >
          {tank.code}
          {tank.name
            ? ` — ${tank.name}`
            : ''}
        </option>
      ))}
    </select>
  </label>
);

const FilterButton: React.FC = () => (
  <button
    type="submit"
    className="inline-flex justify-center items-center gap-2 px-4 py-2 text-xs font-semibold text-white bg-[#087e8b] hover:bg-[#066570] rounded-lg"
  >
    <Filter className="w-4 h-4" />
    Filtrar
  </button>
);

const Empty: React.FC = () => (
  <div className="p-8 text-center text-sm text-slate-500">
    No se encontraron registros con los filtros seleccionados.
  </div>
);

const ReportTable: React.FC<{
  loading: boolean;
  children: React.ReactNode;
}> = ({
  loading,
  children,
}) => (
  <div className="mt-4 bg-white rounded-xl border border-slate-200 overflow-hidden">
    {loading ? (
      <LoadingSpinner message="Cargando reporte..." />
    ) : (
      <div className="overflow-x-auto">
        {children}
      </div>
    )}
  </div>
);

function Pagination<T>({
  result,
  onPage,
}: {
  result: PaginatedResult<T> | null;
  onPage: (page: number) => void;
}) {
  if (
    !result ||
    result.totalPages <= 1
  ) {
    return null;
  }

  return (
    <div className="flex items-center justify-between p-4 border-t text-xs text-slate-600">
      <span>
        {result.totalCount} registros · página {result.page} de{' '}
        {result.totalPages}
      </span>

      <div className="flex gap-2">
        <button
          type="button"
          disabled={result.page <= 1}
          onClick={() =>
            onPage(result.page - 1)
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
            onPage(result.page + 1)
          }
          className="px-3 py-1.5 rounded bg-slate-100 disabled:opacity-50"
        >
          Siguiente
        </button>
      </div>
    </div>
  );
}