using CombustibleAPI.Application.Dtos.Admin;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Domain.Entities;
using CombustibleAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CombustibleAPI.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public AdminService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    // =========================================================
    // DEPARTAMENTOS
    // =========================================================

    public async Task<List<DepartamentoAdminDto>> GetDepartamentosAsync(
        bool incluirInactivos, CancellationToken ct)
    {
        var query = _db.Departamentos.AsNoTracking().AsQueryable();

        if (!incluirInactivos)
            query = query.Where(x => x.Activo);

        return await query
            .OrderBy(x => x.Nombre)
            .Select(x => new DepartamentoAdminDto(
                x.Id, x.Codigo, x.Nombre, x.Descripcion,
                x.Activo, x.FechaCreacion, x.FechaActualizacion))
            .ToListAsync(ct);
    }

    public async Task<DepartamentoAdminDto> GetDepartamentoAsync(
        Guid id, CancellationToken ct)
    {
        var x = await _db.Departamentos.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiException.NotFound("Departamento");

        return MapDepartamento(x);
    }

    public async Task<DepartamentoAdminDto> CreateDepartamentoAsync(
        CreateDepartamentoDto request, Guid usuarioId, CancellationToken ct)
    {
        var codigo = Required(request.Codigo, "El código es obligatorio.");
        var nombre = Required(request.Nombre, "El nombre es obligatorio.");

        Max(codigo, 20, "El código");
        Max(nombre, 100, "El nombre");
        MaxOptional(request.Descripcion, 250, "La descripción");

        if (await _db.Departamentos.AnyAsync(
                x => x.Codigo.ToLower() == codigo.ToLower(), ct))
            throw ApiException.Conflict(
                "DEPARTMENT_CODE_EXISTS",
                "Ya existe un departamento con ese código.");

        if (await _db.Departamentos.AnyAsync(
                x => x.Nombre.ToLower() == nombre.ToLower(), ct))
            throw ApiException.Conflict(
                "DEPARTMENT_NAME_EXISTS",
                "Ya existe un departamento con ese nombre.");

        var entity = new Departamento
        {
            Id = Guid.NewGuid(),
            Codigo = codigo,
            Nombre = nombre,
            Descripcion = Optional(request.Descripcion),
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _db.Departamentos.Add(entity);
        await _db.SaveChangesAsync(ct);

        await Audit(usuarioId, "CREAR", "Departamento", entity.Id,
            null,
            new { entity.Codigo, entity.Nombre, entity.Descripcion, entity.Activo },
            ct);

        return MapDepartamento(entity);
    }

    public async Task<DepartamentoAdminDto> UpdateDepartamentoAsync(
        Guid id, UpdateDepartamentoDto request,
        Guid usuarioId, CancellationToken ct)
    {
        var entity = await _db.Departamentos.FindAsync([id], ct)
            ?? throw ApiException.NotFound("Departamento");

        var codigo = Required(request.Codigo, "El código es obligatorio.");
        var nombre = Required(request.Nombre, "El nombre es obligatorio.");

        Max(codigo, 20, "El código");
        Max(nombre, 100, "El nombre");
        MaxOptional(request.Descripcion, 250, "La descripción");

        if (await _db.Departamentos.AnyAsync(
                x => x.Id != id &&
                     x.Codigo.ToLower() == codigo.ToLower(), ct))
            throw ApiException.Conflict(
                "DEPARTMENT_CODE_EXISTS",
                "Ya existe otro departamento con ese código.");

        if (await _db.Departamentos.AnyAsync(
                x => x.Id != id &&
                     x.Nombre.ToLower() == nombre.ToLower(), ct))
            throw ApiException.Conflict(
                "DEPARTMENT_NAME_EXISTS",
                "Ya existe otro departamento con ese nombre.");

        var previous = new
        {
            entity.Codigo,
            entity.Nombre,
            entity.Descripcion,
            entity.Activo
        };

        entity.Codigo = codigo;
        entity.Nombre = nombre;
        entity.Descripcion = Optional(request.Descripcion);
        entity.Activo = request.Activo;
        entity.FechaActualizacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await Audit(usuarioId, "ACTUALIZAR", "Departamento", id,
            previous,
            new { entity.Codigo, entity.Nombre, entity.Descripcion, entity.Activo },
            ct);

        return MapDepartamento(entity);
    }

    public async Task DeactivateDepartamentoAsync(
        Guid id, Guid usuarioId, CancellationToken ct)
    {
        var entity = await _db.Departamentos.FindAsync([id], ct)
            ?? throw ApiException.NotFound("Departamento");

        if (!entity.Activo)
            return;

        if (await _db.Empleados.AnyAsync(
                x => x.DepartamentoId == id && x.Activo, ct) ||
            await _db.Vehiculos.AnyAsync(
                x => x.DepartamentoId == id && x.Activo, ct))
            throw ApiException.BusinessRule(
                "DEPARTMENT_IN_USE",
                "No se puede desactivar el departamento porque posee empleados o vehículos activos.");

        entity.Activo = false;
        entity.FechaActualizacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await Audit(usuarioId, "DESACTIVAR", "Departamento", id,
            new { Activo = true },
            new { Activo = false },
            ct);
    }

    // =========================================================
    // EMPLEADOS
    // =========================================================

    public async Task<List<EmpleadoAdminDto>> GetEmpleadosAsync(
        bool incluirInactivos, CancellationToken ct)
    {
        var query = _db.Empleados.AsNoTracking()
            .Include(x => x.Departamento)
            .AsQueryable();

        if (!incluirInactivos)
            query = query.Where(x => x.Activo);

        return await query.OrderBy(x => x.Nombre)
            .ThenBy(x => x.Apellido)
            .Select(x => new EmpleadoAdminDto(
                x.Id,
                x.DepartamentoId,
                x.Departamento.Nombre,
                x.CodigoEmpleado,
                x.Nombre,
                x.Apellido,
                (x.Nombre + " " + x.Apellido).Trim(),
                x.Cedula,
                x.Cargo,
                x.Email,
                x.Telefono,
                x.Activo,
                x.FechaCreacion,
                x.FechaActualizacion))
            .ToListAsync(ct);
    }

    public async Task<EmpleadoAdminDto> GetEmpleadoAsync(
        Guid id, CancellationToken ct)
    {
        var x = await _db.Empleados.AsNoTracking()
            .Include(x => x.Departamento)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiException.NotFound("Empleado");

        return MapEmpleado(x);
    }

    public async Task<EmpleadoAdminDto> CreateEmpleadoAsync(
        CreateEmpleadoDto request, Guid usuarioId, CancellationToken ct)
    {
        await ValidateDepartamento(request.DepartamentoId, ct);

        var codigo = Required(request.CodigoEmpleado,
            "El código de empleado es obligatorio.");
        var nombre = Required(request.Nombre, "El nombre es obligatorio.");
        var apellido = Required(request.Apellido, "El apellido es obligatorio.");
        var cedula = Required(request.Cedula, "La cédula es obligatoria.");

        ValidateEmpleadoLengths(
            codigo, nombre, apellido, cedula,
            request.Cargo, request.Email, request.Telefono);

        await ValidateEmpleadoUnique(
            null, codigo, cedula, Optional(request.Email), ct);

        var entity = new Empleado
        {
            Id = Guid.NewGuid(),
            DepartamentoId = request.DepartamentoId,
            CodigoEmpleado = codigo,
            Nombre = nombre,
            Apellido = apellido,
            Cedula = cedula,
            Cargo = Optional(request.Cargo),
            Email = Optional(request.Email),
            Telefono = Optional(request.Telefono),
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _db.Empleados.Add(entity);
        await _db.SaveChangesAsync(ct);

        await Audit(usuarioId, "CREAR", "Empleado", entity.Id,
            null,
            new
            {
                entity.DepartamentoId,
                entity.CodigoEmpleado,
                entity.Nombre,
                entity.Apellido,
                entity.Cedula,
                entity.Cargo,
                entity.Email,
                entity.Telefono,
                entity.Activo
            }, ct);

        return await GetEmpleadoAsync(entity.Id, ct);
    }

    public async Task<EmpleadoAdminDto> UpdateEmpleadoAsync(
        Guid id, UpdateEmpleadoDto request,
        Guid usuarioId, CancellationToken ct)
    {
        var entity = await _db.Empleados.FindAsync([id], ct)
            ?? throw ApiException.NotFound("Empleado");

        await ValidateDepartamento(request.DepartamentoId, ct);

        var codigo = Required(request.CodigoEmpleado,
            "El código de empleado es obligatorio.");
        var nombre = Required(request.Nombre, "El nombre es obligatorio.");
        var apellido = Required(request.Apellido, "El apellido es obligatorio.");
        var cedula = Required(request.Cedula, "La cédula es obligatoria.");
        var email = Optional(request.Email);

        ValidateEmpleadoLengths(
            codigo, nombre, apellido, cedula,
            request.Cargo, email, request.Telefono);

        await ValidateEmpleadoUnique(id, codigo, cedula, email, ct);

        var previous = new
        {
            entity.DepartamentoId,
            entity.CodigoEmpleado,
            entity.Nombre,
            entity.Apellido,
            entity.Cedula,
            entity.Cargo,
            entity.Email,
            entity.Telefono,
            entity.Activo
        };

        entity.DepartamentoId = request.DepartamentoId;
        entity.CodigoEmpleado = codigo;
        entity.Nombre = nombre;
        entity.Apellido = apellido;
        entity.Cedula = cedula;
        entity.Cargo = Optional(request.Cargo);
        entity.Email = email;
        entity.Telefono = Optional(request.Telefono);
        entity.Activo = request.Activo;
        entity.FechaActualizacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await Audit(usuarioId, "ACTUALIZAR", "Empleado", id,
            previous,
            new
            {
                entity.DepartamentoId,
                entity.CodigoEmpleado,
                entity.Nombre,
                entity.Apellido,
                entity.Cedula,
                entity.Cargo,
                entity.Email,
                entity.Telefono,
                entity.Activo
            }, ct);

        return await GetEmpleadoAsync(id, ct);
    }

    public async Task DeactivateEmpleadoAsync(
        Guid id, Guid usuarioId, CancellationToken ct)
    {
        var entity = await _db.Empleados.FindAsync([id], ct)
            ?? throw ApiException.NotFound("Empleado");

        if (!entity.Activo)
            return;

        if (await _db.Usuarios.AnyAsync(
                x => x.EmpleadoId == id && x.Activo, ct))
            throw ApiException.BusinessRule(
                "EMPLOYEE_HAS_ACTIVE_USER",
                "No se puede desactivar el empleado mientras tenga un usuario activo.");

        entity.Activo = false;
        entity.FechaActualizacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await Audit(usuarioId, "DESACTIVAR", "Empleado", id,
            new { Activo = true },
            new { Activo = false },
            ct);
    }

    // =========================================================
    // VEHÍCULOS
    // =========================================================

    public async Task<List<VehiculoAdminDto>> GetVehiculosAsync(
        bool incluirInactivos, CancellationToken ct)
    {
        var query = _db.Vehiculos.AsNoTracking()
            .Include(x => x.Departamento)
            .Include(x => x.TipoCombustible)
            .AsQueryable();

        if (!incluirInactivos)
            query = query.Where(x => x.Activo);

        return await query.OrderBy(x => x.Placa)
            .Select(x => new VehiculoAdminDto(
                x.Id,
                x.DepartamentoId,
                x.Departamento.Nombre,
                x.TipoCombustibleId,
                x.TipoCombustible.Nombre,
                x.Placa,
                x.Ficha,
                x.Marca,
                x.Modelo,
                x.Anio,
                x.TipoVehiculo,
                x.CapacidadTanque,
                x.OdometroActual,
                x.Activo,
                x.FechaCreacion,
                x.FechaActualizacion))
            .ToListAsync(ct);
    }

    public async Task<VehiculoAdminDto> GetVehiculoAsync(
        Guid id, CancellationToken ct)
    {
        var x = await _db.Vehiculos.AsNoTracking()
            .Include(x => x.Departamento)
            .Include(x => x.TipoCombustible)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiException.NotFound("Vehículo");

        return MapVehiculo(x);
    }

    public async Task<VehiculoAdminDto> CreateVehiculoAsync(
        CreateVehiculoDto request, Guid usuarioId, CancellationToken ct)
    {
        await ValidateDepartamento(request.DepartamentoId, ct);
        await ValidateTipoCombustible(request.TipoCombustibleId, ct);

        var placa = Required(request.Placa, "La placa es obligatoria.");
        var ficha = Required(request.Ficha, "La ficha es obligatoria.");

        ValidateVehiculo(request, placa, ficha);

        await ValidateVehiculoUnique(null, placa, ficha, ct);

        var entity = new Vehiculo
        {
            Id = Guid.NewGuid(),
            DepartamentoId = request.DepartamentoId,
            TipoCombustibleId = request.TipoCombustibleId,
            Placa = placa,
            Ficha = ficha,
            Marca = Optional(request.Marca),
            Modelo = Optional(request.Modelo),
            Anio = request.Anio,
            TipoVehiculo = Optional(request.TipoVehiculo),
            CapacidadTanque = request.CapacidadTanque,
            OdometroActual = request.OdometroActual,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        _db.Vehiculos.Add(entity);
        await _db.SaveChangesAsync(ct);

        await Audit(usuarioId, "CREAR", "Vehiculo", entity.Id,
            null,
            new
            {
                entity.DepartamentoId,
                entity.TipoCombustibleId,
                entity.Placa,
                entity.Ficha,
                entity.Marca,
                entity.Modelo,
                entity.Anio,
                entity.TipoVehiculo,
                entity.CapacidadTanque,
                entity.OdometroActual,
                entity.Activo
            }, ct);

        return await GetVehiculoAsync(entity.Id, ct);
    }

    public async Task<VehiculoAdminDto> UpdateVehiculoAsync(
        Guid id, UpdateVehiculoDto request,
        Guid usuarioId, CancellationToken ct)
    {
        var entity = await _db.Vehiculos.FindAsync([id], ct)
            ?? throw ApiException.NotFound("Vehículo");

        await ValidateDepartamento(request.DepartamentoId, ct);
        await ValidateTipoCombustible(request.TipoCombustibleId, ct);

        var placa = Required(request.Placa, "La placa es obligatoria.");
        var ficha = Required(request.Ficha, "La ficha es obligatoria.");

        ValidateVehiculo(request, placa, ficha);

        if (request.OdometroActual < entity.OdometroActual)
            throw ApiException.ValidationError(
                "El odómetro no puede disminuir respecto al valor actual.");

        await ValidateVehiculoUnique(id, placa, ficha, ct);

        var previous = new
        {
            entity.DepartamentoId,
            entity.TipoCombustibleId,
            entity.Placa,
            entity.Ficha,
            entity.Marca,
            entity.Modelo,
            entity.Anio,
            entity.TipoVehiculo,
            entity.CapacidadTanque,
            entity.OdometroActual,
            entity.Activo
        };

        entity.DepartamentoId = request.DepartamentoId;
        entity.TipoCombustibleId = request.TipoCombustibleId;
        entity.Placa = placa;
        entity.Ficha = ficha;
        entity.Marca = Optional(request.Marca);
        entity.Modelo = Optional(request.Modelo);
        entity.Anio = request.Anio;
        entity.TipoVehiculo = Optional(request.TipoVehiculo);
        entity.CapacidadTanque = request.CapacidadTanque;
        entity.OdometroActual = request.OdometroActual;
        entity.Activo = request.Activo;
        entity.FechaActualizacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await Audit(usuarioId, "ACTUALIZAR", "Vehiculo", id,
            previous,
            new
            {
                entity.DepartamentoId,
                entity.TipoCombustibleId,
                entity.Placa,
                entity.Ficha,
                entity.Marca,
                entity.Modelo,
                entity.Anio,
                entity.TipoVehiculo,
                entity.CapacidadTanque,
                entity.OdometroActual,
                entity.Activo
            }, ct);

        return await GetVehiculoAsync(id, ct);
    }

    public async Task DeactivateVehiculoAsync(
        Guid id, Guid usuarioId, CancellationToken ct)
    {
        var entity = await _db.Vehiculos.FindAsync([id], ct)
            ?? throw ApiException.NotFound("Vehículo");

        if (!entity.Activo)
            return;

        entity.Activo = false;
        entity.FechaActualizacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await Audit(usuarioId, "DESACTIVAR", "Vehiculo", id,
            new { Activo = true },
            new { Activo = false },
            ct);
    }

    // =========================================================
    // USUARIOS
    // =========================================================

    public async Task<List<UsuarioAdminDto>> GetUsuariosAsync(
        bool incluirInactivos, CancellationToken ct)
    {
        var query = _db.Usuarios.AsNoTracking()
            .Include(x => x.Rol)
            .Include(x => x.Empleado)
            .Include(x => x.Estacion)
            .AsQueryable();

        if (!incluirInactivos)
            query = query.Where(x => x.Activo);

        return await query.OrderBy(x => x.NombreUsuario)
            .Select(x => new UsuarioAdminDto(
                x.Id,
                x.NombreUsuario,
                x.Email,
                x.RolId,
                x.Rol.Nombre,
                x.EmpleadoId,
                x.Empleado == null
                    ? null
                    : (x.Empleado.Nombre + " " + x.Empleado.Apellido).Trim(),
                x.EstacionId,
                x.Estacion == null ? null : x.Estacion.Nombre,
                x.Activo,
                x.Bloqueado,
                x.IntentosFallidos,
                x.UltimoAcceso,
                x.FechaCreacion,
                x.FechaActualizacion))
            .ToListAsync(ct);
    }

    public async Task<UsuarioAdminDto> GetUsuarioAsync(
        Guid id, CancellationToken ct)
    {
        var x = await _db.Usuarios.AsNoTracking()
            .Include(x => x.Rol)
            .Include(x => x.Empleado)
            .Include(x => x.Estacion)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiException.NotFound("Usuario");

        return MapUsuario(x);
    }

    public async Task<UsuarioAdminDto> CreateUsuarioAsync(
        CreateUsuarioDto request, Guid usuarioId, CancellationToken ct)
    {
        var username = Required(
            request.NombreUsuario,
            "El nombre de usuario es obligatorio.");

        var email = Required(
            request.Email,
            "El correo electrónico es obligatorio.");

        var password = Required(
            request.Password,
            "La contraseña inicial es obligatoria.");

        Max(username, 60, "El nombre de usuario");
        Max(email, 150, "El correo electrónico");

        if (password.Length < 8)
            throw ApiException.ValidationError(
                "La contraseña inicial debe contener al menos 8 caracteres.");

        var rol = await _db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == request.RolId && x.Activo, ct)
            ?? throw ApiException.ValidationError(
                "El rol seleccionado no existe o está inactivo.");

        await ValidateUsuarioRelations(
            null, rol, request.EmpleadoId, request.EstacionId, ct);

        if (await _db.Usuarios.AnyAsync(
                x => x.NombreUsuario.ToLower() == username.ToLower(), ct))
            throw ApiException.Conflict(
                "USERNAME_EXISTS",
                "El nombre de usuario ya está registrado.");

        if (await _db.Usuarios.AnyAsync(
                x => x.Email.ToLower() == email.ToLower(), ct))
            throw ApiException.Conflict(
                "USER_EMAIL_EXISTS",
                "El correo electrónico ya está registrado.");

        var entity = new Usuario
        {
            Id = Guid.NewGuid(),
            NombreUsuario = username,
            Email = email,
            RolId = rol.Id,
            EmpleadoId = request.EmpleadoId,
            EstacionId = rol.Nombre == "DESPACHADOR"
                ? request.EstacionId
                : null,
            Activo = true,
            Bloqueado = false,
            IntentosFallidos = 0,
            FechaCreacion = DateTime.UtcNow
        };

        entity.PasswordHash =
            _passwordHasher.HashPassword(entity, password);

        _db.Usuarios.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Nunca registrar Password ni PasswordHash.
        await Audit(usuarioId, "CREAR", "Usuario", entity.Id,
            null,
            new
            {
                entity.NombreUsuario,
                entity.Email,
                entity.RolId,
                entity.EmpleadoId,
                entity.EstacionId,
                entity.Activo,
                entity.Bloqueado
            }, ct);

        return await GetUsuarioAsync(entity.Id, ct);
    }

    public async Task<UsuarioAdminDto> UpdateUsuarioAsync(
        Guid id, UpdateUsuarioDto request,
        Guid usuarioId, CancellationToken ct)
    {
        var entity = await _db.Usuarios.FindAsync([id], ct)
            ?? throw ApiException.NotFound("Usuario");

        var username = Required(
            request.NombreUsuario,
            "El nombre de usuario es obligatorio.");

        var email = Required(
            request.Email,
            "El correo electrónico es obligatorio.");

        Max(username, 60, "El nombre de usuario");
        Max(email, 150, "El correo electrónico");

        var rol = await _db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == request.RolId && x.Activo, ct)
            ?? throw ApiException.ValidationError(
                "El rol seleccionado no existe o está inactivo.");

        await ValidateUsuarioRelations(
            id, rol, request.EmpleadoId, request.EstacionId, ct);

        if (await _db.Usuarios.AnyAsync(
                x => x.Id != id &&
                     x.NombreUsuario.ToLower() == username.ToLower(), ct))
            throw ApiException.Conflict(
                "USERNAME_EXISTS",
                "El nombre de usuario ya está registrado.");

        if (await _db.Usuarios.AnyAsync(
                x => x.Id != id &&
                     x.Email.ToLower() == email.ToLower(), ct))
            throw ApiException.Conflict(
                "USER_EMAIL_EXISTS",
                "El correo electrónico ya está registrado.");

        var previous = new
        {
            entity.NombreUsuario,
            entity.Email,
            entity.RolId,
            entity.EmpleadoId,
            entity.EstacionId,
            entity.Activo,
            entity.Bloqueado
        };

        entity.NombreUsuario = username;
        entity.Email = email;
        entity.RolId = rol.Id;
        entity.EmpleadoId = request.EmpleadoId;
        entity.EstacionId = rol.Nombre == "DESPACHADOR"
            ? request.EstacionId
            : null;
        entity.Activo = request.Activo;
        entity.Bloqueado = request.Bloqueado;

        if (!entity.Bloqueado)
            entity.IntentosFallidos = 0;

        entity.FechaActualizacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await Audit(usuarioId, "ACTUALIZAR", "Usuario", id,
            previous,
            new
            {
                entity.NombreUsuario,
                entity.Email,
                entity.RolId,
                entity.EmpleadoId,
                entity.EstacionId,
                entity.Activo,
                entity.Bloqueado
            }, ct);

        return await GetUsuarioAsync(id, ct);
    }

    public async Task ChangeUsuarioPasswordAsync(
        Guid id, ChangeUsuarioPasswordDto request,
        Guid usuarioId, CancellationToken ct)
    {
        var entity = await _db.Usuarios.FindAsync([id], ct)
            ?? throw ApiException.NotFound("Usuario");

        var password = Required(
            request.NuevaPassword,
            "La nueva contraseña es obligatoria.");

        if (password.Length < 8)
            throw ApiException.ValidationError(
                "La nueva contraseña debe contener al menos 8 caracteres.");

        entity.PasswordHash =
            _passwordHasher.HashPassword(entity, password);

        entity.FechaActualizacion = DateTime.UtcNow;

        // Revoca las sesiones existentes del usuario.
        var activeTokens = await _db.RefreshTokens
            .Where(x =>
                x.UsuarioId == id &&
                x.FechaRevocacion == null)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
            token.FechaRevocacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // No incluir contraseña ni hash en auditoría.
        await Audit(usuarioId, "CAMBIAR_PASSWORD", "Usuario", id,
            null,
            new { SesionesRevocadas = activeTokens.Count },
            ct);
    }

    public async Task DeactivateUsuarioAsync(
        Guid id, Guid usuarioId, CancellationToken ct)
    {
        if (id == usuarioId)
            throw ApiException.BusinessRule(
                "CANNOT_DEACTIVATE_SELF",
                "No puede desactivar su propio usuario.");

        var entity = await _db.Usuarios.FindAsync([id], ct)
            ?? throw ApiException.NotFound("Usuario");

        if (!entity.Activo)
            return;

        entity.Activo = false;
        entity.Bloqueado = true;
        entity.FechaActualizacion = DateTime.UtcNow;

        var activeTokens = await _db.RefreshTokens
            .Where(x =>
                x.UsuarioId == id &&
                x.FechaRevocacion == null)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
            token.FechaRevocacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await Audit(usuarioId, "DESACTIVAR", "Usuario", id,
            new { Activo = true },
            new
            {
                entity.Activo,
                entity.Bloqueado,
                SesionesRevocadas = activeTokens.Count
            }, ct);
    }

    // =========================================================
    // CATÁLOGOS
    // =========================================================

    public async Task<List<TipoCombustibleAdminDto>>
        GetTiposCombustibleAsync(CancellationToken ct)
    {
        return await _db.TiposCombustible.AsNoTracking()
            .Where(x => x.Activo)
            .OrderBy(x => x.Nombre)
            .Select(x => new TipoCombustibleAdminDto(
                x.Id,
                x.Codigo,
                x.Nombre,
                x.Descripcion,
                x.Activo))
            .ToListAsync(ct);
    }

    // =========================================================
    // VALIDACIONES / MAPPERS
    // =========================================================

    private async Task ValidateDepartamento(
        Guid departamentoId, CancellationToken ct)
    {
        if (!await _db.Departamentos.AnyAsync(
                x => x.Id == departamentoId && x.Activo, ct))
            throw ApiException.ValidationError(
                "El departamento seleccionado no existe o está inactivo.");
    }

    private async Task ValidateTipoCombustible(
        short id, CancellationToken ct)
    {
        if (!await _db.TiposCombustible.AnyAsync(
                x => x.Id == id && x.Activo, ct))
            throw ApiException.ValidationError(
                "El tipo de combustible seleccionado no existe o está inactivo.");
    }

    private async Task ValidateEmpleadoUnique(
        Guid? currentId,
        string codigo,
        string cedula,
        string? email,
        CancellationToken ct)
    {
        if (await _db.Empleados.AnyAsync(
                x => x.Id != currentId &&
                     x.CodigoEmpleado.ToLower() == codigo.ToLower(), ct))
            throw ApiException.Conflict(
                "EMPLOYEE_CODE_EXISTS",
                "Ya existe un empleado con ese código.");

        if (await _db.Empleados.AnyAsync(
                x => x.Id != currentId &&
                     x.Cedula.ToLower() == cedula.ToLower(), ct))
            throw ApiException.Conflict(
                "EMPLOYEE_DOCUMENT_EXISTS",
                "Ya existe un empleado con esa cédula.");

        if (email != null &&
            await _db.Empleados.AnyAsync(
                x => x.Id != currentId &&
                     x.Email != null &&
                     x.Email.ToLower() == email.ToLower(), ct))
            throw ApiException.Conflict(
                "EMPLOYEE_EMAIL_EXISTS",
                "Ya existe un empleado con ese correo electrónico.");
    }

    private async Task ValidateVehiculoUnique(
        Guid? currentId,
        string placa,
        string ficha,
        CancellationToken ct)
    {
        if (await _db.Vehiculos.AnyAsync(
                x => x.Id != currentId &&
                     x.Placa.ToLower() == placa.ToLower(), ct))
            throw ApiException.Conflict(
                "VEHICLE_PLATE_EXISTS",
                "Ya existe un vehículo con esa placa.");

        if (await _db.Vehiculos.AnyAsync(
                x => x.Id != currentId &&
                     x.Ficha.ToLower() == ficha.ToLower(), ct))
            throw ApiException.Conflict(
                "VEHICLE_ASSET_EXISTS",
                "Ya existe un vehículo con esa ficha.");
    }

    private async Task ValidateUsuarioRelations(
        Guid? currentUserId,
        Rol rol,
        Guid? empleadoId,
        Guid? estacionId,
        CancellationToken ct)
    {
        if (empleadoId.HasValue)
        {
            if (!await _db.Empleados.AnyAsync(
                    x => x.Id == empleadoId.Value && x.Activo, ct))
                throw ApiException.ValidationError(
                    "El empleado seleccionado no existe o está inactivo.");

            if (await _db.Usuarios.AnyAsync(
                    x => x.Id != currentUserId &&
                         x.EmpleadoId == empleadoId.Value, ct))
                throw ApiException.Conflict(
                    "EMPLOYEE_ALREADY_HAS_USER",
                    "El empleado seleccionado ya tiene una cuenta de usuario.");
        }

        if (rol.Nombre == "DESPACHADOR")
        {
            if (!estacionId.HasValue)
                throw ApiException.ValidationError(
                    "Todo despachador debe tener una estación asignada.");

            if (!await _db.Estaciones.AnyAsync(
                    x => x.Id == estacionId.Value && x.Activo, ct))
                throw ApiException.ValidationError(
                    "La estación seleccionada no existe o está inactiva.");
        }
    }

    private static void ValidateEmpleadoLengths(
        string codigo,
        string nombre,
        string apellido,
        string cedula,
        string? cargo,
        string? email,
        string? telefono)
    {
        Max(codigo, 30, "El código de empleado");
        Max(nombre, 80, "El nombre");
        Max(apellido, 80, "El apellido");
        Max(cedula, 20, "La cédula");
        MaxOptional(cargo, 100, "El cargo");
        MaxOptional(email, 150, "El correo electrónico");
        MaxOptional(telefono, 25, "El teléfono");
    }

    private static void ValidateVehiculo(
        CreateVehiculoDto request,
        string placa,
        string ficha)
    {
        ValidateVehiculoCommon(
            placa, ficha, request.Marca, request.Modelo,
            request.TipoVehiculo, request.Anio,
            request.CapacidadTanque, request.OdometroActual);
    }

    private static void ValidateVehiculo(
        UpdateVehiculoDto request,
        string placa,
        string ficha)
    {
        ValidateVehiculoCommon(
            placa, ficha, request.Marca, request.Modelo,
            request.TipoVehiculo, request.Anio,
            request.CapacidadTanque, request.OdometroActual);
    }

    private static void ValidateVehiculoCommon(
        string placa,
        string ficha,
        string? marca,
        string? modelo,
        string? tipoVehiculo,
        short? anio,
        decimal capacidadTanque,
        decimal odometroActual)
    {
        Max(placa, 20, "La placa");
        Max(ficha, 30, "La ficha");
        MaxOptional(marca, 50, "La marca");
        MaxOptional(modelo, 50, "El modelo");
        MaxOptional(tipoVehiculo, 50, "El tipo de vehículo");

        if (capacidadTanque <= 0)
            throw ApiException.ValidationError(
                "La capacidad del tanque debe ser mayor que cero.");

        if (odometroActual < 0)
            throw ApiException.ValidationError(
                "El odómetro no puede ser negativo.");

        if (anio.HasValue && (anio.Value < 1900 || anio.Value > 2100))
            throw ApiException.ValidationError(
                "El año del vehículo no es válido.");
    }

    private async Task Audit(
        Guid usuarioId,
        string accion,
        string entidad,
        Guid entidadId,
        object? anterior,
        object? nuevo,
        CancellationToken ct)
    {
        await _audit.RegistrarAsync(
            usuarioId,
            accion,
            entidad,
            entidadId.ToString(),
            null,
            anterior,
            nuevo,
            ct);
    }

    private static string Required(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw ApiException.ValidationError(message);

        return value.Trim();
    }

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Max(
        string value, int max, string field)
    {
        if (value.Length > max)
            throw ApiException.ValidationError(
                $"{field} no puede exceder {max} caracteres.");
    }

    private static void MaxOptional(
        string? value, int max, string field)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            value.Trim().Length > max)
            throw ApiException.ValidationError(
                $"{field} no puede exceder {max} caracteres.");
    }

    private static DepartamentoAdminDto MapDepartamento(Departamento x) =>
        new(
            x.Id, x.Codigo, x.Nombre, x.Descripcion,
            x.Activo, x.FechaCreacion, x.FechaActualizacion);

    private static EmpleadoAdminDto MapEmpleado(Empleado x) =>
        new(
            x.Id,
            x.DepartamentoId,
            x.Departamento.Nombre,
            x.CodigoEmpleado,
            x.Nombre,
            x.Apellido,
            x.NombreCompleto,
            x.Cedula,
            x.Cargo,
            x.Email,
            x.Telefono,
            x.Activo,
            x.FechaCreacion,
            x.FechaActualizacion);

    private static VehiculoAdminDto MapVehiculo(Vehiculo x) =>
        new(
            x.Id,
            x.DepartamentoId,
            x.Departamento.Nombre,
            x.TipoCombustibleId,
            x.TipoCombustible.Nombre,
            x.Placa,
            x.Ficha,
            x.Marca,
            x.Modelo,
            x.Anio,
            x.TipoVehiculo,
            x.CapacidadTanque,
            x.OdometroActual,
            x.Activo,
            x.FechaCreacion,
            x.FechaActualizacion);

    private static UsuarioAdminDto MapUsuario(Usuario x) =>
        new(
            x.Id,
            x.NombreUsuario,
            x.Email,
            x.RolId,
            x.Rol.Nombre,
            x.EmpleadoId,
            x.Empleado == null ? null : x.Empleado.NombreCompleto,
            x.EstacionId,
            x.Estacion?.Nombre,
            x.Activo,
            x.Bloqueado,
            x.IntentosFallidos,
            x.UltimoAcceso,
            x.FechaCreacion,
            x.FechaActualizacion);
}