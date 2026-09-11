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
  LoginResponseData,
} from '../types';
import { apiRequest, isMockEnabled, ApiError } from './apiClient';
import { mockStorage } from './mockStorage';

// Simulación de latencia de red para pruebas de UI y estados de carga
const delay = (ms = 180) => new Promise(res => setTimeout(res, ms));

// ================= AUTH SERVICE =================
export const AuthService = {
  async login(credentials: LoginCredentials): Promise<ApiResponse<LoginResponseData>> {
    if (isMockEnabled()) {
      await delay(250);
      const users = mockStorage.getUsers();
      const user = users.find(
        u =>
          (u.username.toLowerCase() === credentials.usernameOrEmail.toLowerCase() ||
           u.email.toLowerCase() === credentials.usernameOrEmail.toLowerCase())
      );

      if (!user) {
        throw new ApiError('Credenciales incorrectas.', 401, [
          { code: 'INVALID_CREDENTIALS', detail: 'Usuario o contraseña no válidos' }
        ]);
      }

      if (!user.isActive) {
        throw new ApiError('El usuario se encuentra inactivo. Contacte al administrador.', 403, [
          { code: 'USER_INACTIVE', detail: 'Cuenta suspendida' }
        ]);
      }

      return {
        success: true,
        message: 'Autenticación exitosa',
        data: {
          accessToken: `mock-jwt-token-${user.id}-${Date.now()}`,
          expiresAt: new Date(Date.now() + 3600000).toISOString(),
          refreshToken: `mock-refresh-token-${user.id}`,
          user: {
            id: user.id,
            fullName: user.fullName,
            email: user.email,
            username: user.username,
            role: user.role,
            employeeId: user.employeeId,
            stationId: user.stationId,
            stationName: user.stationName,
          },
        },
        errors: [],
      };
    }

    return apiRequest<LoginResponseData>('/auth/login', {
      method: 'POST',
      body: JSON.stringify(credentials),
    });
  },

  async logout(): Promise<ApiResponse<null>> {
    if (isMockEnabled()) {
      await delay(100);
      return { success: true, message: 'Sesión cerrada', data: null, errors: [] };
    }
    return apiRequest<null>('/auth/logout', { method: 'POST' });
  },

  async getMe(): Promise<ApiResponse<User>> {
    if (isMockEnabled()) {
      await delay(100);
      const rawUser = localStorage.getItem('fms_auth_user');
      if (!rawUser) throw new ApiError('No autenticado', 401);
      const user = JSON.parse(rawUser) as User;
      return { success: true, message: 'Datos de usuario', data: user, errors: [] };
    }
    return apiRequest<User>('/auth/me');
  }
};

// ================= USERS SERVICE =================
export const UserService = {
  async getAll(): Promise<ApiResponse<User[]>> {
    if (isMockEnabled()) {
      await delay();
      return { success: true, message: 'Usuarios obtenidos', data: mockStorage.getUsers(), errors: [] };
    }
    return apiRequest<User[]>('/users');
  },

  async getById(id: string): Promise<ApiResponse<User>> {
    if (isMockEnabled()) {
      await delay();
      const user = mockStorage.getUserById(id);
      if (!user) throw new ApiError('Usuario no encontrado', 404);
      return { success: true, message: 'Usuario encontrado', data: user, errors: [] };
    }
    return apiRequest<User>(`/users/${id}`);
  },

  async create(dto: CreateUserDto): Promise<ApiResponse<User>> {
    if (isMockEnabled()) {
      await delay();
      try {
        const created = mockStorage.createUser(dto);
        return { success: true, message: 'Usuario creado exitosamente', data: created, errors: [] };
      } catch (err: unknown) {
        const error = err as { message: string; code?: string; field?: string };
        throw new ApiError(error.message, 400, [
          { code: error.code || 'VALIDATION_ERROR', field: error.field, detail: error.message }
        ]);
      }
    }
    return apiRequest<User>('/users', {
      method: 'POST',
      body: JSON.stringify(dto),
    });
  },

  async update(id: string, dto: UpdateUserDto): Promise<ApiResponse<User>> {
    if (isMockEnabled()) {
      await delay();
      try {
        const updated = mockStorage.updateUser(id, dto);
        return { success: true, message: 'Usuario actualizado exitosamente', data: updated, errors: [] };
      } catch (err: unknown) {
        const error = err as { message: string; code?: string; field?: string };
        throw new ApiError(error.message, 400, [
          { code: error.code || 'VALIDATION_ERROR', field: error.field, detail: error.message }
        ]);
      }
    }
    return apiRequest<User>(`/users/${id}`, {
      method: 'PATCH',
      body: JSON.stringify(dto),
    });
  },

  async toggleActive(id: string): Promise<ApiResponse<User>> {
    if (isMockEnabled()) {
      await delay();
      try {
        const updated = mockStorage.deactivateUser(id);
        const action = updated.isActive ? 'activado' : 'desactivado';
        return { success: true, message: `Usuario ${action} exitosamente`, data: updated, errors: [] };
      } catch (err: unknown) {
        const error = err as { message: string; code?: string };
        throw new ApiError(error.message, 422, [
          { code: error.code || 'BUSINESS_RULE_VIOLATION', detail: error.message }
        ]);
      }
    }
    return apiRequest<User>(`/users/${id}/deactivate`, {
      method: 'PATCH',
    });
  },
};

// ================= DEPARTMENTS SERVICE =================
export const DepartmentService = {
  async getAll(): Promise<ApiResponse<Department[]>> {
    if (isMockEnabled()) {
      await delay();
      return { success: true, message: 'Departamentos obtenidos', data: mockStorage.getDepartments(), errors: [] };
    }
    return apiRequest<Department[]>('/departments');
  },

  async create(dto: CreateDepartmentDto): Promise<ApiResponse<Department>> {
    if (isMockEnabled()) {
      await delay();
      try {
        const created = mockStorage.createDepartment(dto);
        return { success: true, message: 'Departamento registrado', data: created, errors: [] };
      } catch (err: unknown) {
        const error = err as { message: string; code?: string; field?: string };
        throw new ApiError(error.message, 400, [
          { code: error.code || 'VALIDATION_ERROR', field: error.field, detail: error.message }
        ]);
      }
    }
    return apiRequest<Department>('/departments', {
      method: 'POST',
      body: JSON.stringify(dto),
    });
  },

  async update(id: string, dto: UpdateDepartmentDto): Promise<ApiResponse<Department>> {
    if (isMockEnabled()) {
      await delay();
      const updated = mockStorage.updateDepartment(id, dto);
      return { success: true, message: 'Departamento modificado', data: updated, errors: [] };
    }
    return apiRequest<Department>(`/departments/${id}`, {
      method: 'PATCH',
      body: JSON.stringify(dto),
    });
  },

  async toggleActive(id: string): Promise<ApiResponse<Department>> {
    if (isMockEnabled()) {
      await delay();
      const updated = mockStorage.deactivateDepartment(id);
      const action = updated.isActive ? 'activado' : 'desactivado';
      return { success: true, message: `Departamento ${action}`, data: updated, errors: [] };
    }
    return apiRequest<Department>(`/departments/${id}/deactivate`, {
      method: 'PATCH',
    });
  },
};

// ================= EMPLOYEES SERVICE =================
export const EmployeeService = {
  async getAll(): Promise<ApiResponse<Employee[]>> {
    if (isMockEnabled()) {
      await delay();
      return { success: true, message: 'Empleados obtenidos', data: mockStorage.getEmployees(), errors: [] };
    }
    return apiRequest<Employee[]>('/employees');
  },

  async create(dto: CreateEmployeeDto): Promise<ApiResponse<Employee>> {
    if (isMockEnabled()) {
      await delay();
      try {
        const created = mockStorage.createEmployee(dto);
        return { success: true, message: 'Empleado registrado exitosamente', data: created, errors: [] };
      } catch (err: unknown) {
        const error = err as { message: string; code?: string; field?: string };
        throw new ApiError(error.message, 400, [
          { code: error.code || 'VALIDATION_ERROR', field: error.field, detail: error.message }
        ]);
      }
    }
    return apiRequest<Employee>('/employees', {
      method: 'POST',
      body: JSON.stringify(dto),
    });
  },

  async update(id: string, dto: UpdateEmployeeDto): Promise<ApiResponse<Employee>> {
    if (isMockEnabled()) {
      await delay();
      const updated = mockStorage.updateEmployee(id, dto);
      return { success: true, message: 'Empleado actualizado', data: updated, errors: [] };
    }
    return apiRequest<Employee>(`/employees/${id}`, {
      method: 'PATCH',
      body: JSON.stringify(dto),
    });
  },

  async toggleActive(id: string): Promise<ApiResponse<Employee>> {
    if (isMockEnabled()) {
      await delay();
      const updated = mockStorage.deactivateEmployee(id);
      const action = updated.isActive ? 'activado' : 'desactivado';
      return { success: true, message: `Empleado ${action}`, data: updated, errors: [] };
    }
    return apiRequest<Employee>(`/employees/${id}/deactivate`, {
      method: 'PATCH',
    });
  },
};

// ================= VEHICLES SERVICE =================
export const VehicleService = {
  async getAll(): Promise<ApiResponse<Vehicle[]>> {
    if (isMockEnabled()) {
      await delay();
      return { success: true, message: 'Vehículos obtenidos', data: mockStorage.getVehicles(), errors: [] };
    }
    return apiRequest<Vehicle[]>('/vehicles');
  },

  async create(dto: CreateVehicleDto): Promise<ApiResponse<Vehicle>> {
    if (isMockEnabled()) {
      await delay();
      try {
        const created = mockStorage.createVehicle(dto);
        return { success: true, message: 'Vehículo registrado correctamente', data: created, errors: [] };
      } catch (err: unknown) {
        const error = err as { message: string; code?: string; field?: string };
        throw new ApiError(error.message, 400, [
          { code: error.code || 'VALIDATION_ERROR', field: error.field, detail: error.message }
        ]);
      }
    }
    return apiRequest<Vehicle>('/vehicles', {
      method: 'POST',
      body: JSON.stringify(dto),
    });
  },

  async update(id: string, dto: UpdateVehicleDto): Promise<ApiResponse<Vehicle>> {
    if (isMockEnabled()) {
      await delay();
      try {
        const updated = mockStorage.updateVehicle(id, dto);
        return { success: true, message: 'Vehículo actualizado', data: updated, errors: [] };
      } catch (err: unknown) {
        const error = err as { message: string; code?: string; field?: string };
        throw new ApiError(error.message, 400, [
          { code: error.code || 'VALIDATION_ERROR', field: error.field, detail: error.message }
        ]);
      }
    }
    return apiRequest<Vehicle>(`/vehicles/${id}`, {
      method: 'PATCH',
      body: JSON.stringify(dto),
    });
  },

  async toggleActive(id: string): Promise<ApiResponse<Vehicle>> {
    if (isMockEnabled()) {
      await delay();
      const updated = mockStorage.deactivateVehicle(id);
      const action = updated.isActive ? 'activado' : 'desactivado';
      return { success: true, message: `Vehículo ${action}`, data: updated, errors: [] };
    }
    return apiRequest<Vehicle>(`/vehicles/${id}/deactivate`, {
      method: 'PATCH',
    });
  },
};

// ================= CATALOGS SERVICE =================
export const CatalogService = {
  async getStations(): Promise<ApiResponse<Station[]>> {
    if (isMockEnabled()) {
      await delay(50);
      return { success: true, message: 'Estaciones obtenidas', data: mockStorage.getStations(), errors: [] };
    }
    return apiRequest<Station[]>('/stations');
  },

  async getFuelTypes(): Promise<ApiResponse<FuelType[]>> {
    if (isMockEnabled()) {
      await delay(50);
      return { success: true, message: 'Tipos de combustible obtenidos', data: mockStorage.getFuelTypes(), errors: [] };
    }
    return apiRequest<FuelType[]>('/fuel-types');
  },
};

// ================= DASHBOARD SERVICE =================
export const DashboardService = {
  async getSummary(params?: { stationId?: string; from?: string; to?: string }): Promise<ApiResponse<DashboardSummary>> {
    if (isMockEnabled()) {
      await delay(200);
      return { success: true, message: 'Resumen ejecutivo cargado', data: mockStorage.getDashboard(), errors: [] };
    }
    const query = new URLSearchParams();
    if (params?.stationId) query.set('stationId', params.stationId);
    if (params?.from) query.set('from', params.from);
    if (params?.to) query.set('to', params.to);

    const queryString = query.toString() ? `?${query.toString()}` : '';
    return apiRequest<DashboardSummary>(`/dashboard${queryString}`);
  },
};
