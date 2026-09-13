namespace Tickets.Sandbox.Api.Application.Contracts;

public record EmployeeDto(Guid Id, string FullName, string EmployeeNumber, Guid DepartmentId, string Email, string? PhoneNumber);
public record VehicleDto(Guid Id, string Plate, Guid FuelTypeId, decimal TankCapacity, bool IsActive);
public record DepartmentDto(Guid Id, string Name, string Code);
public record FuelTypeDto(Guid Id, string Name, string Code);

/// <summary>
/// Contrato con el módulo de Maestros (mantenido por Angel / Christopher).
/// TODO-INTEGRACIÓN: Conectar con el servicio real de Maestros cuando esté integrado.
/// </summary>
public interface IMasterDataService
{
    Task<EmployeeDto?> GetEmployeeByIdAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<VehicleDto?> GetVehicleByIdAsync(Guid vehicleId, CancellationToken cancellationToken = default);
    Task<DepartmentDto?> GetDepartmentByIdAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<FuelTypeDto?> GetFuelTypeByIdAsync(Guid fuelTypeId, CancellationToken cancellationToken = default);
}
