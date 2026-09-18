namespace CombustibleAPI.Application.Dtos.Admin;

// ==================== DEPARTAMENTOS ====================

public record DepartamentoAdminDto(
    Guid Id,
    string Codigo,
    string Nombre,
    string? Descripcion,
    bool Activo,
    DateTime FechaCreacion,
    DateTime? FechaActualizacion);

public record CreateDepartamentoDto(
    string Codigo,
    string Nombre,
    string? Descripcion);

public record UpdateDepartamentoDto(
    string Codigo,
    string Nombre,
    string? Descripcion,
    bool Activo);

// ==================== EMPLEADOS ====================

public record EmpleadoAdminDto(
    Guid Id,
    Guid DepartamentoId,
    string DepartamentoNombre,
    string CodigoEmpleado,
    string Nombre,
    string Apellido,
    string NombreCompleto,
    string Cedula,
    string? Cargo,
    string? Email,
    string? Telefono,
    bool Activo,
    DateTime FechaCreacion,
    DateTime? FechaActualizacion);

public record CreateEmpleadoDto(
    Guid DepartamentoId,
    string CodigoEmpleado,
    string Nombre,
    string Apellido,
    string Cedula,
    string? Cargo,
    string? Email,
    string? Telefono);

public record UpdateEmpleadoDto(
    Guid DepartamentoId,
    string CodigoEmpleado,
    string Nombre,
    string Apellido,
    string Cedula,
    string? Cargo,
    string? Email,
    string? Telefono,
    bool Activo);

// ==================== VEHÍCULOS ====================

public record VehiculoAdminDto(
    Guid Id,
    Guid DepartamentoId,
    string DepartamentoNombre,
    short TipoCombustibleId,
    string TipoCombustibleNombre,
    string Placa,
    string Ficha,
    string? Marca,
    string? Modelo,
    short? Anio,
    string? TipoVehiculo,
    decimal CapacidadTanque,
    decimal OdometroActual,
    bool Activo,
    DateTime FechaCreacion,
    DateTime? FechaActualizacion);

public record CreateVehiculoDto(
    Guid DepartamentoId,
    short TipoCombustibleId,
    string Placa,
    string Ficha,
    string? Marca,
    string? Modelo,
    short? Anio,
    string? TipoVehiculo,
    decimal CapacidadTanque,
    decimal OdometroActual);

public record UpdateVehiculoDto(
    Guid DepartamentoId,
    short TipoCombustibleId,
    string Placa,
    string Ficha,
    string? Marca,
    string? Modelo,
    short? Anio,
    string? TipoVehiculo,
    decimal CapacidadTanque,
    decimal OdometroActual,
    bool Activo);

// ==================== USUARIOS ====================

public record UsuarioAdminDto(
    Guid Id,
    string NombreUsuario,
    string Email,
    short RolId,
    string RolNombre,
    Guid? EmpleadoId,
    string? EmpleadoNombre,
    Guid? EstacionId,
    string? EstacionNombre,
    bool Activo,
    bool Bloqueado,
    short IntentosFallidos,
    DateTime? UltimoAcceso,
    DateTime FechaCreacion,
    DateTime? FechaActualizacion);

public record CreateUsuarioDto(
    string NombreUsuario,
    string Email,
    string Password,
    short RolId,
    Guid? EmpleadoId,
    Guid? EstacionId);

public record UpdateUsuarioDto(
    string NombreUsuario,
    string Email,
    short RolId,
    Guid? EmpleadoId,
    Guid? EstacionId,
    bool Activo,
    bool Bloqueado);

public record ChangeUsuarioPasswordDto(
    string NuevaPassword);

// ==================== CATÁLOGOS ====================

public record TipoCombustibleAdminDto(
    short Id,
    string Codigo,
    string Nombre,
    string? Descripcion,
    bool Activo);