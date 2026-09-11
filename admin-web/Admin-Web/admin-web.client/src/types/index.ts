// Contratos y tipos unificados para el Portal Administrativo (Angel)
// Alineados con Contrato_Tecnico_y_Cronograma_Integracion.html y SDP_Individual_Angel_Portal_Administrativo.html

export type UserRole = 'ADMINISTRADOR' | 'SUPERVISOR' | 'DESPACHADOR' | 'SOLICITANTE' | 'AUDITOR';

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
  usernameOrEmail: string;
  password: string;
}

export interface LoginResponseData {
  accessToken: string;
  expiresAt: string;
  refreshToken: string;
  user: AuthUser;
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

// ================= VEHICULOS =================
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
  tankCapacity: number; // Galones
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

// ================= ESTACIONES (Catálogo para despachadores y filtros) =================
export interface Station {
  id: string;
  code: string;
  name: string;
  address?: string;
  isActive: boolean;
}

// ================= DASHBOARD =================
export interface InventoryByFuel {
  fuelTypeId: string;
  fuelTypeName: string;
  physicalQuantity: number;
  reservedQuantity: number;
  availableQuantity: number;
  criticalLevel: number;
  isLowInventory: boolean;
}

export interface RecentAlert {
  id: string;
  type: 'NEAR_EXPIRY' | 'EXPIRED' | 'LOW_INVENTORY' | 'INTEGRATION_FAILURE' | 'ADJUSTMENT_PENDING';
  severity: 'INFO' | 'WARNING' | 'CRITICAL';
  message: string;
  createdAt: string;
}

export interface DepartmentConsumption {
  departmentName: string;
  gallons: number;
  percentage: number;
}

export interface VehicleConsumption {
  plate: string;
  vehicleModel: string;
  departmentName: string;
  gallons: number;
}

export interface DashboardSummary {
  pendingRequests: number;
  activeTickets: number;
  lowInventoryTanks: number;
  dispatchesToday: number;
  gallonsDispatchedToday: number;
  inventoryByFuel: InventoryByFuel[];
  recentAlerts: RecentAlert[];
  consumptionByDepartment: DepartmentConsumption[];
  consumptionByVehicle: VehicleConsumption[];
}
