import {
  ApiResponse,
  User,
  CreateUserDto,
  UpdateUserDto,
  Department,
  CreateDepartmentDto,
  UpdateDepartmentDto,
  Employee,
  CreateEmployeeDto,
  UpdateEmployeeDto,
  Vehicle,
  CreateVehicleDto,
  UpdateVehicleDto,
  DashboardSummary,
  Station,
  FuelType,
  LoginCredentials,
  CoreLoginResponseData,
  UserProfileData,
  Role,
  Closure,
  ClosureFilters,
  ClosureStatus,
  PaginatedResult,
  Tank,
  ConsumptionReportFilters,
  InventoryReportFilters,
  TraceabilityReportFilters,
  ConsumptionReportItem,
  InventoryReportItem,
  TraceabilityReportItem,
  ReportFormat,
  LowInventoryAlertFilters,
LowInventoryAlert,
  FuelRequest,
  CreateFuelRequestDto,
  ApproveFuelRequestDto,
  FuelRequestFilter,
} from '../types';

import {
  apiDownload,
  apiRequest,
  saveDownloadedFile,
} from './apiClient';

// ================= CORE TYPES =================

type CoreDepartment = {
  id: string;
  codigo: string;
  nombre: string;
  descripcion?: string | null;
  activo: boolean;
  fechaCreacion: string;
};

type CoreEmployee = {
  id: string;
  departamentoId: string;
  departamentoNombre: string;
  codigoEmpleado: string;
  nombre: string;
  apellido: string;
  cedula: string;
  email?: string | null;
  telefono?: string | null;
  activo: boolean;
  fechaCreacion: string;
};

type CoreVehicle = {
  id: string;
  departamentoId: string;
  departamentoNombre: string;
  tipoCombustibleId: number;
  tipoCombustibleNombre: string;
  placa: string;
  ficha: string;
  marca?: string | null;
  modelo?: string | null;
  anio?: number | null;
  capacidadTanque: number;
  odometroActual: number;
  activo: boolean;
  fechaCreacion: string;
};

type CoreUser = {
  id: string;
  nombreUsuario: string;
  email: string;
  rolId: number;
  rolNombre: User['role'];
  empleadoId?: string | null;
  empleadoNombre?: string | null;
  estacionId?: string | null;
  estacionNombre?: string | null;
  activo: boolean;
  fechaCreacion: string;
};

type CoreTank = {
  id: string;
  estacionId: string;
  estacionNombre?: string | null;
  tipoCombustibleId: number;
  combustibleNombre?: string | null;
  codigo: string;
  nombre?: string | null;
};

type CoreClosure = {
  id: string;
  tanqueId: string;
  tanqueCodigo?: string | null;
  tanqueNombre?: string | null;
  estacionId?: string | null;
  estacionNombre?: string | null;
  fechaCierre: string;
  stockInicial: number;
  totalRecepciones: number;
  totalTransferenciasEntrada: number;
  totalTransferenciasSalida: number;
  totalDespachos: number;
  totalAjustesPositivos: number;
  totalAjustesNegativos: number;
  stockTeoricoFinal: number;
  stockFisicoFinal: number;
  diferencia: number;
  estado: ClosureStatus;
  creadoPorUsuarioId: string;
  creadoPor?: string | null;
  revisadoPorUsuarioId?: string | null;
  revisadoPor?: string | null;
  fechaCreacion: string;
  fechaRevision?: string | null;
  motivoDiferencia?: string | null;
  motivoRechazo?: string | null;
  observaciones?: string | null;
};

// ================= MAPPERS =================

const mapDepartment = (
  x: CoreDepartment,
): Department => ({
  id: x.id,
  code: x.codigo,
  name: x.nombre,
  description: x.descripcion,
  isActive: x.activo,
  createdAt: x.fechaCreacion,
});

const mapEmployee = (
  x: CoreEmployee,
): Employee => ({
  id: x.id,
  employeeNumber: x.codigoEmpleado,
  firstName: x.nombre,
  lastName: x.apellido,
  documentId: x.cedula,
  email: x.email,
  phone: x.telefono,
  departmentId: x.departamentoId,
  departmentName: x.departamentoNombre,
  isActive: x.activo,
  createdAt: x.fechaCreacion,
});

const mapVehicle = (
  x: CoreVehicle,
): Vehicle => ({
  id: x.id,
  plate: x.placa,
  assetNumber: x.ficha,
  brand: x.marca,
  model: x.modelo,
  year: x.anio,
  departmentId: x.departamentoId,
  departmentName: x.departamentoNombre,
  fuelTypeId: String(x.tipoCombustibleId),
  fuelTypeName: x.tipoCombustibleNombre,
  tankCapacity: x.capacidadTanque,
  currentOdometer: x.odometroActual,
  isActive: x.activo,
  createdAt: x.fechaCreacion,
});

const mapUser = (
  x: CoreUser,
): User => ({
  id: x.id,
  fullName: x.empleadoNombre || x.nombreUsuario,
  email: x.email,
  username: x.nombreUsuario,
  role: x.rolNombre,
  employeeId: x.empleadoId,
  stationId: x.estacionId,
  stationName: x.estacionNombre,
  isActive: x.activo,
  createdAt: x.fechaCreacion,
});

const mapTank = (
  x: CoreTank,
): Tank => ({
  id: x.id,
  stationId: x.estacionId,
  stationName: x.estacionNombre,
  fuelTypeId: x.tipoCombustibleId,
  fuelTypeName: x.combustibleNombre,
  code: x.codigo,
  name: x.nombre,
});

const mapClosure = (
  x: CoreClosure,
): Closure => ({
  id: x.id,
  tankId: x.tanqueId,
  tankCode: x.tanqueCodigo,
  tankName: x.tanqueNombre,
  stationId: x.estacionId,
  stationName: x.estacionNombre,
  closureDate: x.fechaCierre,
  openingStock: x.stockInicial,
  totalReceipts: x.totalRecepciones,
  totalTransfersIn: x.totalTransferenciasEntrada,
  totalTransfersOut: x.totalTransferenciasSalida,
  totalDispatches: x.totalDespachos,
  totalPositiveAdjustments: x.totalAjustesPositivos,
  totalNegativeAdjustments: x.totalAjustesNegativos,
  theoreticalFinalStock: x.stockTeoricoFinal,
  physicalFinalStock: x.stockFisicoFinal,
  difference: x.diferencia,
  status: x.estado,
  createdByUserId: x.creadoPorUsuarioId,
  createdBy: x.creadoPor,
  reviewedByUserId: x.revisadoPorUsuarioId,
  reviewedBy: x.revisadoPor,
  createdAt: x.fechaCreacion,
  reviewedAt: x.fechaRevision,
  differenceReason: x.motivoDiferencia,
  rejectionReason: x.motivoRechazo,
  observations: x.observaciones,
});

const mapped = <T, R>(
  response: ApiResponse<T>,
  mapper: (value: T) => R,
): ApiResponse<R> => ({
  ...response,
  data: mapper(response.data),
});

const roleId = (role: User['role']) =>
  (
    {
      ADMINISTRADOR: 1,
      SUPERVISOR: 2,
      DESPACHADOR: 3,
      SOLICITANTE: 4,
      AUDITOR: 5,
    } as const
  )[role];

const employeePayload = (
  dto: CreateEmployeeDto | UpdateEmployeeDto,
) => ({
  departamentoId: dto.departmentId,
  codigoEmpleado: dto.employeeNumber,
  nombre: dto.firstName,
  apellido: dto.lastName,
  cedula: dto.documentId || null,
  cargo: null,
  email: dto.email || null,
  telefono: dto.phone || null,
});

const vehiclePayload = (
  dto: CreateVehicleDto | UpdateVehicleDto,
) => ({
  departamentoId: dto.departmentId,
  tipoCombustibleId: Number(dto.fuelTypeId),
  placa: dto.plate,
  ficha: dto.assetNumber || '',
  marca: dto.brand || null,
  modelo: dto.model || null,
  anio: dto.year || null,
  tipoVehiculo: null,
  capacidadTanque: dto.tankCapacity,
  odometroActual: dto.currentOdometer,
});

// ================= AUTH =================

export const AuthService = {
  login: (credentials: LoginCredentials) =>
    apiRequest<CoreLoginResponseData>('/auth/login', {
      method: 'POST',
      body: JSON.stringify(credentials),
    }),

  logout: (refreshToken: string) =>
    apiRequest<{ message: string }>('/auth/logout', {
      method: 'POST',
      body: JSON.stringify({ refreshToken }),
    }),

  getMe: () =>
    apiRequest<UserProfileData>('/auth/me'),
};

// ================= USUARIOS =================

export const UserService = {
  async getAll() {
    return mapped(
      await apiRequest<CoreUser[]>(
        '/users?incluirInactivos=true',
      ),
      xs => xs.map(mapUser),
    );
  },

  async getById(id: string) {
    return mapped(
      await apiRequest<CoreUser>(`/users/${id}`),
      mapUser,
    );
  },

  async create(dto: CreateUserDto) {
    return mapped(
      await apiRequest<CoreUser>('/users', {
        method: 'POST',
        body: JSON.stringify({
          nombreUsuario: dto.username,
          email: dto.email,
          password: dto.password,
          rolId: roleId(dto.role),
          empleadoId: dto.employeeId || null,
          estacionId:
            dto.role === 'DESPACHADOR'
              ? dto.stationId || null
              : null,
        }),
      }),
      mapUser,
    );
  },

  async update(
    id: string,
    dto: UpdateUserDto,
  ) {
    const response = await apiRequest<CoreUser>(
      `/users/${id}`,
      {
        method: 'PUT',
        body: JSON.stringify({
          nombreUsuario: dto.username,
          email: dto.email,
          rolId: roleId(dto.role!),
          empleadoId: dto.employeeId || null,
          estacionId:
            dto.role === 'DESPACHADOR'
              ? dto.stationId || null
              : null,
          activo: dto.isActive ?? true,
          bloqueado: false,
        }),
      },
    );

    if (dto.password) {
      await apiRequest<null>(
        `/users/${id}/password`,
        {
          method: 'PATCH',
          body: JSON.stringify({
            nuevaPassword: dto.password,
          }),
        },
      );
    }

    return mapped(response, mapUser);
  },

  deactivate: (id: string) =>
    apiRequest<null>(
      `/users/${id}/deactivate`,
      {
        method: 'PATCH',
      },
    ),
};

// ================= DEPARTAMENTOS =================

export const DepartmentService = {
  async getAll() {
    return mapped(
      await apiRequest<CoreDepartment[]>(
        '/departments?incluirInactivos=true',
      ),
      xs => xs.map(mapDepartment),
    );
  },

  async create(dto: CreateDepartmentDto) {
    return mapped(
      await apiRequest<CoreDepartment>(
        '/departments',
        {
          method: 'POST',
          body: JSON.stringify({
            codigo: dto.code,
            nombre: dto.name,
            descripcion: dto.description || null,
          }),
        },
      ),
      mapDepartment,
    );
  },

  async update(
    id: string,
    dto: UpdateDepartmentDto,
  ) {
    return mapped(
      await apiRequest<CoreDepartment>(
        `/departments/${id}`,
        {
          method: 'PUT',
          body: JSON.stringify({
            codigo: dto.code,
            nombre: dto.name,
            descripcion: dto.description || null,
            activo: dto.isActive ?? true,
          }),
        },
      ),
      mapDepartment,
    );
  },

  deactivate: (id: string) =>
    apiRequest<null>(
      `/departments/${id}/deactivate`,
      {
        method: 'PATCH',
      },
    ),
};

// ================= EMPLEADOS =================

export const EmployeeService = {
  async getAll() {
    return mapped(
      await apiRequest<CoreEmployee[]>(
        '/employees?incluirInactivos=true',
      ),
      xs => xs.map(mapEmployee),
    );
  },

  async create(dto: CreateEmployeeDto) {
    return mapped(
      await apiRequest<CoreEmployee>(
        '/employees',
        {
          method: 'POST',
          body: JSON.stringify(
            employeePayload(dto),
          ),
        },
      ),
      mapEmployee,
    );
  },

  async update(
    id: string,
    dto: UpdateEmployeeDto,
  ) {
    return mapped(
      await apiRequest<CoreEmployee>(
        `/employees/${id}`,
        {
          method: 'PUT',
          body: JSON.stringify({
            ...employeePayload(dto),
            activo: dto.isActive ?? true,
          }),
        },
      ),
      mapEmployee,
    );
  },

  deactivate: (id: string) =>
    apiRequest<null>(
      `/employees/${id}/deactivate`,
      {
        method: 'PATCH',
      },
    ),
};

// ================= VEHÍCULOS =================

export const VehicleService = {
  async getAll() {
    return mapped(
      await apiRequest<CoreVehicle[]>(
        '/vehicles?incluirInactivos=true',
      ),
      xs => xs.map(mapVehicle),
    );
  },

  async create(dto: CreateVehicleDto) {
    return mapped(
      await apiRequest<CoreVehicle>(
        '/vehicles',
        {
          method: 'POST',
          body: JSON.stringify(
            vehiclePayload(dto),
          ),
        },
      ),
      mapVehicle,
    );
  },

  async update(
    id: string,
    dto: UpdateVehicleDto,
  ) {
    return mapped(
      await apiRequest<CoreVehicle>(
        `/vehicles/${id}`,
        {
          method: 'PUT',
          body: JSON.stringify({
            ...vehiclePayload(dto),
            activo: dto.isActive ?? true,
          }),
        },
      ),
      mapVehicle,
    );
  },

  deactivate: (id: string) =>
    apiRequest<null>(
      `/vehicles/${id}/deactivate`,
      {
        method: 'PATCH',
      },
    ),
};

// ================= CATÁLOGOS =================

export const CatalogService = {
  async getRoles() {
    return mapped(
      await apiRequest<
        Array<{
          id: number;
          nombre: User['role'];
          descripcion?: string | null;
        }>
      >('/masters/roles'),
      xs =>
        xs.map(
          x =>
            ({
              id: x.id,
              name: x.nombre,
              description: x.descripcion,
            }) as Role,
        ),
    );
  },

  async getStations() {
    return mapped(
      await apiRequest<
        Array<{
          id: string;
          codigo: string;
          nombre: string;
          direccion?: string;
          activo: boolean;
        }>
      >('/masters/estaciones'),
      xs =>
        xs.map(
          x =>
            ({
              id: x.id,
              code: x.codigo,
              name: x.nombre,
              address: x.direccion,
              isActive: x.activo,
            }) as Station,
        ),
    );
  },

  async getFuelTypes() {
    return mapped(
      await apiRequest<
        Array<{
          id: number;
          codigo: string;
          nombre: string;
        }>
      >('/masters/fuel-types'),
      xs =>
        xs.map(
          x =>
            ({
              id: String(x.id),
              code: x.codigo,
              name: x.nombre,
            }) as FuelType,
        ),
    );
  },

  async getVehicles() {
    return apiRequest<
      Array<{
        id: string;
        placa: string;
        ficha: string;
        marca?: string | null;
        modelo?: string | null;
        anio?: number | null;
        tipoVehiculo?: string | null;
        capacidadTanque: number;
        odometroActual: number;
        tipoCombustibleId: number;
        combustibleNombre?: string | null;
        departamentoId: string;
        departamentoNombre?: string | null;
        activo: boolean;
      }>
    >('/masters/vehiculos');
  },

  async getTanks() {
    return mapped(
      await apiRequest<CoreTank[]>(
        '/masters/tanques',
      ),
      xs => xs.map(mapTank),
    );
  },
};

// ================= CIERRES =================

export const ClosureService = {
  async getAll(
    filters: ClosureFilters = {},
  ) {
    const query = new URLSearchParams();

    if (filters.fechaDesde) {
      query.set(
        'fechaDesde',
        filters.fechaDesde,
      );
    }

    if (filters.fechaHasta) {
      query.set(
        'fechaHasta',
        filters.fechaHasta,
      );
    }

    if (filters.tanqueId) {
      query.set(
        'tanqueId',
        filters.tanqueId,
      );
    }

    if (filters.estado) {
      query.set(
        'estado',
        filters.estado,
      );
    }

    query.set(
      'page',
      String(filters.page ?? 1),
    );

    query.set(
      'pageSize',
      String(filters.pageSize ?? 20),
    );

    const response = await apiRequest<{
      items: CoreClosure[];
      totalCount: number;
      page: number;
      pageSize: number;
      totalPages: number;
    }>(
      `/closures?${query.toString()}`,
    );

    return {
      ...response,
      data: {
        ...response.data,
        items:
          response.data.items.map(
            mapClosure,
          ),
      } as PaginatedResult<Closure>,
    };
  },

  async getById(id: string) {
    return mapped(
      await apiRequest<CoreClosure>(
        `/closures/${id}`,
      ),
      mapClosure,
    );
  },

  approve: (id: string) =>
    apiRequest<object>(
      `/closures/${id}/approve`,
      {
        method: 'PUT',
      },
    ),

  reject: (
    id: string,
    motivo: string,
  ) =>
    apiRequest<object>(
      `/closures/${id}/reject`,
      {
        method: 'PUT',
        body: JSON.stringify({
          motivo,
        }),
      },
    ),
};

// ================= DASHBOARD =================

export const DashboardService = {
  getSummary: (params?: {
    stationId?: string;
    from?: string;
    to?: string;
  }) => {
    const query =
      new URLSearchParams();

    if (params?.stationId) {
      query.set(
        'stationId',
        params.stationId,
      );
    }

    if (params?.from) {
      query.set(
        'from',
        params.from,
      );
    }

    if (params?.to) {
      query.set(
        'to',
        params.to,
      );
    }

    const queryString =
      query.toString();

    return apiRequest<DashboardSummary>(
      `/dashboard${
        queryString
          ? `?${queryString}`
          : ''
      }`,
    );
  },
};

// ================= REPORTES =================

const buildReportQuery = (
  filters: Record<string, unknown>,
  includePagination = true,
) => {
  const query =
    new URLSearchParams();

  Object.entries(filters).forEach(
    ([key, value]) => {
      if (
        value === undefined ||
        value === null ||
        value === ''
      ) {
        return;
      }

      if (
        !includePagination &&
        (key === 'page' ||
          key === 'pageSize')
      ) {
        return;
      }

      query.set(
        key,
        String(value),
      );
    },
  );

  if (includePagination) {
    if (!query.has('page')) {
      query.set('page', '1');
    }

    if (!query.has('pageSize')) {
      query.set(
        'pageSize',
        '20',
      );
    }
  }

  return query;
};

const exportReport = async (
  report:
    | 'consumption'
    | 'inventory'
    | 'traceability',
  filters: Record<string, unknown>,
  format: ReportFormat,
) => {
  const query =
    buildReportQuery(
      filters,
      false,
    );

  query.set('format', format);

  const file =
    await apiDownload(
      `/reports/${report}/export?${query.toString()}`,
    );

  saveDownloadedFile(file);
};

export const ReportService = {
  getConsumption: (
    filters:
      ConsumptionReportFilters = {},
  ) => {
    const query =
      buildReportQuery(
        filters as Record<
          string,
          unknown
        >,
      );

    return apiRequest<
      PaginatedResult<ConsumptionReportItem>
    >(
      `/reports/consumption?${query.toString()}`,
    );
  },

  getInventory: (
    filters:
      InventoryReportFilters = {},
  ) => {
    const query =
      buildReportQuery(
        filters as Record<
          string,
          unknown
        >,
      );

    return apiRequest<
      PaginatedResult<InventoryReportItem>
    >(
      `/reports/inventory?${query.toString()}`,
    );
  },

  getTraceability: (
    filters:
      TraceabilityReportFilters = {},
  ) => {
    const query =
      buildReportQuery(
        filters as Record<
          string,
          unknown
        >,
      );

    return apiRequest<
      PaginatedResult<TraceabilityReportItem>
    >(
      `/reports/traceability?${query.toString()}`,
    );
  },

  exportConsumption: (
    filters:
      ConsumptionReportFilters,
    format: ReportFormat,
  ) =>
    exportReport(
      'consumption',
      filters as Record<
        string,
        unknown
      >,
      format,
    ),

  exportInventory: (
    filters:
      InventoryReportFilters,
    format: ReportFormat,
  ) =>
    exportReport(
      'inventory',
      filters as Record<
        string,
        unknown
      >,
      format,
    ),

  exportTraceability: (
    filters:
      TraceabilityReportFilters,
    format: ReportFormat,
  ) =>
    exportReport(
      'traceability',
      filters as Record<
        string,
        unknown
      >,
      format,
    ),
};

// ================= ALERTAS =================

export const AlertService = {
  getLowInventory: (
    filters: LowInventoryAlertFilters = {},
  ) => {
    const query = new URLSearchParams();

    if (filters.estacionId) {
      query.set(
        'estacionId',
        filters.estacionId,
      );
    }

    query.set(
      'page',
      String(filters.page ?? 1),
    );

    query.set(
      'pageSize',
      String(filters.pageSize ?? 20),
    );

    return apiRequest<
      PaginatedResult<LowInventoryAlert>
    >(
      `/alerts/low-inventory?${query.toString()}`,
    );
  },
};

// ================= SOLICITUDES =================

export const RequestService = {
  create: (dto: CreateFuelRequestDto) =>
    apiRequest<FuelRequest>('/requests', {
      method: 'POST',
      body: JSON.stringify(dto),
    }),

  getAll: (filters?: FuelRequestFilter) => {
    const params = new URLSearchParams();
    if (filters?.estado) params.append('estado', filters.estado);
    if (filters?.empleadoId) params.append('empleadoId', filters.empleadoId);
    if (filters?.vehiculoId) params.append('vehiculoId', filters.vehiculoId);
    if (filters?.departamentoId) params.append('departamentoId', filters.departamentoId);
    if (filters?.fechaInicio) params.append('fechaInicio', filters.fechaInicio);
    if (filters?.fechaFin) params.append('fechaFin', filters.fechaFin);
    if (filters?.page) params.append('page', String(filters.page));
    if (filters?.pageSize) params.append('pageSize', String(filters.pageSize));
    const query = params.toString();
    return apiRequest<PaginatedResult<FuelRequest>>(
      `/requests${query ? `?${query}` : ''}`,
    );
  },

  getById: (id: string) => apiRequest<FuelRequest>(`/requests/${id}`),

  cancel: (id: string, motivo?: string) =>
    apiRequest<FuelRequest>(`/requests/${id}/cancel`, {
      method: 'POST',
      body: JSON.stringify({ motivo }),
    }),

  approve: (id: string, dto: ApproveFuelRequestDto) =>
    apiRequest<FuelRequest>(`/requests/${id}/approve`, {
      method: 'POST',
      body: JSON.stringify(dto),
    }),

  reject: (id: string, motivo: string) =>
    apiRequest<FuelRequest>(`/requests/${id}/reject`, {
      method: 'POST',
      body: JSON.stringify({ motivo }),
    }),

  getTicketQr: (ticketId: string) => apiDownload(`/tickets/${ticketId}/qr`),
};

export interface PublicTicketData {
  numeroTicket: string;
  empleado: string;
  codigoEmpleado?: string | null;
  vehiculo: string;
  placa: string;
  ficha: string;
  tipoCombustible: string;
  cantidadAutorizada: number;
  estacion: string;
  fechaExpiracion: string;
  estado: string;
  mensajeEstado: string;
  permiteDespacho: boolean;
}

export const PublicTicketService = {
  getByToken: (token: string) =>
    apiRequest<PublicTicketData>(`/public/tickets/${token}`),

  getQrImage: (token: string) =>
    apiDownload(`/public/tickets/${token}/qr`),
};