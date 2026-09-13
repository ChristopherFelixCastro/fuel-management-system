using System.Collections.Concurrent;
using Tickets.Sandbox.Api.Application.Contracts;

namespace Tickets.Sandbox.Api.Infrastructure.ExternalServices.Stubs;

/// <summary>
/// TODO-INTEGRACIÓN: Stub temporal que simula el módulo de Maestros (mantenido por Angel / Christopher).
/// Provee datos de prueba de empleados, vehículos, departamentos y tipos de combustible.
/// </summary>
public class MasterDataStubService : IMasterDataService
{
    public static readonly Guid DefaultDepartmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DefaultEmployeeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid DefaultFuelTypeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid AlternativeFuelTypeId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid DefaultVehicleId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private readonly ConcurrentDictionary<Guid, EmployeeDto> _employees = new();
    private readonly ConcurrentDictionary<Guid, VehicleDto> _vehicles = new();
    private readonly ConcurrentDictionary<Guid, DepartmentDto> _departments = new();
    private readonly ConcurrentDictionary<Guid, FuelTypeDto> _fuelTypes = new();

    public MasterDataStubService()
    {
        // Sembrar datos por defecto
        var dept1 = new DepartmentDto(DefaultDepartmentId, "Transporte y Logística", "DEP-LOG");
        _departments[dept1.Id] = dept1;

        var fuel1 = new FuelTypeDto(DefaultFuelTypeId, "Diesel Óptimo", "DIESEL");
        var fuel2 = new FuelTypeDto(AlternativeFuelTypeId, "Gasolina Premium 95", "GAS-95");
        _fuelTypes[fuel1.Id] = fuel1;
        _fuelTypes[fuel2.Id] = fuel2;

        var emp1 = new EmployeeDto(DefaultEmployeeId, "Carlos Mendoza", "EMP-1042", DefaultDepartmentId, "carlos.mendoza@empresa.com", "+18095551234");
        _employees[emp1.Id] = emp1;

        var veh1 = new VehicleDto(DefaultVehicleId, "A-123456", DefaultFuelTypeId, 80.00m, true);
        _vehicles[veh1.Id] = veh1;
    }

    public void SeedEmployee(EmployeeDto emp) => _employees[emp.Id] = emp;
    public void SeedVehicle(VehicleDto veh) => _vehicles[veh.Id] = veh;
    public void SeedDepartment(DepartmentDto dept) => _departments[dept.Id] = dept;
    public void SeedFuelType(FuelTypeDto fuel) => _fuelTypes[fuel.Id] = fuel;

    public Task<EmployeeDto?> GetEmployeeByIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        _employees.TryGetValue(employeeId, out var employee);
        return Task.FromResult(employee);
    }

    public Task<VehicleDto?> GetVehicleByIdAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        _vehicles.TryGetValue(vehicleId, out var vehicle);
        return Task.FromResult(vehicle);
    }

    public Task<DepartmentDto?> GetDepartmentByIdAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        _departments.TryGetValue(departmentId, out var department);
        return Task.FromResult(department);
    }

    public Task<FuelTypeDto?> GetFuelTypeByIdAsync(Guid fuelTypeId, CancellationToken cancellationToken = default)
    {
        _fuelTypes.TryGetValue(fuelTypeId, out var fuelType);
        return Task.FromResult(fuelType);
    }
}
