// Contratos y tipos unificados para el Portal Administrativo (Angel)
// Alineados con el Core API real y el Portal Administrativo.

// ================= ROLES =================

export type UserRole =
  | 'ADMINISTRADOR'
  | 'SUPERVISOR'
  | 'DESPACHADOR'
  | 'SOLICITANTE'
  | 'AUDITOR';

export interface Role {
  id: number;
  name: UserRole;
  description?: string | null;
}

// ================= API =================

export interface ApiErrorDetail {
  code: string;
  field?: string;
  detail: string;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: ApiErrorDetail[];
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ================= AUTH =================

export interface AuthUser {
  id: string;
  fullName: string;
  email: string;
  username: string;
  role: UserRole;
  employeeId?: string | null;
  stationId?: string | null;
  stationName?: string | null;
}

export interface LoginCredentials {
  username: string;
  password: string;
}

export interface CoreLoginResponseData {
  accessToken: string;
  accessTokenExpiraEn: string;
  expiracion: string;
  refreshToken: string;
  refreshTokenExpiraEn: string;
  usuario: string;
  rol: UserRole;
  usuarioId: string;
  estacionId?: string | null;
}

export interface UserProfileData {
  id: string;
  usuario: string;
  email: string;
  rol: UserRole;
  stationId?: string | null;
  estacionId?: string | null;
  estacionNombre?: string | null;
  empleadoId?: string | null;
  empleadoNombre?: string | null;
}

// ================= USUARIOS =================

export interface User {
  id: string;
  fullName: string;
  email: string;
  username: string;
  role: UserRole;
  employeeId?: string | null;
  stationId?: string | null;
  stationName?: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface CreateUserDto {
  fullName: string;
  email: string;
  username: string;
  password: string;
  role: UserRole;
  employeeId?: string | null;
  stationId?: string | null;
}

export interface UpdateUserDto {
  fullName?: string;
  email?: string;
  username?: string;
  password?: string;
  role?: UserRole;
  employeeId?: string | null;
  stationId?: string | null;
  isActive?: boolean;
}

// ================= DEPARTAMENTOS =================

export interface Department {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface CreateDepartmentDto {
  code: string;
  name: string;
  description?: string;
}

export interface UpdateDepartmentDto {
  code?: string;
  name?: string;
  description?: string;
  isActive?: boolean;
}

// ================= EMPLEADOS =================

export interface Employee {
  id: string;
  employeeNumber: string;
  firstName: string;
  lastName: string;
  documentId?: string | null;
  email?: string | null;
  phone?: string | null;
  departmentId: string;
  departmentName?: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateEmployeeDto {
  employeeNumber: string;
  firstName: string;
  lastName: string;
  documentId?: string | null;
  email?: string | null;
  phone?: string | null;
  departmentId: string;
  isActive?: boolean;
}

export interface UpdateEmployeeDto {
  employeeNumber?: string;
  firstName?: string;
  lastName?: string;
  documentId?: string | null;
  email?: string | null;
  phone?: string | null;
  departmentId?: string;
  isActive?: boolean;
}

// ================= VEHÍCULOS =================

export interface FuelType {
  id: string;
  code: string;
  name: string;
}

export interface Vehicle {
  id: string;
  plate: string;
  assetNumber?: string | null;
  brand?: string | null;
  model?: string | null;
  year?: number | null;
  departmentId: string;
  departmentName?: string;
  fuelTypeId: string;
  fuelTypeName?: string;
  tankCapacity: number;
  currentOdometer: number;
  isActive: boolean;
  createdAt: string;
}

export interface CreateVehicleDto {
  plate: string;
  assetNumber?: string | null;
  brand?: string | null;
  model?: string | null;
  year?: number | null;
  departmentId: string;
  fuelTypeId: string;
  tankCapacity: number;
  currentOdometer: number;
  isActive?: boolean;
}

export interface UpdateVehicleDto {
  plate?: string;
  assetNumber?: string | null;
  brand?: string | null;
  model?: string | null;
  year?: number | null;
  departmentId?: string;
  fuelTypeId?: string;
  tankCapacity?: number;
  currentOdometer?: number;
  isActive?: boolean;
}

// ================= ESTACIONES =================
// Catálogo utilizado para despachadores y filtros.

export interface Station {
  id: string;
  code: string;
  name: string;
  address?: string;
  isActive: boolean;
}

// ================= DASHBOARD =================
// Estos contratos corresponden a la respuesta real de GET /dashboard.

// Inventario consolidado por tipo de combustible.
export interface DashboardInventoryByFuel {
  fuelTypeId: number;
  fuelTypeName: string;
  currentStock: number;
  totalCapacity: number;
  percentage: number;
}

// Alerta de tanque en nivel crítico.
export interface DashboardAlert {
  tankId: string;
  tankCode: string;
  tankName?: string | null;
  stationName: string;
  fuelTypeName: string;
  currentStock: number;
  criticalLevel: number;
  message: string;
}

// Consumo agregado.
// El Core utiliza la misma estructura tanto para departamentos
// como para vehículos.
export interface DashboardConsumption {
  name: string;
  gallons: number;
}

// Resumen completo retornado por GET /dashboard.
export interface DashboardSummary {
  pendingRequests: number;
  activeTickets: number;
  lowInventoryTanks: number;
  dispatchesToday: number;
  gallonsDispatchedToday: number;
  inventoryByFuel: DashboardInventoryByFuel[];
  recentAlerts: DashboardAlert[];
  consumptionByDepartment: DashboardConsumption[];
  consumptionByVehicle: DashboardConsumption[];
}

// ================= CIERRES DIARIOS =================

export type ClosureStatus =
  | 'PENDIENTE_APROBACION'
  | 'APROBADO'
  | 'RECHAZADO';

export interface Tank {
  id: string;
  stationId: string;
  stationName?: string | null;
  fuelTypeId: number;
  fuelTypeName?: string | null;
  code: string;
  name?: string | null;
}

export interface Closure {
  id: string;
  tankId: string;
  tankCode?: string | null;
  tankName?: string | null;
  stationId?: string | null;
  stationName?: string | null;
  closureDate: string;
  openingStock: number;
  totalReceipts: number;
  totalTransfersIn: number;
  totalTransfersOut: number;
  totalDispatches: number;
  totalPositiveAdjustments: number;
  totalNegativeAdjustments: number;
  theoreticalFinalStock: number;
  physicalFinalStock: number;
  difference: number;
  status: ClosureStatus;
  createdByUserId: string;
  createdBy?: string | null;
  reviewedByUserId?: string | null;
  reviewedBy?: string | null;
  createdAt: string;
  reviewedAt?: string | null;
  differenceReason?: string | null;
  rejectionReason?: string | null;
  observations?: string | null;
}

export interface ClosureFilters {
  fechaDesde?: string;
  fechaHasta?: string;
  tanqueId?: string;
  estado?: ClosureStatus;
  page?: number;
  pageSize?: number;
}

// ================= REPORTES =================

export type ReportFormat = 'pdf' | 'xlsx' | 'csv';

export interface ConsumptionReportFilters {
  fechaDesde?: string;
  fechaHasta?: string;
  tanqueId?: string;
  page?: number;
  pageSize?: number;
}

export interface InventoryReportFilters {
  tanqueId?: string;
  tipoCombustibleId?: number;
  estacionId?: string;
  page?: number;
  pageSize?: number;
}

export interface TraceabilityReportFilters {
  fechaDesde?: string;
  fechaHasta?: string;
  tanqueId?: string;
  tipoMovimiento?: string;
  page?: number;
  pageSize?: number;
}

export interface ConsumptionReportItem {
  fecha: string;
  tanqueId: string;
  tanqueCodigo: string;
  tanqueNombre?: string | null;
  tipoCombustibleId: number;
  combustible: string;
  totalDespachadoGalones: number;
  cantidadDespachos: number;
}

export interface InventoryReportItem {
  tanqueId: string;
  tanqueCodigo: string;
  tanqueNombre?: string | null;

  estacionId: string;
  estacion: string;

  tipoCombustibleId: number;
  combustible: string;

  capacidadMaximaGalones: number;
  stockActualGalones: number;
  nivelCriticoGalones: number;
  porcentajeOcupacion: number;
  estadoStock: string;
}

export interface TraceabilityReportItem {
  movimientoId: string;

  tanqueId: string;
  tanqueCodigo: string;
  tanqueNombre?: string | null;

  estacionId: string;
  estacion: string;

  tipoCombustibleId: number;
  combustible: string;

  tipoMovimiento: string;
  cantidadGalones: number;
  saldoAnteriorGalones: number;
  saldoPosteriorGalones: number;

  registradoPorUsuarioId: string;
  registradoPor: string;

  recepcionId?: string | null;
  despachoId?: string | null;
  transferenciaId?: string | null;
  ajusteId?: string | null;

  fechaMovimiento: string;
  observaciones?: string | null;
}

// ================= ALERTAS =================

export interface LowInventoryAlertFilters {
  estacionId?: string;
  page?: number;
  pageSize?: number;
}

export interface LowInventoryAlert {
  tipo: string;
  severidad: string;

  tanqueId: string;
  tanqueCodigo: string;
  tanqueNombre?: string | null;

  estacionId: string;
  estacion: string;

  tipoCombustibleId: number;
  combustible: string;

  stockActualGalones: number;
  nivelCriticoGalones: number;
  deficitGalones: number;

  mensaje: string;
}

// ================= SOLICITUDES DE COMBUSTIBLE =================

export interface FuelRequest {
  id: string;
  empleadoId: string;
  empleadoNombre?: string;
  vehiculoId: string;
  vehiculoPlaca?: string;
  vehiculoFicha?: string;
  departamentoId: string;
  departamentoNombre?: string;
  tipoCombustibleId: number;
  tipoCombustible?: string;
  cantidadSolicitada: number;
  cantidadAutorizada?: number | null;
  estado: string; // PENDIENTE | APROBADA | RECHAZADA | CANCELADA
  tipoSolicitud: string;
  fechaSolicitud: string;
  fechaRevision?: string | null;
  motivoRechazo?: string | null;
  motivoCancelacion?: string | null;
  observaciones?: string | null;
  ticketId?: string | null;
  numeroTicket?: string | null;
}

export interface CreateFuelRequestDto {
  empleadoId: string;
  vehiculoId: string;
  departamentoId: string;
  cantidadSolicitada: number;
  tipoSolicitud?: string;
  observaciones?: string;
}

export interface ApproveFuelRequestDto {
  estacionId: string;
  cantidadAutorizada: number;
  fechaExpiracion: string;
  observaciones?: string;
}

export interface RejectFuelRequestDto {
  motivo: string;
}

export interface CancelFuelRequestDto {
  motivo?: string;
}

export interface FuelRequestFilter {
  estado?: string;
  empleadoId?: string;
  vehiculoId?: string;
  departamentoId?: string;
  fechaInicio?: string;
  fechaFin?: string;
  page?: number;
  pageSize?: number;
}
