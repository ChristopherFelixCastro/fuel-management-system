import { 
  User, Department, Employee, Vehicle, DashboardSummary, Station, FuelType,
  CreateUserDto, UpdateUserDto, CreateEmployeeDto, UpdateEmployeeDto,
  CreateVehicleDto, UpdateVehicleDto, CreateDepartmentDto, UpdateDepartmentDto
} from '../types';
import { 
  INITIAL_USERS, INITIAL_DEPARTMENTS, INITIAL_EMPLOYEES, 
  INITIAL_VEHICLES, INITIAL_DASHBOARD, INITIAL_STATIONS, INITIAL_FUEL_TYPES 
} from './mockData';

const STORAGE_KEYS = {
  USERS: 'fms_mock_users',
  DEPARTMENTS: 'fms_mock_departments',
  EMPLOYEES: 'fms_mock_employees',
  VEHICLES: 'fms_mock_vehicles',
  DASHBOARD: 'fms_mock_dashboard',
  USE_MOCK: 'fms_use_mock_api',
};

class MockStorage {
  private getItem<T>(key: string, defaultValue: T): T {
    const data = localStorage.getItem(key);
    if (!data) {
      localStorage.setItem(key, JSON.stringify(defaultValue));
      return defaultValue;
    }
    try {
      return JSON.parse(data) as T;
    } catch {
      return defaultValue;
    }
  }

  private setItem<T>(key: string, value: T): void {
    localStorage.setItem(key, JSON.stringify(value));
  }

  // ================= USERS =================
  getUsers(): User[] {
    return this.getItem<User[]>(STORAGE_KEYS.USERS, INITIAL_USERS);
  }

  getUserById(id: string): User | undefined {
    return this.getUsers().find(u => u.id === id);
  }

  createUser(dto: CreateUserDto): User {
    const users = this.getUsers();
    
    // Regla: username y email únicos
    if (users.some(u => u.username.toLowerCase() === dto.username.toLowerCase())) {
      const err = new Error('El nombre de usuario ya se encuentra registrado.');
      (err as unknown as { code: string; field: string }).code = 'USERNAME_ALREADY_EXISTS';
      (err as unknown as { code: string; field: string }).field = 'username';
      throw err;
    }
    if (users.some(u => u.email.toLowerCase() === dto.email.toLowerCase())) {
      const err = new Error('El correo electrónico ya se encuentra registrado.');
      (err as unknown as { code: string; field: string }).code = 'EMAIL_ALREADY_EXISTS';
      (err as unknown as { code: string; field: string }).field = 'email';
      throw err;
    }

    // Regla: DESPACHADOR exige estación activa
    if (dto.role === 'DESPACHADOR' && !dto.stationId) {
      const err = new Error('El rol DESPACHADOR requiere obligatoriamente una estación asignada.');
      (err as unknown as { code: string; field: string }).code = 'DISPATCHER_STATION_REQUIRED';
      (err as unknown as { code: string; field: string }).field = 'stationId';
      throw err;
    }

    const station = dto.stationId ? INITIAL_STATIONS.find(s => s.id === dto.stationId) : null;

    const newUser: User = {
      id: crypto.randomUUID(),
      fullName: dto.fullName,
      email: dto.email,
      username: dto.username,
      role: dto.role,
      employeeId: dto.employeeId || null,
      stationId: dto.stationId || null,
      stationName: station ? station.name : null,
      isActive: true,
      createdAt: new Date().toISOString(),
    };

    users.unshift(newUser);
    this.setItem(STORAGE_KEYS.USERS, users);
    return newUser;
  }

  updateUser(id: string, dto: UpdateUserDto): User {
    const users = this.getUsers();
    const index = users.findIndex(u => u.id === id);
    if (index === -1) throw new Error('Usuario no encontrado');

    const current = users[index];

    // Regla: DESPACHADOR exige estación activa
    const targetRole = dto.role || current.role;
    const targetStation = dto.stationId !== undefined ? dto.stationId : current.stationId;
    if (targetRole === 'DESPACHADOR' && !targetStation) {
      const err = new Error('El rol DESPACHADOR requiere obligatoriamente una estación asignada.');
      (err as unknown as { code: string; field: string }).code = 'DISPATCHER_STATION_REQUIRED';
      (err as unknown as { code: string; field: string }).field = 'stationId';
      throw err;
    }

    const station = targetStation ? INITIAL_STATIONS.find(s => s.id === targetStation) : null;

    const updated: User = {
      ...current,
      ...dto,
      stationName: targetRole === 'DESPACHADOR' && station ? station.name : null,
      stationId: targetRole === 'DESPACHADOR' ? targetStation : null,
    };

    users[index] = updated;
    this.setItem(STORAGE_KEYS.USERS, users);
    return updated;
  }

  deactivateUser(id: string): User {
    const users = this.getUsers();
    const index = users.findIndex(u => u.id === id);
    if (index === -1) throw new Error('Usuario no encontrado');

    // Regla: LAST_ADMIN_PROTECTED
    const activeAdmins = users.filter(u => u.role === 'ADMINISTRADOR' && u.isActive);
    if (users[index].role === 'ADMINISTRADOR' && users[index].isActive && activeAdmins.length <= 1) {
      const err = new Error('No es posible desactivar al último Administrador activo del sistema.');
      (err as unknown as { code: string }).code = 'LAST_ADMIN_PROTECTED';
      throw err;
    }

    users[index].isActive = !users[index].isActive;
    this.setItem(STORAGE_KEYS.USERS, users);
    return users[index];
  }

  // ================= DEPARTAMENTOS =================
  getDepartments(): Department[] {
    return this.getItem<Department[]>(STORAGE_KEYS.DEPARTMENTS, INITIAL_DEPARTMENTS);
  }

  getDepartmentById(id: string): Department | undefined {
    return this.getDepartments().find(d => d.id === id);
  }

  createDepartment(dto: CreateDepartmentDto): Department {
    const departments = this.getDepartments();
    if (departments.some(d => d.code.toLowerCase() === dto.code.toLowerCase())) {
      const err = new Error('Ya existe un departamento con ese código.');
      (err as unknown as { code: string; field: string }).code = 'CODE_ALREADY_EXISTS';
      (err as unknown as { code: string; field: string }).field = 'code';
      throw err;
    }

    const newDep: Department = {
      id: crypto.randomUUID(),
      code: dto.code.toUpperCase(),
      name: dto.name,
      description: dto.description || null,
      isActive: true,
      createdAt: new Date().toISOString(),
    };

    departments.unshift(newDep);
    this.setItem(STORAGE_KEYS.DEPARTMENTS, departments);
    return newDep;
  }

  updateDepartment(id: string, dto: UpdateDepartmentDto): Department {
    const departments = this.getDepartments();
    const index = departments.findIndex(d => d.id === id);
    if (index === -1) throw new Error('Departamento no encontrado');

    const updated: Department = {
      ...departments[index],
      ...dto,
      code: dto.code ? dto.code.toUpperCase() : departments[index].code,
    };

    departments[index] = updated;
    this.setItem(STORAGE_KEYS.DEPARTMENTS, departments);
    return updated;
  }

  deactivateDepartment(id: string): Department {
    const departments = this.getDepartments();
    const index = departments.findIndex(d => d.id === id);
    if (index === -1) throw new Error('Departamento no encontrado');

    departments[index].isActive = !departments[index].isActive;
    this.setItem(STORAGE_KEYS.DEPARTMENTS, departments);
    return departments[index];
  }

  // ================= EMPLEADOS =================
  getEmployees(): Employee[] {
    const employees = this.getItem<Employee[]>(STORAGE_KEYS.EMPLOYEES, INITIAL_EMPLOYEES);
    const deps = this.getDepartments();
    // Resolver nombres de departamento
    return employees.map(emp => ({
      ...emp,
      departmentName: deps.find(d => d.id === emp.departmentId)?.name || 'Sin departamento',
    }));
  }

  getEmployeeById(id: string): Employee | undefined {
    return this.getEmployees().find(e => e.id === id);
  }

  createEmployee(dto: CreateEmployeeDto): Employee {
    const employees = this.getEmployees();
    if (employees.some(e => e.employeeNumber.toLowerCase() === dto.employeeNumber.toLowerCase())) {
      const err = new Error('El número de empleado ya se encuentra asignado.');
      (err as unknown as { code: string; field: string }).code = 'EMPLOYEE_NUMBER_EXISTS';
      (err as unknown as { code: string; field: string }).field = 'employeeNumber';
      throw err;
    }

    const deps = this.getDepartments();
    const dep = deps.find(d => d.id === dto.departmentId);

    const newEmp: Employee = {
      id: crypto.randomUUID(),
      employeeNumber: dto.employeeNumber.toUpperCase(),
      firstName: dto.firstName,
      lastName: dto.lastName,
      documentId: dto.documentId || null,
      email: dto.email || null,
      phone: dto.phone || null,
      departmentId: dto.departmentId,
      departmentName: dep ? dep.name : '',
      isActive: dto.isActive !== undefined ? dto.isActive : true,
      createdAt: new Date().toISOString(),
    };

    employees.unshift(newEmp);
    this.setItem(STORAGE_KEYS.EMPLOYEES, employees);
    return newEmp;
  }

  updateEmployee(id: string, dto: UpdateEmployeeDto): Employee {
    const employees = this.getEmployees();
    const index = employees.findIndex(e => e.id === id);
    if (index === -1) throw new Error('Empleado no encontrado');

    const deps = this.getDepartments();
    const depId = dto.departmentId || employees[index].departmentId;
    const dep = deps.find(d => d.id === depId);

    const updated: Employee = {
      ...employees[index],
      ...dto,
      employeeNumber: dto.employeeNumber ? dto.employeeNumber.toUpperCase() : employees[index].employeeNumber,
      departmentName: dep ? dep.name : employees[index].departmentName,
    };

    employees[index] = updated;
    this.setItem(STORAGE_KEYS.EMPLOYEES, employees);
    return updated;
  }

  deactivateEmployee(id: string): Employee {
    const employees = this.getEmployees();
    const index = employees.findIndex(e => e.id === id);
    if (index === -1) throw new Error('Empleado no encontrado');

    employees[index].isActive = !employees[index].isActive;
    this.setItem(STORAGE_KEYS.EMPLOYEES, employees);
    return employees[index];
  }

  // ================= VEHICULOS =================
  getVehicles(): Vehicle[] {
    const vehicles = this.getItem<Vehicle[]>(STORAGE_KEYS.VEHICLES, INITIAL_VEHICLES);
    const deps = this.getDepartments();
    const fuels = INITIAL_FUEL_TYPES;

    return vehicles.map(v => ({
      ...v,
      departmentName: deps.find(d => d.id === v.departmentId)?.name || 'Sin departamento',
      fuelTypeName: fuels.find(f => f.id === v.fuelTypeId)?.name || 'Desconocido',
    }));
  }

  getVehicleById(id: string): Vehicle | undefined {
    return this.getVehicles().find(v => v.id === id);
  }

  createVehicle(dto: CreateVehicleDto): Vehicle {
    const vehicles = this.getVehicles();

    // Regla: Placa única
    if (vehicles.some(v => v.plate.toUpperCase() === dto.plate.toUpperCase())) {
      const err = new Error('La placa especificada ya existe en el sistema.');
      (err as unknown as { code: string; field: string }).code = 'PLATE_ALREADY_EXISTS';
      (err as unknown as { code: string; field: string }).field = 'plate';
      throw err;
    }

    // Regla: Tipo de combustible obligatorio
    if (!dto.fuelTypeId) {
      const err = new Error('El tipo de combustible es obligatorio.');
      (err as unknown as { code: string; field: string }).code = 'FUEL_TYPE_REQUIRED';
      (err as unknown as { code: string; field: string }).field = 'fuelTypeId';
      throw err;
    }

    // Regla: Capacidad positiva
    if (dto.tankCapacity <= 0) {
      const err = new Error('La capacidad del tanque debe ser mayor a 0 galones.');
      (err as unknown as { code: string; field: string }).code = 'INVALID_TANK_CAPACITY';
      (err as unknown as { code: string; field: string }).field = 'tankCapacity';
      throw err;
    }

    // Regla: Odómetro no negativo
    if (dto.currentOdometer < 0) {
      const err = new Error('El odómetro no puede ser negativo.');
      (err as unknown as { code: string; field: string }).code = 'ODOMETER_INVALID';
      (err as unknown as { code: string; field: string }).field = 'currentOdometer';
      throw err;
    }

    const deps = this.getDepartments();
    const fuels = INITIAL_FUEL_TYPES;

    const newVehicle: Vehicle = {
      id: crypto.randomUUID(),
      plate: dto.plate.toUpperCase().trim(),
      assetNumber: dto.assetNumber ? dto.assetNumber.toUpperCase().trim() : null,
      brand: dto.brand || null,
      model: dto.model || null,
      year: dto.year || null,
      departmentId: dto.departmentId,
      departmentName: deps.find(d => d.id === dto.departmentId)?.name || '',
      fuelTypeId: dto.fuelTypeId,
      fuelTypeName: fuels.find(f => f.id === dto.fuelTypeId)?.name || '',
      tankCapacity: Number(dto.tankCapacity),
      currentOdometer: Number(dto.currentOdometer),
      isActive: dto.isActive !== undefined ? dto.isActive : true,
      createdAt: new Date().toISOString(),
    };

    vehicles.unshift(newVehicle);
    this.setItem(STORAGE_KEYS.VEHICLES, vehicles);
    return newVehicle;
  }

  updateVehicle(id: string, dto: UpdateVehicleDto): Vehicle {
    const vehicles = this.getVehicles();
    const index = vehicles.findIndex(v => v.id === id);
    if (index === -1) throw new Error('Vehículo no encontrado');

    const current = vehicles[index];

    if (dto.plate && dto.plate.toUpperCase() !== current.plate.toUpperCase()) {
      if (vehicles.some(v => v.id !== id && v.plate.toUpperCase() === dto.plate!.toUpperCase())) {
        const err = new Error('La placa especificada ya existe en otro vehículo.');
        (err as unknown as { code: string; field: string }).code = 'PLATE_ALREADY_EXISTS';
        (err as unknown as { code: string; field: string }).field = 'plate';
        throw err;
      }
    }

    if (dto.tankCapacity !== undefined && dto.tankCapacity <= 0) {
      const err = new Error('La capacidad del tanque debe ser mayor a 0 galones.');
      (err as unknown as { code: string; field: string }).code = 'INVALID_TANK_CAPACITY';
      (err as unknown as { code: string; field: string }).field = 'tankCapacity';
      throw err;
    }

    if (dto.currentOdometer !== undefined && dto.currentOdometer < current.currentOdometer) {
      const err = new Error('El odómetro no puede ser menor al kilometraje registrado anteriormente.');
      (err as unknown as { code: string; field: string }).code = 'ODOMETER_INVALID';
      (err as unknown as { code: string; field: string }).field = 'currentOdometer';
      throw err;
    }

    const deps = this.getDepartments();
    const fuels = INITIAL_FUEL_TYPES;
    const depId = dto.departmentId || current.departmentId;
    const fuelId = dto.fuelTypeId || current.fuelTypeId;

    const updated: Vehicle = {
      ...current,
      ...dto,
      plate: dto.plate ? dto.plate.toUpperCase().trim() : current.plate,
      departmentName: deps.find(d => d.id === depId)?.name || current.departmentName,
      fuelTypeName: fuels.find(f => f.id === fuelId)?.name || current.fuelTypeName,
    };

    vehicles[index] = updated;
    this.setItem(STORAGE_KEYS.VEHICLES, vehicles);
    return updated;
  }

  deactivateVehicle(id: string): Vehicle {
    const vehicles = this.getVehicles();
    const index = vehicles.findIndex(v => v.id === id);
    if (index === -1) throw new Error('Vehículo no encontrado');

    vehicles[index].isActive = !vehicles[index].isActive;
    this.setItem(STORAGE_KEYS.VEHICLES, vehicles);
    return vehicles[index];
  }

  // ================= CATÁLOGOS =================
  getStations(): Station[] {
    return INITIAL_STATIONS;
  }

  getFuelTypes(): FuelType[] {
    return INITIAL_FUEL_TYPES;
  }

  // ================= DASHBOARD =================
  getDashboard(): DashboardSummary {
    return this.getItem<DashboardSummary>(STORAGE_KEYS.DASHBOARD, INITIAL_DASHBOARD);
  }

  // Reset a datos de fábrica
  resetAll(): void {
    localStorage.removeItem(STORAGE_KEYS.USERS);
    localStorage.removeItem(STORAGE_KEYS.DEPARTMENTS);
    localStorage.removeItem(STORAGE_KEYS.EMPLOYEES);
    localStorage.removeItem(STORAGE_KEYS.VEHICLES);
    localStorage.removeItem(STORAGE_KEYS.DASHBOARD);
  }
}

export const mockStorage = new MockStorage();
