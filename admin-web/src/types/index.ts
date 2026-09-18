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
