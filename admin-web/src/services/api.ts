import { ApiResponse, User, CreateUserDto, UpdateUserDto, Department, CreateDepartmentDto, UpdateDepartmentDto, Employee, CreateEmployeeDto, UpdateEmployeeDto, Vehicle, CreateVehicleDto, UpdateVehicleDto, DashboardSummary, Station, FuelType, LoginCredentials, CoreLoginResponseData, UserProfileData, Role } from '../types';
import { apiRequest } from './apiClient';

type CoreDepartment = { id: string; codigo: string; nombre: string; descripcion?: string | null; activo: boolean; fechaCreacion: string };
type CoreEmployee = { id: string; departamentoId: string; departamentoNombre: string; codigoEmpleado: string; nombre: string; apellido: string; cedula: string; email?: string | null; telefono?: string | null; activo: boolean; fechaCreacion: string };
type CoreVehicle = { id: string; departamentoId: string; departamentoNombre: string; tipoCombustibleId: number; tipoCombustibleNombre: string; placa: string; ficha: string; marca?: string | null; modelo?: string | null; anio?: number | null; capacidadTanque: number; odometroActual: number; activo: boolean; fechaCreacion: string };
type CoreUser = { id: string; nombreUsuario: string; email: string; rolId: number; rolNombre: User['role']; empleadoId?: string | null; empleadoNombre?: string | null; estacionId?: string | null; estacionNombre?: string | null; activo: boolean; fechaCreacion: string };

const mapDepartment = (x: CoreDepartment): Department => ({ id: x.id, code: x.codigo, name: x.nombre, description: x.descripcion, isActive: x.activo, createdAt: x.fechaCreacion });
const mapEmployee = (x: CoreEmployee): Employee => ({ id: x.id, employeeNumber: x.codigoEmpleado, firstName: x.nombre, lastName: x.apellido, documentId: x.cedula, email: x.email, phone: x.telefono, departmentId: x.departamentoId, departmentName: x.departamentoNombre, isActive: x.activo, createdAt: x.fechaCreacion });
const mapVehicle = (x: CoreVehicle): Vehicle => ({ id: x.id, plate: x.placa, assetNumber: x.ficha, brand: x.marca, model: x.modelo, year: x.anio, departmentId: x.departamentoId, departmentName: x.departamentoNombre, fuelTypeId: String(x.tipoCombustibleId), fuelTypeName: x.tipoCombustibleNombre, tankCapacity: x.capacidadTanque, currentOdometer: x.odometroActual, isActive: x.activo, createdAt: x.fechaCreacion });
const mapUser = (x: CoreUser): User => ({ id: x.id, fullName: x.empleadoNombre || x.nombreUsuario, email: x.email, username: x.nombreUsuario, role: x.rolNombre, employeeId: x.empleadoId, stationId: x.estacionId, stationName: x.estacionNombre, isActive: x.activo, createdAt: x.fechaCreacion });
const mapped = <T, R>(response: ApiResponse<T>, mapper: (value: T) => R): ApiResponse<R> => ({ ...response, data: mapper(response.data) });
const roleId = (role: User['role']) => ({ ADMINISTRADOR: 1, SUPERVISOR: 2, DESPACHADOR: 3, SOLICITANTE: 4, AUDITOR: 5 } as const)[role];
const employeePayload = (dto: CreateEmployeeDto | UpdateEmployeeDto) => ({ departamentoId: dto.departmentId, codigoEmpleado: dto.employeeNumber, nombre: dto.firstName, apellido: dto.lastName, cedula: dto.documentId || null, cargo: null, email: dto.email || null, telefono: dto.phone || null });
const vehiclePayload = (dto: CreateVehicleDto | UpdateVehicleDto) => ({ departamentoId: dto.departmentId, tipoCombustibleId: Number(dto.fuelTypeId), placa: dto.plate, ficha: dto.assetNumber || '', marca: dto.brand || null, modelo: dto.model || null, anio: dto.year || null, tipoVehiculo: null, capacidadTanque: dto.tankCapacity, odometroActual: dto.currentOdometer });

export const AuthService = {
  login: (credentials: LoginCredentials) => apiRequest<CoreLoginResponseData>('/auth/login', { method: 'POST', body: JSON.stringify(credentials) }),
  logout: (refreshToken: string) => apiRequest<{ message: string }>('/auth/logout', { method: 'POST', body: JSON.stringify({ refreshToken }) }),
  getMe: () => apiRequest<UserProfileData>('/auth/me'),
};
export const UserService = {
  async getAll() { return mapped(await apiRequest<CoreUser[]>('/users?incluirInactivos=true'), xs => xs.map(mapUser)); },
  async getById(id: string) { return mapped(await apiRequest<CoreUser>(`/users/${id}`), mapUser); },
  async create(dto: CreateUserDto) { return mapped(await apiRequest<CoreUser>('/users', { method: 'POST', body: JSON.stringify({ nombreUsuario: dto.username, email: dto.email, password: dto.password, rolId: roleId(dto.role), empleadoId: dto.employeeId || null, estacionId: dto.role === 'DESPACHADOR' ? dto.stationId || null : null }) }), mapUser); },
  async update(id: string, dto: UpdateUserDto) { const response = await apiRequest<CoreUser>(`/users/${id}`, { method: 'PUT', body: JSON.stringify({ nombreUsuario: dto.username, email: dto.email, rolId: roleId(dto.role!), empleadoId: dto.employeeId || null, estacionId: dto.role === 'DESPACHADOR' ? dto.stationId || null : null, activo: dto.isActive ?? true, bloqueado: false }) }); if (dto.password) await apiRequest<null>(`/users/${id}/password`, { method: 'PATCH', body: JSON.stringify({ nuevaPassword: dto.password }) }); return mapped(response, mapUser); },
  deactivate: (id: string) => apiRequest<null>(`/users/${id}/deactivate`, { method: 'PATCH' }),
};
export const DepartmentService = {
  async getAll() { return mapped(await apiRequest<CoreDepartment[]>('/departments?incluirInactivos=true'), xs => xs.map(mapDepartment)); },
  async create(dto: CreateDepartmentDto) { return mapped(await apiRequest<CoreDepartment>('/departments', { method: 'POST', body: JSON.stringify({ codigo: dto.code, nombre: dto.name, descripcion: dto.description || null }) }), mapDepartment); },
  async update(id: string, dto: UpdateDepartmentDto) { return mapped(await apiRequest<CoreDepartment>(`/departments/${id}`, { method: 'PUT', body: JSON.stringify({ codigo: dto.code, nombre: dto.name, descripcion: dto.description || null, activo: dto.isActive ?? true }) }), mapDepartment); },
  deactivate: (id: string) => apiRequest<null>(`/departments/${id}/deactivate`, { method: 'PATCH' }),
};
export const EmployeeService = {
  async getAll() { return mapped(await apiRequest<CoreEmployee[]>('/employees?incluirInactivos=true'), xs => xs.map(mapEmployee)); },
  async create(dto: CreateEmployeeDto) { return mapped(await apiRequest<CoreEmployee>('/employees', { method: 'POST', body: JSON.stringify(employeePayload(dto)) }), mapEmployee); },
  async update(id: string, dto: UpdateEmployeeDto) { return mapped(await apiRequest<CoreEmployee>(`/employees/${id}`, { method: 'PUT', body: JSON.stringify({ ...employeePayload(dto), activo: dto.isActive ?? true }) }), mapEmployee); },
  deactivate: (id: string) => apiRequest<null>(`/employees/${id}/deactivate`, { method: 'PATCH' }),
};
export const VehicleService = {
  async getAll() { return mapped(await apiRequest<CoreVehicle[]>('/vehicles?incluirInactivos=true'), xs => xs.map(mapVehicle)); },
  async create(dto: CreateVehicleDto) { return mapped(await apiRequest<CoreVehicle>('/vehicles', { method: 'POST', body: JSON.stringify(vehiclePayload(dto)) }), mapVehicle); },
  async update(id: string, dto: UpdateVehicleDto) { return mapped(await apiRequest<CoreVehicle>(`/vehicles/${id}`, { method: 'PUT', body: JSON.stringify({ ...vehiclePayload(dto), activo: dto.isActive ?? true }) }), mapVehicle); },
  deactivate: (id: string) => apiRequest<null>(`/vehicles/${id}/deactivate`, { method: 'PATCH' }),
};
export const CatalogService = {
  async getRoles() { return mapped(await apiRequest<Array<{ id: number; nombre: User['role']; descripcion?: string | null }>>('/masters/roles'), xs => xs.map(x => ({ id: x.id, name: x.nombre, description: x.descripcion } as Role))); },
  async getStations() { return mapped(await apiRequest<Array<{ id: string; codigo: string; nombre: string; direccion?: string; activo: boolean }>>('/masters/estaciones'), xs => xs.map(x => ({ id: x.id, code: x.codigo, name: x.nombre, address: x.direccion, isActive: x.activo } as Station))); },
  async getFuelTypes() { return mapped(await apiRequest<Array<{ id: number; codigo: string; nombre: string }>>('/masters/fuel-types'), xs => xs.map(x => ({ id: String(x.id), code: x.codigo, name: x.nombre } as FuelType))); },
};
export const DashboardService = {
  getSummary: (params?: {
    stationId?: string;
    from?: string;
    to?: string;
  }) => {
    const query = new URLSearchParams();

    if (params?.stationId) {
      query.set('stationId', params.stationId);
    }

    if (params?.from) {
      query.set('from', params.from);
    }

    if (params?.to) {
      query.set('to', params.to);
    }

    const queryString = query.toString();

    return apiRequest<DashboardSummary>(
      `/dashboard${queryString ? `?${queryString}` : ''}`
    );
  },
};
